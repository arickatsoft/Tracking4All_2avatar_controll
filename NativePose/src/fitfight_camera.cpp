#include <algorithm>
#include <cstdint>
#include <vector>
#include "libyuv.h"
#ifdef __ANDROID__
#include <jni.h>
#endif
#define FF_API extern "C" __attribute__((visibility("default")))
struct CameraConversion { std::vector<uint8_t> rotated, scaled, planar; double luma=0; };
// Mean source luma over a sparse grid; drives the optional exposure-cap ISO loop.
FF_API double ff_camera_luma(void* context) { return context ? static_cast<CameraConversion*>(context)->luma : 0; }
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
    {
        uint64_t sum=0,count=0;
        for(int row=4;row<sh;row+=8) for(int col=4;col<sw;col+=8) { sum+=y[static_cast<int64_t>(row)*ys+col];count++; }
        c.luma=count?static_cast<double>(sum)/count:0;
    }
    // Downscale in sensor orientation with a box filter before rotating: anti-aliased
    // thin limbs/wrists at 384/288 input, and rotation runs on the smaller image.
    int pw=rotation%180?h:w,ph=rotation%180?w:h;
    int cw=(sw+1)/2,ch=(sh+1)/2;
    const uint8_t *iy=y,*iu=u,*iv=v;int iys=ys,ius=us,ivs=vs;
    if(uvPixel!=1) {
        // NV12/NV21 and other chroma pixel strides are normalized to I420 first (NEON).
        c.planar.resize(static_cast<size_t>(sw)*sh+2*static_cast<size_t>(cw)*ch);
        uint8_t* py=c.planar.data();uint8_t* pu=py+sw*sh;uint8_t* pv=pu+cw*ch;
        int normalized=libyuv::Android420ToI420(y,ys,u,us,v,vs,uvPixel,py,sw,pu,cw,pv,cw,sw,sh);
        if(normalized) return normalized;
        iy=py;iys=sw;iu=pu;ius=cw;iv=pv;ivs=cw;
    }
    const uint8_t *sy=iy,*su=iu,*sv=iv;int sys=iys,sus=ius,svs=ivs;
    int dcw=(pw+1)/2,dch=(ph+1)/2;
    if(pw!=sw || ph!=sh) {
        c.scaled.resize(static_cast<size_t>(pw)*ph+2*static_cast<size_t>(dcw)*dch);
        uint8_t* dy=c.scaled.data();uint8_t* du=dy+pw*ph;uint8_t* dv=du+dcw*dch;
        int error=libyuv::I420Scale(iy,iys,iu,ius,iv,ivs,sw,sh,dy,pw,du,dcw,dv,dcw,pw,ph,libyuv::kFilterBox);
        if(error) return error;
        sy=dy;sys=pw;su=du;sus=dcw;sv=dv;svs=dcw;
    }
    if(rotation==0) return libyuv::I420ToABGR(sy,sys,su,sus,sv,svs,rgba,w*4,w,h);
    int ocw=(w+1)/2,och=(h+1)/2;
    c.rotated.resize(static_cast<size_t>(w)*h+2*static_cast<size_t>(ocw)*och);
    uint8_t* ry=c.rotated.data();uint8_t* ru=ry+w*h;uint8_t* rv=ru+ocw*och;
    int error=libyuv::I420Rotate(sy,sys,su,sus,sv,svs,ry,w,ru,ocw,rv,ocw,pw,ph,static_cast<libyuv::RotationMode>(rotation));
    if(error) return error;
    return libyuv::I420ToABGR(ry,w,ru,ocw,rv,ocw,rgba,w*4,w,h);
}
#ifdef __ANDROID__
extern "C" JNIEXPORT jlong JNICALL Java_com_fitfight_camera_FitFightCamera_createConverter(JNIEnv*,jclass) {
    return reinterpret_cast<jlong>(ff_camera_create());
}
extern "C" JNIEXPORT jdouble JNICALL Java_com_fitfight_camera_FitFightCamera_lastLuma(JNIEnv*,jclass,jlong c) {
    return ff_camera_luma(reinterpret_cast<void*>(c));
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
