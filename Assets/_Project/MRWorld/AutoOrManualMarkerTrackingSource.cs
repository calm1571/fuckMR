// Team-developed source file for the Enhancing Augmented Reality-Based Competitive Sports Experience, and Teaching How
// Authoring team: Team-GRP01
// Purpose: Switches between automatic and manual marker tracking sources.
// Third-party adaptation: No (see SOURCE_ATTRIBUTION.md)

namespace Project.MRWorld
{
    // Prefer auto AprilTag source; fallback to manual source when auto has no valid pose.
        /// <summary>
    /// 自动识别与手动识别之间的组合 Marker 数据源。
    /// </summary>
    public sealed class AutoOrManualMarkerTrackingSource : IMarkerTrackingSource
    {
        private readonly IMarkerTrackingSource _auto;
        private readonly IMarkerTrackingSource _manual;
        private bool _usingAuto;

        public AutoOrManualMarkerTrackingSource(IMarkerTrackingSource auto, IMarkerTrackingSource manual)
        {
            _auto = auto;
            _manual = manual;
        }

        public void Begin()
        {
            _usingAuto = false;
            _auto?.Begin();
            _manual?.Begin();
        }

        public void End()
        {
            _auto?.End();
            _manual?.End();
            _usingAuto = false;
        }

        public void Tick(float deltaTime)
        {
            _auto?.Tick(deltaTime);
            _manual?.Tick(deltaTime);
        }

        public bool TryGetSample(out MarkerTrackingSample sample)
        {
            if (_auto != null && _auto.TryGetSample(out var autoSample) && autoSample.hasPose)
            {
                _usingAuto = true;
                sample = autoSample;
                return true;
            }

            if (_manual != null && _manual.TryGetSample(out var manualSample) && manualSample.hasPose)
            {
                _usingAuto = false;
                sample = manualSample;
                return true;
            }

            sample = default;
            return false;
        }

        public string BuildDebugText()
        {
            var mode = _usingAuto ? "AUTO" : "MANUAL";
            var autoText = _auto != null ? _auto.BuildDebugText() : "Auto: null";
            var manualText = _manual != null ? _manual.BuildDebugText() : "Manual: null";
            return $"Marker Mode: {mode}\n{autoText}\nFallback:\n{manualText}";
        }
    }
}



