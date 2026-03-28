// Team-developed source file for the Enhancing Augmented Reality-Based Competitive Sports Experience, and Teaching How
// Authoring team: Team-GRP01
// Purpose: Adds required Android camera permission settings during build post-processing.
// Third-party adaptation: No (see SOURCE_ATTRIBUTION.md)

using System.IO;
using System.Xml;
using UnityEditor;
using UnityEditor.Android;
using UnityEngine;

namespace Project.Tools.Editor
{
        /// <summary>
    /// Android 构建后处理：补充相机权限相关配置。
    /// </summary>
    public sealed class AndroidManifestCameraPermissionPostprocessor : IPostGenerateGradleAndroidProject
    {
        public int callbackOrder => 9999;

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            TryAddCameraPermission(Path.Combine(path, "..", "launcher", "src", "main", "AndroidManifest.xml"));
            TryAddCameraPermission(Path.Combine(path, "src", "main", "AndroidManifest.xml"));
        }

        private static void TryAddCameraPermission(string manifestPath)
        {
            var fullPath = Path.GetFullPath(manifestPath);
            if (!File.Exists(fullPath))
            {
                return;
            }

            var doc = new XmlDocument();
            doc.Load(fullPath);

            var manifest = doc.SelectSingleNode("/manifest") as XmlElement;
            if (manifest == null)
            {
                Debug.LogWarning($"Manifest postprocess skipped, root node missing: {fullPath}");
                return;
            }

            var androidNs = "http://schemas.android.com/apk/res/android";
            var exists = false;
            var nodeList = manifest.SelectNodes("uses-permission");
            if (nodeList != null)
            {
                foreach (XmlNode node in nodeList)
                {
                    if (node is XmlElement element &&
                        element.GetAttribute("name", androidNs) == "android.permission.CAMERA")
                    {
                        exists = true;
                        break;
                    }
                }
            }

            if (exists)
            {
                return;
            }

            var permission = doc.CreateElement("uses-permission");
            permission.SetAttribute("name", androidNs, "android.permission.CAMERA");
            manifest.AppendChild(permission);
            doc.Save(fullPath);
            Debug.Log($"Added CAMERA permission to manifest: {fullPath}");
        }
    }
}



