package com.project.apriltag;

public final class AprilTagBridge {
    static {
        try {
            System.loadLibrary("apriltag_bridge");
            sReady = true;
        } catch (Throwable t) {
            sReady = false;
        }
    }

    private static boolean sReady = false;

    private AprilTagBridge() {}

    public static boolean isReady() {
        return sReady;
    }

    public static String detectWithIntrinsics(
            long rgbaPtr,
            int width,
            int height,
            int stride,
            float fx,
            float fy,
            float cx,
            float cy,
            String family,
            int id,
            float tagSizeMeters) {
        if (!sReady) {
            return "";
        }
        return nativeDetectWithIntrinsics(
                rgbaPtr, width, height, stride, fx, fy, cx, cy, family, id, tagSizeMeters);
    }

    // Unity side will fallback to this when detectWithIntrinsics is absent.
    public static String detect(
            long rgbaPtr,
            int width,
            int height,
            int stride,
            String family,
            int id,
            float tagSizeMeters) {
        if (!sReady) {
            return "";
        }
        return nativeDetect(
                rgbaPtr, width, height, stride, family, id, tagSizeMeters);
    }

    private static native String nativeDetectWithIntrinsics(
            long rgbaPtr,
            int width,
            int height,
            int stride,
            float fx,
            float fy,
            float cx,
            float cy,
            String family,
            int id,
            float tagSizeMeters);

    private static native String nativeDetect(
            long rgbaPtr,
            int width,
            int height,
            int stride,
            String family,
            int id,
            float tagSizeMeters);
}
