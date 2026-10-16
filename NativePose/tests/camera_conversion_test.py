"""Exercise production C ABI on padded planar/interleaved YUV; no camera required."""
import ctypes as C
import time
from pathlib import Path
root=Path(__file__).resolve().parents[2]
lib=C.CDLL(str(root/'Assets/Plugins/macOS/libfitfight_pose.dylib'))
lib.ff_camera_create.restype=C.c_void_p
lib.ff_camera_destroy.argtypes=[C.c_void_p]
fn=lib.ff_camera_convert
fn.argtypes=[C.c_void_p,C.c_void_p,C.c_int64,C.c_int,C.c_void_p,C.c_int64,C.c_int,C.c_void_p,C.c_int64,C.c_int,C.c_int,C.c_int,C.c_int,C.c_int,C.c_void_p,C.c_int64,C.c_int,C.c_int]
ctx=lib.ff_camera_create();checks=0

def check(ok,label):
 global checks
 if not ok: raise AssertionError(label)
 checks+=1

def convert(sw,sh,angle,pixel=1,colors=None,w=None,h=None):
 ys=sw+7;uvs=((sw+1)//2)*pixel+7
 y=(C.c_uint8*(ys*sh))();u=(C.c_uint8*(uvs*((sh+1)//2)))();v=(C.c_uint8*len(u))()
 for row in range(sh):
  for col in range(sw): y[row*ys+col]=colors[0] if colors else 16+(row*sw+col)*200//(sw*sh)
 for row in range((sh+1)//2):
  for col in range((sw+1)//2): u[row*uvs+col*pixel]=colors[1] if colors else 128;v[row*uvs+col*pixel]=colors[2] if colors else 128
 rw,rh=(sh,sw) if angle%180 else (sw,sh)
 w,h=(w or rw),(h or rh);dst=(C.c_uint8*(w*h*4))()
 args=(ctx,y,len(y),ys,u,len(u),uvs,v,len(v),uvs,pixel,sw,sh,angle,dst,len(dst),w,h)
 check(fn(*args)==0,f'Converter accepts valid padded input {sw}x{sh} angle={angle} pixel={pixel}')
 return y,dst,args

try:
 for pixel in [1,2]:
  for angle in [0,90,180,270]:
   sw,sh=4,6;y,dst,args=convert(sw,sh,angle,pixel)
   w,h=(sh,sw) if angle%180 else (sw,sh)
   for row in range(h):
    for col in range(w):
     sx,sy={0:(col,row),90:(row,sh-1-col),180:(sw-1-col,sh-1-row),270:(sw-1-row,col)}[angle]
     yy=y[sy*(sw+7)+sx];expected=max(0,min(255,round((yy-16)*255/219)))
     rgb=list(dst[(row*w+col)*4:(row*w+col)*4+4])
     check(all(abs(c-expected)<=2 for c in rgb[:3]) and rgb[3]==255,'Upright pixel orientation and RGBA channels')
 # Contiguous NV12/NV21 fast paths, nonzero buffer positions and padded rows.
 for order in [0,1]:
  for angle in [0,90,180,270]:
   sw,sh,ys,uvs=4,6,11,11
   y=(C.c_uint8*(ys*sh))();uv=(C.c_uint8*(uvs*(sh//2)+7))()
   for row in range(sh):
    for col in range(sw):y[row*ys+col]=81
   for row in range(sh//2):
    for col in range(sw//2):uv[7+row*uvs+col*2+order]=90;uv[7+row*uvs+col*2+1-order]=240
   w,h=(sh,sw) if angle%180 else (sw,sh);dst=(C.c_uint8*(w*h*4))()
   u=C.byref(uv,7+order);v=C.byref(uv,8-order)
   check(fn(ctx,y,len(y),ys,u,len(uv)-7-order,uvs,v,len(uv)-8+order,uvs,2,sw,sh,angle,dst,len(dst),w,h)==0,'NV12/NV21 rotation with offset buffers')
   check(all(dst[i*4]>250 and dst[i*4+1]<3 and dst[i*4+2]<3 and dst[i*4+3]==255 for i in range(w*h)),'Interleaved conversion preserves correct color and alpha')
 for yy,uu,vv,channel in [(81,90,240,0),(145,54,34,1),(41,240,110,2)]:
  _,dst,_=convert(4,6,0,2,(yy,uu,vv));check(dst[channel]>250 and sum(dst[:3])-dst[channel]<8,'ABGR produces RGBA memory order')
 _,_,args=convert(5,3,90,2);check(fn(*args)==0,'Odd YUV dimensions and rotation stay in bounds')
 for change in [{2:1},{5:1},{8:1},{15:1},{13:45},{17:513}]:
  invalid=list(args)
  for index,value in change.items():invalid[index]=value
  check(fn(*invalid)!=0,'Reject insufficient capacity / invalid rotation / output dimensions')
 for size in [512,384]:
  _,_,args=convert(640,480,90,2,w=size*3//4,h=size)
  started=time.perf_counter()
  for _ in range(100):check(fn(*args)==0,'Repeated conversion with retained scratch buffers')
  print(f'HOST portable conversion {size}: {(time.perf_counter()-started)*10:.3f} ms/frame (not Android NEON performance)')
 print(f'PASS {checks} native conversion checks; Android JNI/camera/thermal timing still require phone verification.')
finally:lib.ff_camera_destroy(ctx)
