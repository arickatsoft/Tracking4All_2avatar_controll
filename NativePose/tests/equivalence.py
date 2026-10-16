from pathlib import Path
import sys,ctypes,json,time
sys.path.insert(0,str(Path('NativePose/python').resolve()))
import numpy as np
import onnxruntime as ort
from PIL import Image
lib=ctypes.CDLL(str(Path('Assets/Plugins/macOS/libfitfight_pose.dylib').resolve()))
lib.ff_pose_create.argtypes=[ctypes.c_void_p,ctypes.c_int,ctypes.c_void_p,ctypes.c_int,ctypes.c_int];lib.ff_pose_create.restype=ctypes.c_void_p
lib.ff_pose_infer.argtypes=[ctypes.c_void_p,ctypes.c_void_p,ctypes.c_void_p,ctypes.c_void_p];lib.ff_pose_destroy.argtypes=[ctypes.c_void_p]
p=Path('NativePose/converted/Body26.param').read_bytes();w=Path('NativePose/converted/Body26.bin').read_bytes()
ctx=lib.ff_pose_create(p,len(p),w,len(w),2)
assert ctx,'native model load failed'
sess=ort.InferenceSession('Assets/BodyTracking/Resources/RTMPose/Body26.onnx',providers=['CPUExecutionProvider'])
print([(o.name,o.shape) for o in sess.get_outputs()],flush=True)
inputs=[np.zeros((1,3,256,192),dtype=np.float32),np.random.default_rng(8).normal(size=(1,3,256,192)).astype(np.float32)]
im=np.asarray(Image.open('NativePose/tests/standing.png').convert('RGB').resize((192,256)),dtype=np.float32)
inputs.append(np.transpose((im-np.array([123.675,116.28,103.53]))/np.array([58.395,57.12,57.375]),(2,0,1))[None].astype(np.float32))
reports=[]
try:
 for a in inputs:
  a=np.ascontiguousarray(a);x=np.zeros((1,26,384),np.float32);y=np.zeros((1,26,512),np.float32)
  t=time.perf_counter();err=lib.ff_pose_infer(ctx,a.ctypes.data,x.ctypes.data,y.ctypes.data);ms=(time.perf_counter()-t)*1000
  assert err==0,err
  ref=sess.run(None,{sess.get_inputs()[0].name:a})
  pairs= [(x,next(o for o in ref if o.shape[-1]==384)),(y,next(o for o in ref if o.shape[-1]==512))]
  maxerr=max(float(np.max(np.abs(a-b))) for a,b in pairs)
  rms=max(float(np.sqrt(np.mean((a-b)**2))) for a,b in pairs)
  binerr=max(int(np.max(np.abs(a.argmax(-1)-b.argmax(-1)))) for a,b in pairs)
  reports.append(dict(maxAbsoluteError=maxerr,rmsError=rms,maxArgmaxBinError=binerr,nativeHostMs=ms))
  assert maxerr<.003 and binerr<=1,reports[-1]
finally: lib.ff_pose_destroy(ctx)
Path('Logs/ncnn-equivalence.json').write_text(json.dumps(reports,indent=2))
print(json.dumps(reports,indent=2))
