// Team-developed source file for the Enhancing Augmented Reality-Based Competitive Sports Experience, and Teaching How
// Authoring team: Team-GRP01
// Purpose: Implements AprilTag detection through OpenCVForUnity.
// Third-party adaptation: Yes (see SOURCE_ATTRIBUTION.md)

using System;
using System.Collections.Generic;
using OpenCVForUnity.Calib3dModule;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.ObjdetectModule;
using UnityEngine;
using CvDictionary = OpenCVForUnity.ObjdetectModule.Dictionary;
using Mat = OpenCVForUnity.CoreModule.Mat;
using Point = OpenCVForUnity.CoreModule.Point;
using Point3 = OpenCVForUnity.CoreModule.Point3;

namespace Project.MRWorld
{
    // AprilTag detector based on OpenCVForUnity (Objdetect+Aruco APIs).
        /// <summary>
    /// AprilTag detection implementation based on OpenCVForUnity.
    /// </summary>
    public sealed class OpenCVForUnityAprilTagDetector : IAprilTagDetector
    {
        private readonly Dictionary<int, CvDictionary> _dictionaryCache = new Dictionary<int, CvDictionary>();
        private bool _checked;
        private bool _available;
        private string _lastError;

        public bool IsAvailable
        {
            get
            {
                EnsureAvailabilityChecked();
                return _available;
            }
        }

