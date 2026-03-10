# AprilTag Bridge Integration (PICO 4 Ultra)

This project expects an Android Java bridge:

- Class: `com.project.apriltag.AprilTagBridge`
- Static APIs:
  - `boolean isReady()`
  - `String detectWithIntrinsics(long rgbaPtr, int width, int height, int stride, float fx, float fy, float cx, float cy, String family, int id, float tagSizeMeters)`
  - `String detect(long rgbaPtr, int width, int height, int stride, String family, int id, float tagSizeMeters)` (fallback)

Returned JSON:

```json
{"ok":true,"id":0,"tx":0.1,"ty":0.2,"tz":1.3,"qx":0,"qy":0,"qz":0,"qw":1,"confidence":0.92}
```

## Steps

1. Build Android AAR that contains `com.project.apriltag.AprilTagBridge`.
2. Include native `arm64-v8a` library for AprilTag detection.
3. Copy AAR (and `.so` if separate) to `Assets/Plugins/Android/`.
4. In Unity Inspector, ensure Android platform is enabled and CPU includes ARM64.
5. Build to headset and verify Calibration debug text shows `Detector: Ready`.

## Notes

- `rgbaPtr` is a native memory pointer to RGBA frame data from PICO camera stream.
- Pose returned must be **tag pose in camera coordinates**.
- Tag family and ID are provided by Unity config (`36h11`, `id=0` by default).
