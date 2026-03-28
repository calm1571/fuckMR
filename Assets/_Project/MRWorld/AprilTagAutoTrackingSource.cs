// Team-developed source file for the Enhancing Augmented Reality-Based Competitive Sports Experience, and Teaching How
// Authoring team: Team-GRP01
// Purpose: Produces marker tracking samples from automatic AprilTag detection.
// Third-party adaptation: No (see SOURCE_ATTRIBUTION.md)

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.XR.PICO.TOBSupport;
using Unity.XR.PXR;
using UnityEngine;
using UnityEngine.XR;
using Pose = UnityEngine.Pose;

namespace Project.MRWorld
{
    // Auto marker tracking using PICO readable camera frames + pluggable AprilTag detector.
        /// <summary>
    /// Produces marker poses from AprilTag detection results.
    /// </summary>
    public sealed class AprilTagAutoTrackingSource : IMarkerTrackingSource
    {
        private readonly AprilTagTrackingConfig _config;
        private readonly IAprilTagDetector _detector;
        private readonly XRNode _lockInputNode;
        private readonly float _smoothLerp;
        private readonly float _stabilityLerp;
        private readonly float _stableDistanceThreshold;

        private readonly object _poseLock = new object();

        private InputDevice _lockInputDevice;
        private bool _xPrev;
        private bool _yPrev;
        private bool _isLocked;

        private bool _cameraOpenRequested;
        private bool _cameraOpened;
        private bool _streamStarted;
        private bool _hasPose;
        private bool _hasFrame;
        private float _stability01;
        private string _cameraStatus = "Idle";

        private Pose _smoothedMarkerWorldPose;
        private Pose _lastRawMarkerWorldPose;
        private Pose _latestCameraHeadPose;
        private float _latestConfidence;
        private long _latestTimestamp;
        private Pose _cameraLocalPose;
        private bool _hasCameraLocalPose;
        private AprilTagCameraIntrinsics _cameraIntrinsics;

        private byte[] _frameBytes;
        private GCHandle _framePin;
        private IntPtr _framePtr;

        public AprilTagAutoTrackingSource(
            AprilTagTrackingConfig config,
            IAprilTagDetector detector,
            XRNode lockInputNode = XRNode.LeftHand,
            float smoothLerp = 8f,
            float stabilityLerp = 0.2f,
            float stableDistanceThreshold = 0.015f)
        {
            _config = config;
            _detector = detector;
            _lockInputNode = lockInputNode;
            _smoothLerp = Mathf.Max(0.5f, smoothLerp);
            _stabilityLerp = Mathf.Clamp01(stabilityLerp);
            _stableDistanceThreshold = Mathf.Clamp(stableDistanceThreshold, 0.003f, 0.2f);
        }

        public void Begin()
        {
            _hasPose = false;
            _hasFrame = false;
            _isLocked = false;
            _stability01 = 0f;
            _latestConfidence = 0f;
            _latestTimestamp = 0;
            _hasCameraLocalPose = false;
            _cameraIntrinsics = new AprilTagCameraIntrinsics(0f, 0f, 0f, 0f);
            _cameraStatus = "Starting";
            _lockInputDevice = InputDevices.GetDeviceAtXRNode(_lockInputNode);
            _xPrev = false;
            _yPrev = false;
            StartCameraStream();
        }

        public void End()
        {
            StopCameraStream();
        }

        public void Tick(float deltaTime)
        {
            EnsureLockInputDevice();

            var xPressed = false;
            var yPressed = false;
            if (_lockInputDevice.isValid)
            {
                _lockInputDevice.TryGetFeatureValue(CommonUsages.primaryButton, out xPressed);
                _lockInputDevice.TryGetFeatureValue(CommonUsages.secondaryButton, out yPressed);
            }

            var xDown = xPressed && !_xPrev;
            var yDown = yPressed && !_yPrev;
            _xPrev = xPressed;
            _yPrev = yPressed;

            if (xDown && _hasPose)
            {
                _isLocked = true;
            }

            if (yDown)
            {
                _isLocked = false;
            }

            if (!_isLocked && _hasPose)
            {
                // Keep smoothing active while unlocked to suppress frame jitter.
                lock (_poseLock)
                {
                    var t = 1f - Mathf.Exp(-_smoothLerp * Mathf.Max(0.0001f, deltaTime));
                    _smoothedMarkerWorldPose.position = Vector3.Lerp(_smoothedMarkerWorldPose.position, _lastRawMarkerWorldPose.position, t);
                    _smoothedMarkerWorldPose.rotation = Quaternion.Slerp(_smoothedMarkerWorldPose.rotation, _lastRawMarkerWorldPose.rotation, t);
                }
            }
        }