        public string DebugName
        {
            get
            {
                EnsureAvailabilityChecked();
                return string.IsNullOrEmpty(_lastError) ? "OpenCVForUnity.Objdetect" : $"OpenCVForUnity.Objdetect ({_lastError})";
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
            EnsureAvailabilityChecked();
            if (!_available || rgbaBuffer == IntPtr.Zero || width <= 0 || height <= 0)
            {
                return false;
            }

            var dictId = ResolveDictionaryId(config.family);
            if (dictId < 0)
            {
                _lastError = $"UnsupportedFamily:{config.family}";
                return false;
            }

            try
            {
                var dictionary = GetDictionary(dictId);
                if (dictionary == null)
                {
                    _lastError = $"DictionaryUnavailable:{dictId}";
                    return false;
                }

                using (var rgba = new Mat(height, width, CvType.CV_8UC4, rgbaBuffer, strideBytes))
                using (var gray = new Mat())
                using (var ids = new Mat())
                using (var rejected = new Mat())
                using (var cameraMatrix = BuildCameraMatrix(intrinsics, width, height))
                using (var distCoeffs = new MatOfDouble(0, 0, 0, 0))
                using (var detectorParams = BuildDetectorParameters())
                using (var arucoDetector = new ArucoDetector(dictionary, detectorParams))
                {
                    Imgproc.cvtColor(rgba, gray, Imgproc.COLOR_RGBA2GRAY);

                    var corners = new List<Mat>(8);
                    arucoDetector.detectMarkers(gray, corners, ids);
                    if (ids.total() <= 0 || corners.Count == 0)
                    {
                        DisposeMats(corners);
                        return false;
                    }

                    var matchedCorner = FindCornerById(corners, ids, config.id);
                    if (matchedCorner == null)
                    {
                        DisposeMats(corners);
                        return false;
                    }

                    using (matchedCorner)
                    using (var corner4x1 = matchedCorner.reshape(2, 4))
                    using (var imagePoints = new MatOfPoint2f(corner4x1))
                    using (var objectPoints = BuildMarkerObjectPoints(config.tagSizeMeters))
                    using (var rvec = new Mat(1, 1, CvType.CV_64FC3))
                    using (var tvec = new Mat(1, 1, CvType.CV_64FC3))
                    {
                        var solved = Calib3d.solvePnP(objectPoints, imagePoints, cameraMatrix, distCoeffs, rvec, tvec, false, Calib3d.SOLVEPNP_ITERATIVE);
                        if (!solved)
                        {
                            DisposeMats(corners);
                            return false;
                        }

                        using (var rotMat = new Mat())
                        {
                            Calib3d.Rodrigues(rvec, rotMat);

                            var tv = new double[3];
                            var rvMat = new double[9];
                            tvec.get(0, 0, tv);
                            rotMat.get(0, 0, rvMat);

                            var positionCv = new Vector3((float)tv[0], (float)tv[1], (float)tv[2]);
                            var rotationCv = QuaternionFromRotationMatrix(rvMat);

                            // OpenCV camera coords: +X right, +Y down, +Z forward
                            // Unity camera-local coords: +X right, +Y up, +Z forward
                            var positionUnity = new Vector3(positionCv.x, -positionCv.y, positionCv.z);
                            var rotationUnity = ConvertCvToUnityRotation(rotationCv);
                            var confidence = ComputeCornerConfidence(imagePoints.toArray(), width, height);

                            result = new AprilTagDetectionResult(
                                hasPose: true,
                                id: config.id,
                                tagPoseInCamera: new Pose(positionUnity, rotationUnity),
                                confidence: confidence);
                        }
                    }

                    DisposeMats(corners);
                    return true;
                }
            }
            catch (Exception e)
            {
                _lastError = e.GetType().Name;
                _available = false;
                return false;
            }
        }

        private void EnsureAvailabilityChecked()
        {
            if (_checked)
            {
                return;
            }

            _checked = true;
            try
            {
                var version = OpenCVForUnity.CoreModule.Core.getVersionString();
                _available = !string.IsNullOrEmpty(version);
                if (!_available)
                {
                    _lastError = "VersionEmpty";
                }
            }
            catch (Exception e)
            {
                _available = false;
                _lastError = e.GetType().Name;
            }
        }

        private CvDictionary GetDictionary(int dictId)
        {
            if (_dictionaryCache.TryGetValue(dictId, out var cached) && cached != null)
            {
                return cached;
            }

            var dictionary = Objdetect.getPredefinedDictionary(dictId);
            if (dictionary != null)
            {
                _dictionaryCache[dictId] = dictionary;
            }

            return dictionary;
        }

        private static DetectorParameters BuildDetectorParameters()
        {
            var p = new DetectorParameters();
            p.set_cornerRefinementMethod(Objdetect.CORNER_REFINE_SUBPIX);
            p.set_cornerRefinementWinSize(5);
            p.set_cornerRefinementMaxIterations(30);
            p.set_useAruco3Detection(true);
            p.set_aprilTagQuadDecimate(1f);
            p.set_aprilTagQuadSigma(0f);
            p.set_aprilTagMinClusterPixels(5);
            p.set_aprilTagMaxNmaxima(10);
            p.set_aprilTagCriticalRad(10f * Mathf.Deg2Rad);
            p.set_aprilTagMaxLineFitMse(10f);
            p.set_aprilTagMinWhiteBlackDiff(5);
            p.set_aprilTagDeglitch(0);
            return p;
        }

        private static Mat BuildCameraMatrix(in AprilTagCameraIntrinsics intrinsics, int width, int height)
        {
            var fx = intrinsics.fx > 1f ? intrinsics.fx : width;
            var fy = intrinsics.fy > 1f ? intrinsics.fy : height;
            var cx = intrinsics.cx > 1f ? intrinsics.cx : width * 0.5f;
            var cy = intrinsics.cy > 1f ? intrinsics.cy : height * 0.5f;

            var cameraMatrix = new Mat(3, 3, CvType.CV_64FC1);
            cameraMatrix.put(0, 0, fx);
            cameraMatrix.put(0, 1, 0d);
            cameraMatrix.put(0, 2, cx);
            cameraMatrix.put(1, 0, 0d);
            cameraMatrix.put(1, 1, fy);
            cameraMatrix.put(1, 2, cy);
            cameraMatrix.put(2, 0, 0d);
            cameraMatrix.put(2, 1, 0d);
            cameraMatrix.put(2, 2, 1d);
            return cameraMatrix;
        }

        private static MatOfPoint3f BuildMarkerObjectPoints(float sizeMeters)
        {
            var half = sizeMeters * 0.5f;
            return new MatOfPoint3f(
                new Point3(-half, half, 0f),
                new Point3(half, half, 0f),
                new Point3(half, -half, 0f),
                new Point3(-half, -half, 0f));
        }

        private static Mat FindCornerById(List<Mat> corners, Mat ids, int targetId)
        {
            var count = Mathf.Min(corners.Count, (int)ids.total());
            if (count <= 0)
            {
                return null;
            }

            var idData = new int[count];
            ids.get(0, 0, idData);
            for (var i = 0; i < count; i++)
            {
                if (idData[i] == targetId)
                {
                    return corners[i];
                }
            }

            return null;
        }

        private static Quaternion QuaternionFromRotationMatrix(double[] m9)
        {
            if (m9 == null || m9.Length < 9)
            {
                return Quaternion.identity;
            }

            var m00 = m9[0];
            var m01 = m9[1];
            var m02 = m9[2];
            var m10 = m9[3];
            var m11 = m9[4];
            var m12 = m9[5];
            var m20 = m9[6];
            var m21 = m9[7];
            var m22 = m9[8];

            var trace = m00 + m11 + m22;
            float qw;
            float qx;
            float qy;
            float qz;
            if (trace > 0d)
            {
                var s = Math.Sqrt(trace + 1d) * 2d;
                qw = (float)(0.25d * s);
                qx = (float)((m21 - m12) / s);
                qy = (float)((m02 - m20) / s);
                qz = (float)((m10 - m01) / s);
            }
            else if (m00 > m11 && m00 > m22)
            {
                var s = Math.Sqrt(1d + m00 - m11 - m22) * 2d;
                qw = (float)((m21 - m12) / s);
                qx = (float)(0.25d * s);
                qy = (float)((m01 + m10) / s);
                qz = (float)((m02 + m20) / s);
            }
            else if (m11 > m22)
            {
                var s = Math.Sqrt(1d + m11 - m00 - m22) * 2d;
                qw = (float)((m02 - m20) / s);
                qx = (float)((m01 + m10) / s);
                qy = (float)(0.25d * s);
                qz = (float)((m12 + m21) / s);
            }
            else
            {
                var s = Math.Sqrt(1d + m22 - m00 - m11) * 2d;
                qw = (float)((m10 - m01) / s);
                qx = (float)((m02 + m20) / s);
                qy = (float)((m12 + m21) / s);
                qz = (float)(0.25d * s);
            }

            var q = new Quaternion(qx, qy, qz, qw);
            var sqrMag = (q.x * q.x) + (q.y * q.y) + (q.z * q.z) + (q.w * q.w);
            return sqrMag < 0.00001f ? Quaternion.identity : Quaternion.Normalize(q);
        }

        private static Quaternion ConvertCvToUnityRotation(Quaternion rotationCv)
        {
            var flipY = Matrix4x4.Scale(new Vector3(1f, -1f, 1f));
            var rCv = Matrix4x4.Rotate(rotationCv);
            var rUnity = flipY * rCv * flipY;
            return Quaternion.Normalize(rUnity.rotation);
        }

        private static float ComputeCornerConfidence(Point[] points, int width, int height)
        {
            if (points == null || points.Length < 4)
            {
                return 0f;
            }

            var area = Math.Abs(PolygonArea(points));
            var full = Math.Max(1d, width * height);
            var areaRatio = area / full;
            return (float)Math.Min(1d, Math.Max(0.2d, areaRatio * 24d));
        }

        private static double PolygonArea(Point[] points)
        {
            var sum = 0d;
            for (var i = 0; i < points.Length; i++)
            {
                var j = (i + 1) % points.Length;
                sum += points[i].x * points[j].y - points[j].x * points[i].y;
            }

            return 0.5d * sum;
        }

        private static int ResolveDictionaryId(string family)
        {
            var key = string.IsNullOrWhiteSpace(family) ? "36h11" : family.Trim().ToLowerInvariant();
            switch (key)
            {
                case "16h5":
                case "apriltag_16h5":
                    return Objdetect.DICT_APRILTAG_16h5;
                case "25h9":
                case "apriltag_25h9":
                    return Objdetect.DICT_APRILTAG_25h9;
                case "36h10":
                case "apriltag_36h10":
                    return Objdetect.DICT_APRILTAG_36h10;
                case "36h11":
                case "apriltag_36h11":
                    return Objdetect.DICT_APRILTAG_36h11;
                default:
                    return -1;
            }
        }

        private static void DisposeMats(List<Mat> mats)
        {
            if (mats == null)
            {
                return;
            }

            for (var i = 0; i < mats.Count; i++)
            {
                mats[i]?.Dispose();
            }
            mats.Clear();
        }
    }
}




