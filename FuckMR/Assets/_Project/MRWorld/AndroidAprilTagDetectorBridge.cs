using System;
using UnityEngine;

namespace Project.MRWorld
{
    // Bridge to an Android Java detector plugin.
    // Expected Java class:
    //   com.project.apriltag.AprilTagBridge
    // Expected static methods:
    //   boolean isReady()
    //   String detectWithIntrinsics(long rgbaPtr, int width, int height, int stride, float fx, float fy, float cx, float cy, String family, int id, float tagSizeMeters)
    //   String detect(long rgbaPtr, int width, int height, int stride, String family, int id, float tagSizeMeters)
    // Returned JSON example:
    //   {"ok":true,"id":0,"tx":0.1,"ty":0.2,"tz":1.3,"qx":0,"qy":0,"qz":0,"qw":1,"confidence":0.92}
    public sealed class AndroidAprilTagDetectorBridge : IAprilTagDetector
    {
        private const string JavaClassName = "com.project.apriltag.AprilTagBridge";
        private AndroidJavaClass _bridgeClass;
        private bool _checked;
        private bool _available;
        private bool _preferIntrinsicsApi = true;
        private string _lastError;

        public bool IsAvailable
        {
            get
            {
                EnsureReady();
                return _available;
            }
        }

        public string DebugName
        {
            get
            {
                EnsureReady();
                return string.IsNullOrEmpty(_lastError) ? JavaClassName : $"{JavaClassName} ({_lastError})";
            }
        }

        public bool TryDetect(
            IntPtr rgbaBuffer,
            int width,
            int height,
            int strideBytes,
            in AprilTagCameraIntrinsics intrinsics,
            in AprilTagTrackingConfig config,
            out AprilTagDetectionResult result)
        {
            result = default;
            EnsureReady();
            if (!_available || rgbaBuffer == IntPtr.Zero || width <= 0 || height <= 0)
            {
                return false;
            }

            try
            {
                string json;
                if (_preferIntrinsicsApi)
                {
                    try
                    {
                        json = _bridgeClass.CallStatic<string>(
                            "detectWithIntrinsics",
                            rgbaBuffer.ToInt64(),
                            width,
                            height,
                            strideBytes,
                            intrinsics.fx,
                            intrinsics.fy,
                            intrinsics.cx,
                            intrinsics.cy,
                            config.family,
                            config.id,
                            config.tagSizeMeters);
                    }
                    catch
                    {
                        _preferIntrinsicsApi = false;
                        json = _bridgeClass.CallStatic<string>(
                            "detect",
                            rgbaBuffer.ToInt64(),
                            width,
                            height,
                            strideBytes,
                            config.family,
                            config.id,
                            config.tagSizeMeters);
                    }
                }
                else
                {
                    json = _bridgeClass.CallStatic<string>(
                        "detect",
                        rgbaBuffer.ToInt64(),
                        width,
                        height,
                        strideBytes,
                        config.family,
                        config.id,
                        config.tagSizeMeters);
                }

                if (string.IsNullOrWhiteSpace(json))
                {
                    return false;
                }

                var dto = JsonUtility.FromJson<DetectionDto>(json);
                if (dto == null || !dto.ok || dto.id != config.id)
                {
                    return false;
                }

                var pose = new Pose(
                    new Vector3(dto.tx, dto.ty, dto.tz),
                    new Quaternion(dto.qx, dto.qy, dto.qz, dto.qw));
                result = new AprilTagDetectionResult(true, dto.id, pose, dto.confidence);
                return true;
            }
            catch (Exception e)
            {
                _lastError = e.GetType().Name;
                return false;
            }
        }

        private void EnsureReady()
        {
            if (_checked)
            {
                return;
            }

            _checked = true;

#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                _bridgeClass = new AndroidJavaClass(JavaClassName);
                _available = _bridgeClass != null && _bridgeClass.CallStatic<bool>("isReady");
                if (!_available)
                {
                    _lastError = "isReady=false";
                }
            }
            catch (Exception e)
            {
                _available = false;
                _lastError = e.GetType().Name;
            }
#else
            _available = false;
            _lastError = "AndroidOnly";
#endif
        }

        [Serializable]
        private sealed class DetectionDto
        {
            public bool ok;
            public int id;
            public float tx;
            public float ty;
            public float tz;
            public float qx;
            public float qy;
            public float qz;
            public float qw = 1f;
            public float confidence = 0.5f;
        }
    }
}
