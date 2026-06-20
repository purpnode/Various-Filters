using System.Numerics;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Tablet;

namespace OpenTabletDriver.SmoothingFilters.Filters
{
    [PluginName("purpnode's Pressure Smoothing")]
    public class PressureSmoothing : SmoothingFilterBase
    {
        public static new readonly string[] PredictionModeOptions = SmoothingFilterBase.PredictionModeOptions;
        public static new readonly string[] ACModeOptions = SmoothingFilterBase.ACModeOptions;

        private float _smoothedPressure;
        private bool _initialized;

        protected override Vector2 FilterPosition(Vector2 pos)
        {
            return pos;
        }

        protected override float FilterPressure(float pressure)
        {
            if (!_initialized)
            {
                _smoothedPressure = pressure;
                _initialized = true;
                return pressure;
            }

            var alpha = SmoothingFactor / 100f;
            _smoothedPressure += (pressure - _smoothedPressure) * alpha;

            if (Math.Abs(pressure - _smoothedPressure) < Deadzone)
                _smoothedPressure = pressure;

            return _smoothedPressure;
        }

        [SliderProperty("Smoothing Factor", 1f, 100f, 40f)]
        [DefaultPropertyValue(40f)]
        [ToolTip("Pressure smoothing strength in percent.\n" +
            "High (60-100) = responsive pressure, less smoothing.\n" +
            "Low (1-30) = smooths pressure jitter but feels sluggish.\n" +
            "Useful for feathering or controlling ink flow.\n" +
            "40% is a balanced default for most users.")]
        [Unit("%")]
        public float SmoothingFactor { get; set; } = 40f;

        [SliderProperty("Deadzone", 0f, 100f, 3f)]
        [DefaultPropertyValue(3f)]
        [ToolTip("Pressure snap deadzone in levels (0-65535).\n" +
            "When smoothed pressure is within this distance from raw,\n" +
            "output snaps directly to the raw value for zero-lag response.\n" +
            "Higher = small changes feel instant, but may cause slight\n" +
            "inconsistency at the threshold boundary.\n" +
            "Lower = smoother transitions, slightly more latency on small changes.")]
        [Unit("levels")]
        public float Deadzone { get; set; } = 3f;
    }
}
