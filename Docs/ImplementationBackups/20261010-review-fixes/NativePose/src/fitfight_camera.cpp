#include <algorithm>
#include <cstdint>
#include <vector>
#include "libyuv.h"
#ifdef __ANDROID__
#include <jni.h>
#endif
#define FF_API extern "C" __attribute__((visibility("default")))
struct CameraConversion { std::vector<uint8_t> rotated, scaled, planar; };
FF_API void* ff_camera_create() { return new CameraConversion; }
FF_API void ff_camera_destroy(void* context) { delete static_cast<CameraConversion*>(context); }
static bool planeFits(int64_t capacity,int stride,int pixel,int width,int height) {
    return width>0 && height>0 && stride>0 && pixel>0 &&
        capacity>=static_cast<int64_t>(height-1)*stride+static_cast<int64_t>(width-1)*pixel+1;
}
// Android YUV_420_888: Y pixel stride 1; U and V have the same row/pixel strides.
// ABGR is libyuv's register naming: bytes in memory are R,G,B,A on little endian.
FF_API int ff_camera_convert(void* context,const uint8_t* y,int64_t yc,int ys,
    const uint8_t* u,int64_t uc,int us,const uint8_t* v,int64_t vc,int vs,int uvPixel,
    int sw,int sh,int rotation,uint8_t* rgba,int64_t capacity,int w,int h) {
    if(!context || !y || !u || !v || !rgba || sw<1 || sh<1 || sw>4096 || sh>4096 ||
        w<1 || h<1 || w>512 || h>512 || (rotation!=0 && rotation!=90 && rotation!=180 && rotation!=270) ||
        !planeFits(yc,ys,1,sw,sh) || !planeFits(uc,us,uvPixel,(sw+1)/2,(sh+1)/2) ||
        !planeFits(vc,vs,uvPixel,(sw+1)/2,(sh+1)/2) || capacity<static_cast<int64_t>(w)*h*4) return -1;
    auto& c=*static_cast<CameraConversion*>(context);
    int rw=rotation%180?sh:sw,rh=rotation%180?sw:sh,cw=(rw+1)/2,ch=(rh+1)/2;
    c.rotated.resize(rw*rh+2*cw*ch);
    uint8_t* ry=c.rotated.data();uint8_t* ru=ry+rw*rh;uint8_t* rv=ru+cw*ch;
    // Non-contiguous pixel-stride planes are legal too. libyuv's rotated fast path
    // handles planar/NV12/NV21; normalize other layouts before rotating.
    auto offset=reinterpret_cast<intptr_t>(v)-reinterpret_cast<intptr_t>(u);
    if(uvPixel!=1 && !(uvPixel==2 && (offset==1 || offset==-1) && us==vs)) {
        int pcw=(sw+1)/2,pch=(sh+1)/2;c.planar.resize(sw*sh+2*pcw*pch);
        uint8_t* py=c.planar.data();uint8_t* pu=py+sw*sh;uint8_t* pv=pu+pcw*pch;
        int normalized=libyuv::Android420ToI420(y,ys,u,us,v,vs,uvPixel,py,sw,pu,pcw,pv,pcw,sw,sh);
        if(normalized) return normalized;
        y=py;ys=sw;u=pu;v=pv;us=vs=pcw;uvPixel=1;
    }
    int error=libyuv::Android420ToI420Rotate(y,ys,u,us,v,vs,uvPixel,ry,rw,ru,cw,rv,cw,sw,sh,
        static_cast<libyuv::RotationMode>(rotation));
    if(error) return error;
    if(rw!=w || rh!=h) {
        int dw=(w+1)/2,dh=(h+1)/2;c.scaled.resize(w*h+2*dw*dh);
        uint8_t* dy=c.scaled.data();uint8_t* du=dy+w*h;uint8_t* dv=du+dw*dh;
        error=libyuv::I420Scale(ry,rw,ru,cw,rv,cw,rw,rh,dy,w,du,dw,dv,dw,w,h,libyuv::kFilterNone);
        if(error) return error;
        return libyuv::I420ToABGR(dy,w,du,dw,dv,dw,rgba,w*4,w,h);
    }
    return libyuv::I420ToABGR(ry,rw,ru,cw,rv,cw,rgba,w*4,w,h);
}
#ifdef __ANDROID__
extern "C" JNIEXPORT jlong JNICALL Java_com_fitfight_camera_FitFightCamera_createConverter(JNIEnv*,jclass) {
    return reinterpret_cast<jlong>(ff_camera_create());
}
extern "C" JNIEXPORT void JNICALL Java_com_fitfight_camera_FitFightCamera_destroyConverter(JNIEnv*,jclass,jlong c) {
    ff_camera_destroy(reinterpret_cast<void*>(c));
}
extern "C" JNIEXPORT jint JNICALL Java_com_fitfight_camera_FitFightCamera_convert(JNIEnv* env,jclass,jlong c,
    jobject y,jint ys,jobject u,jint us,jobject v,jint vs,jint pixel,jint sw,jint sh,jint rotation,
    jobject dst,jint w,jint h) {
    return ff_camera_convert(reinterpret_cast<void*>(c),
        static_cast<uint8_t*>(env->GetDirectBufferAddress(y)),env->GetDirectBufferCapacity(y),ys,
        static_cast<uint8_t*>(env->GetDirectBufferAddress(u)),env->GetDirectBufferCapacity(u),us,
        static_cast<uint8_t*>(env->GetDirectBufferAddress(v)),env->GetDirectBufferCapacity(v),vs,pixel,sw,sh,rotation,
        static_cast<uint8_t*>(env->GetDirectBufferAddress(dst)),env->GetDirectBufferCapacity(dst),w,h);
}
#endif