        public bool TryGetSample(out MarkerTrackingSample sample)
        {
            lock (_poseLock)
            {
                sample = new MarkerTrackingSample
                {
                    hasPose = _hasPose,
                    isLocked = _isLocked,
                    stability01 = _stability01,
                    pose = _smoothedMarkerWorldPose,
                    sourceMode = MarkerTrackingSourceMode.AutoAprilTag
                };
                return _hasPose;
            }
        }

        public string BuildDebugText()
        {
            var detectorName = _detector != null ? _detector.DebugName : "null";
            var detectorState = _detector != null && _detector.IsAvailable ? "Ready" : "Unavailable";
            var poseState = _hasPose ? "Pose Ready" : "Pose Missing";
            var lockState = _isLocked ? "Locked" : "Unlocked";
            var ts = _latestTimestamp > 0 ? _latestTimestamp.ToString() : "-";
            return
                $"Auto AprilTag: {poseState} | {lockState}\n" +
                $"Camera: {_cameraStatus} | Detector: {detectorState}\n" +
                $"Tag: {_config.family}:{_config.id} Size:{_config.tagSizeMeters * 1000f:F0}mm\n" +
                $"Stability:{_stability01 * 100f:F0}% Confidence:{_latestConfidence * 100f:F0}% Timestamp:{ts}\n" +
                $"DetectorImpl: {detectorName}\n" +
                $"X Lock / Y Unlock";
        }

        private void StartCameraStream()
        {
            if (_cameraOpenRequested)
            {
                return;
            }

            _cameraOpenRequested = true;

            try
            {
                var cameraConfig = new Dictionary<string, string>
                {
                    { PXRCapture.KEY_OUTPUT_CAMERA_RAW_DATA, PXRCapture.VALUE_FALSE },
                    { PXRCapture.KEY_MCTF, PXRCapture.VALUE_TRUE },
                    { PXRCapture.KEY_EIS, PXRCapture.VALUE_FALSE },
                    { PXRCapture.KEY_MFNR, PXRCapture.VALUE_FALSE }
                };
                PXR_Enterprise.Configurefor4U(cameraConfig);
                PXR_Enterprise.OpenCameraAsyncfor4U(OnCameraOpenResult, cameraConfig);
                _cameraStatus = "OpenRequested";
            }
            catch (Exception e)
            {
                _cameraStatus = $"OpenError:{e.GetType().Name}";
                _cameraOpenRequested = false;
            }
        }

        private void OnCameraOpenResult(bool success)
        {
            _cameraOpened = success;
            if (!success)
            {
                _cameraStatus = "OpenFailed";
                return;
            }

            if (_frameBytes == null || _frameBytes.Length != _config.frameWidth * _config.frameHeight * 4)
            {
                _frameBytes = new byte[_config.frameWidth * _config.frameHeight * 4];
            }

            if (_framePin.IsAllocated)
            {
                _framePin.Free();
            }

            _framePin = GCHandle.Alloc(_frameBytes, GCHandleType.Pinned);
            _framePtr = _framePin.AddrOfPinnedObject();
            PXR_Enterprise.SetCameraFrameBufferfor4U(_config.frameWidth, _config.frameHeight, ref _framePtr, OnFrameAvailable);
            _streamStarted = PXR_Enterprise.StartGetImageDatafor4U(PXRCaptureRenderMode.PXRCapture_RenderMode_LEFT, _config.frameWidth, _config.frameHeight);
            CacheCameraLocalPose();
            _cameraStatus = _streamStarted ? "Streaming" : "StreamStartFailed";
        }

