// Team-developed source file for the Enhancing Augmented Reality-Based Competitive Sports Experience, and Teaching How
// Authoring team: Team-GRP01
// Purpose: Wraps spatial anchor creation, persistence, and synchronization workflows.
// Third-party adaptation: Yes (see SOURCE_ATTRIBUTION.md)

using System;
using System.Threading.Tasks;
using Unity.XR.PXR;
using UnityEngine;

namespace Project.MRWorld
{
        /// <summary>
    /// Wrapper around PICO Spatial Anchor synchronization capabilities.
    /// </summary>
    public sealed class SpatialAnchorSyncService
    {
        private const string LastSharedAnchorUuidKey = "project.last_shared_anchor_uuid";

        public async Task<bool> EnsureProviderReadyAsync()
        {
            try
            {
                var stateResult = PXR_MixedReality.GetSenseDataProviderState(PxrSenseDataProviderType.SpatialAnchor, out var state);
                if (stateResult == PxrResult.SUCCESS && state == PxrSenseDataProviderState.Running)
                {
                    return true;
                }

                var startResult = await PXR_MixedReality.StartSenseDataProvider(PxrSenseDataProviderType.SpatialAnchor);
                return startResult == PxrResult.SUCCESS;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"SpatialAnchor: EnsureProviderReady failed: {e.Message}");
                return false;
            }
        }

        public async Task<(bool ok, string uuid, Vector3 position, Quaternion rotation)> CreateOrReuseSharedAnchorAsync(Vector3 preferredPosition, Quaternion preferredRotation)
        {
            if (!await EnsureProviderReadyAsync())
            {
                return (false, string.Empty, Vector3.zero, Quaternion.identity);
            }

            var cachedUuid = LoadCachedSharedAnchorUuid();
            if (TryParseGuid(cachedUuid, out var cachedGuid))
            {
                var cachedPose = await TryLocateByUuidAsync(cachedGuid);
                if (cachedPose.ok)
                {
                    Debug.Log($"SpatialAnchor: Reuse cached anchor uuid={cachedUuid}");
                    return (true, cachedUuid, cachedPose.position, cachedPose.rotation);
                }
            }

            try
            {
                var create = await PXR_MixedReality.CreateSpatialAnchorAsync(preferredPosition, preferredRotation);
                if (create.result != PxrResult.SUCCESS || create.anchorHandle == 0 || create.uuid == Guid.Empty)
                {
                    Debug.LogWarning($"SpatialAnchor: Create failed result={create.result}");
                    return (false, string.Empty, Vector3.zero, Quaternion.identity);
                }

                var persist = await PXR_MixedReality.PersistSpatialAnchorAsync(create.anchorHandle);
                if (persist != PxrResult.SUCCESS)
                {
                    Debug.LogWarning($"SpatialAnchor: Persist failed result={persist}");
                    return (false, string.Empty, Vector3.zero, Quaternion.identity);
                }

                var upload = await PXR_MixedReality.UploadSpatialAnchorAsync(create.anchorHandle);
                if (upload.result != PxrResult.SUCCESS || upload.uuid == Guid.Empty)
                {
                    Debug.LogWarning($"SpatialAnchor: Upload failed result={upload.result}");
                    return (false, string.Empty, Vector3.zero, Quaternion.identity);
                }

                var uuidStr = upload.uuid.ToString();
                SaveCachedSharedAnchorUuid(uuidStr);
                Debug.Log($"SpatialAnchor: Created and uploaded uuid={uuidStr}");
                return (true, uuidStr, preferredPosition, preferredRotation);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"SpatialAnchor: CreateOrReuse failed: {e.Message}");
                return (false, string.Empty, Vector3.zero, Quaternion.identity);
            }
        }

        public async Task<(bool ok, Vector3 position, Quaternion rotation)> DownloadAndLocateSharedAnchorAsync(string uuid, int locateRetry = 40, int retryDelayMs = 80)
        {
            if (!TryParseGuid(uuid, out var guid))
            {
                return (false, Vector3.zero, Quaternion.identity);
            }

            if (!await EnsureProviderReadyAsync())
            {
                return (false, Vector3.zero, Quaternion.identity);
            }

            try
            {
                var download = await PXR_MixedReality.DownloadSharedSpatialAnchorAsync(guid);
                if (download != PxrResult.SUCCESS)
                {
                    Debug.LogWarning($"SpatialAnchor: Download failed result={download}");
                    return (false, Vector3.zero, Quaternion.identity);
                }

                var located = await TryLocateByUuidAsync(guid, locateRetry, retryDelayMs);
                if (located.ok)
                {
                    SaveCachedSharedAnchorUuid(uuid);
                    return located;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"SpatialAnchor: DownloadAndLocate failed: {e.Message}");
            }

            return (false, Vector3.zero, Quaternion.identity);
        }

        private static async Task<(bool ok, Vector3 position, Quaternion rotation)> TryLocateByUuidAsync(Guid uuid, int locateRetry = 25, int retryDelayMs = 60)
        {
            for (var i = 0; i < Mathf.Max(1, locateRetry); i++)
            {
                var query = await PXR_MixedReality.QuerySpatialAnchorAsync(new[] { uuid });
                if (query.result == PxrResult.SUCCESS && query.anchorHandleList != null && query.anchorHandleList.Count > 0)
                {
                    var anchorHandle = query.anchorHandleList[0];
                    var locateResult = PXR_MixedReality.LocateAnchor(anchorHandle, out var position, out var rotation);
                    if (locateResult == PxrResult.SUCCESS)
                    {
                        return (true, position, rotation);
                    }
                }

                await Task.Delay(Mathf.Max(20, retryDelayMs));
            }

            return (false, Vector3.zero, Quaternion.identity);
        }

        private static bool TryParseGuid(string value, out Guid guid)
        {
            guid = Guid.Empty;
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            return Guid.TryParse(value, out guid) && guid != Guid.Empty;
        }

        private static string LoadCachedSharedAnchorUuid()
        {
            return PlayerPrefs.GetString(LastSharedAnchorUuidKey, string.Empty);
        }

        private static void SaveCachedSharedAnchorUuid(string uuid)
        {
            if (string.IsNullOrEmpty(uuid))
            {
                return;
            }

            PlayerPrefs.SetString(LastSharedAnchorUuidKey, uuid);
            PlayerPrefs.Save();
        }
    }
}




