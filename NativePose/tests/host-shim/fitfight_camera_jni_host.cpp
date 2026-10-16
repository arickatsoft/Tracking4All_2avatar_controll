// Host-only JNI shim for NativePose/tests/CameraLeaseTest.java. Production Android exports the
// same JNI entry points from fitfight_camera.cpp; this forwards to the macOS validation dylib.
#include <jni.h>
#include <cstdint>
extern "C" void* ff_camera_create();
extern "C" void ff_camera_destroy(void*);
extern "C" double ff_camera_luma(void*);
extern "C" int ff_camera_convert(void*,const uint8_t*,int64_t,int,const uint8_t*,int64_t,int,const uint8_t*,int64_t,int,int,int,int,int,uint8_t*,int64_t,int,int);
extern "C" JNIEXPORT jlong JNICALL Java_com_fitfight_camera_FitFightCamera_createConverter(JNIEnv*,jclass) { return reinterpret_cast<jlong>(ff_camera_create()); }
extern "C" JNIEXPORT void JNICALL Java_com_fitfight_camera_FitFightCamera_destroyConverter(JNIEnv*,jclass,jlong c) { ff_camera_destroy(reinterpret_cast<void*>(c)); }
extern "C" JNIEXPORT jdouble JNICALL Java_com_fitfight_camera_FitFightCamera_lastLuma(JNIEnv*,jclass,jlong c) { return ff_camera_luma(reinterpret_cast<void*>(c)); }
extern "C" JNIEXPORT jint JNICALL Java_com_fitfight_camera_FitFightCamera_convert(JNIEnv* env,jclass,jlong c,
    jobject y,jint ys,jobject u,jint us,jobject v,jint vs,jint pixel,jint sw,jint sh,jint rotation,jobject dst,jint w,jint h) {
    return ff_camera_convert(reinterpret_cast<void*>(c),
        static_cast<uint8_t*>(env->GetDirectBufferAddress(y)),env->GetDirectBufferCapacity(y),ys,
        static_cast<uint8_t*>(env->GetDirectBufferAddress(u)),env->GetDirectBufferCapacity(u),us,
        static_cast<uint8_t*>(env->GetDirectBufferAddress(v)),env->GetDirectBufferCapacity(v),vs,pixel,sw,sh,rotation,
        static_cast<uint8_t*>(env->GetDirectBufferAddress(dst)),env->GetDirectBufferCapacity(dst),w,h);
}
