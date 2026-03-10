#include <jni.h>
#include <string>
#include <sstream>

// TODO:
// 1) Add AprilTag library integration.
// 2) Read RGBA data from rgbaPtr (uint8_t*) and convert if needed.
// 3) Detect tag with requested family+id.
// 4) Estimate pose in camera coordinates.

static jstring ToJson(JNIEnv* env, bool ok, int id,
    float tx, float ty, float tz,
    float qx, float qy, float qz, float qw,
    float confidence) {
    std::ostringstream os;
    os << "{\"ok\":" << (ok ? "true" : "false")
       << ",\"id\":" << id
       << ",\"tx\":" << tx
       << ",\"ty\":" << ty
       << ",\"tz\":" << tz
       << ",\"qx\":" << qx
       << ",\"qy\":" << qy
       << ",\"qz\":" << qz
       << ",\"qw\":" << qw
       << ",\"confidence\":" << confidence
       << "}";
    return env->NewStringUTF(os.str().c_str());
}

extern "C" JNIEXPORT jstring JNICALL
Java_com_project_apriltag_AprilTagBridge_nativeDetectWithIntrinsics(
    JNIEnv* env,
    jclass,
    jlong rgbaPtr,
    jint width,
    jint height,
    jint stride,
    jfloat fx,
    jfloat fy,
    jfloat cx,
    jfloat cy,
    jstring family,
    jint id,
    jfloat tagSizeMeters) {
    (void)rgbaPtr;
    (void)width;
    (void)height;
    (void)stride;
    (void)fx;
    (void)fy;
    (void)cx;
    (void)cy;
    (void)family;
    (void)id;
    (void)tagSizeMeters;

    // Return "ok=false" by default until detector is implemented.
    return ToJson(env, false, id, 0.f, 0.f, 0.f, 0.f, 0.f, 0.f, 1.f, 0.f);
}

extern "C" JNIEXPORT jstring JNICALL
Java_com_project_apriltag_AprilTagBridge_nativeDetect(
    JNIEnv* env,
    jclass,
    jlong rgbaPtr,
    jint width,
    jint height,
    jint stride,
    jstring family,
    jint id,
    jfloat tagSizeMeters) {
    (void)rgbaPtr;
    (void)width;
    (void)height;
    (void)stride;
    (void)family;
    (void)id;
    (void)tagSizeMeters;
    return ToJson(env, false, id, 0.f, 0.f, 0.f, 0.f, 0.f, 0.f, 1.f, 0.f);
}
