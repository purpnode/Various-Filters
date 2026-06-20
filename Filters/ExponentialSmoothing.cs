using System.Numerics;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Tablet;

namespace OpenTabletDriver.SmoothingFilters.Filters
{
    [PluginName("purpnode's Exponential Smoothing")]
    public class ExponentialSmoothing : SmoothingFilterBase
    {
        public static new readonly string[] PredictionModeOptions = SmoothingFilterBase.PredictionModeOptions;
        public static new readonly string[] ACModeOptions = SmoothingFilterBase.ACModeOptions;

        private Vector2 _smoothedPos;
        private float _lastPressure;
        private bool _initialized;

        protected override Vector2 FilterPosition(Vector2 pos)
        {
            if (!_initialized)
            {
                _smoothedPos = pos;
                _initialized = true;
                return pos;
            }

            var alpha = SmoothingFactor / 100f;
            _smoothedPos.X += (pos.X - _smoothedPos.X) * alpha;
            _smoothedPos.Y += (pos.Y - _smoothedPos.Y) * alpha;
            return _smoothedPos;
        }

        protected override float FilterPressure(float pressure)
        {
            if (!_initialized)
            {
                _lastPressure = pressure;
                return pressure;
            }

            var alpha = SmoothingFactor / 100f;
            _lastPressure += (pressure - _lastPressure) * alpha;
            return _lastPressure;
        }

        [SliderProperty("Smoothing Factor", 1f, 100f, 30f)]
        [DefaultPropertyValue(30f)]
        [ToolTip("Exponential smoothing coefficient in percent.\n" +
            "Higher (60-100) = more responsive, less smoothing but may retain jitter.\n" +
            "Lower (1-30) = very smooth but adds noticeable lag.\n" +
            "30% is a balanced starting point for most users.\n" +
            "Applies to both cursor position and pen pressure.")]
        [Unit("%")]
        public float SmoothingFactor { get; set; } = 30f;
    }
}