        private void OnFrameAvailable(Frame frame)
        {
            _hasFrame = true;
            _latestTimestamp = (long)frame.timestamp;
            _latestCameraHeadPose = frame.pose;

            if (_detector == null || !_detector.IsAvailable || frame.data == IntPtr.Zero)
            {
                return;
            }

            var detected = _detector.TryDetect(
                frame.data,
                _config.frameWidth,
                _config.frameHeight,
                _config.frameWidth * 4,
                _cameraIntrinsics,
                _config,
                out var result);
            if (!detected || !result.hasPose || result.id != _config.id)
            {
                lock (_poseLock)
                {
                    _hasPose = false;
                    _stability01 = Mathf.Lerp(_stability01, 0f, _stabilityLerp);
                }
                return;
            }

            var markerWorldPose = ToWorldPose(frame.pose, result.tagPoseInCamera);
            lock (_poseLock)
            {
                if (!_hasPose)
                {
                    _lastRawMarkerWorldPose = markerWorldPose;
                    _smoothedMarkerWorldPose = markerWorldPose;
                    _hasPose = true;
                    _stability01 = 0f;
                }
                else
                {
                    var movement = Vector3.Distance(_lastRawMarkerWorldPose.position, markerWorldPose.position);
                    var instantStability = Mathf.Clamp01(1f - movement / _stableDistanceThreshold);
                    _stability01 = Mathf.Lerp(_stability01, instantStability, _stabilityLerp);
                    _lastRawMarkerWorldPose = markerWorldPose;

                    if (!_isLocked)
                    {
                        _smoothedMarkerWorldPose = markerWorldPose;
                    }
                }
            }

            _latestConfidence = result.confidence;
        }

        private Pose ToWorldPose(Pose headPose, Pose tagPoseInCamera)
        {
            var cameraWorldPose = Compose(headPose, _hasCameraLocalPose ? _cameraLocalPose : Pose.identity);
            return Compose(cameraWorldPose, tagPoseInCamera);
        }

        private static Pose Compose(Pose a, Pose b)
        {
            return new Pose(a.position + a.rotation * b.position, a.rotation * b.rotation);
        }

        private void StopCameraStream()
        {
            try
            {
                if (_cameraOpened)
                {
                    PXR_Enterprise.CloseCamerafor4U();
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"AprilTagAutoTrackingSource.CloseCamerafor4U failed: {e.GetType().Name}");
            }

            _cameraOpened = false;
            _streamStarted = false;
            _cameraOpenRequested = false;
            _cameraStatus = "Stopped";
            _hasFrame = false;
            _hasPose = false;
            _latestConfidence = 0f;
            _latestTimestamp = 0;
            _hasCameraLocalPose = false;

            if (_framePin.IsAllocated)
            {
                _framePin.Free();
            }

            _framePtr = IntPtr.Zero;
        }

        private void EnsureLockInputDevice()
        {
            if (!_lockInputDevice.isValid)
            {
                _lockInputDevice = InputDevices.GetDeviceAtXRNode(_lockInputNode);
            }
        }

        private void CacheCameraLocalPose()
        {
            try
            {
                var cameraParam = PXR_Enterprise.GetCameraParametersNewfor4U(_config.frameWidth, _config.frameHeight);
                _cameraLocalPose = new Pose(cameraParam.l_pos, cameraParam.l_rot);
                _cameraIntrinsics = new AprilTagCameraIntrinsics(
                    (float)cameraParam.fx,
                    (float)cameraParam.fy,
                    (float)cameraParam.cx,
                    (float)cameraParam.cy);
                _hasCameraLocalPose = true;
            }
            catch
            {
                _cameraLocalPose = Pose.identity;
                _cameraIntrinsics = new AprilTagCameraIntrinsics(0f, 0f, 0f, 0f);
                _hasCameraLocalPose = false;
            }
        }
    }
}




