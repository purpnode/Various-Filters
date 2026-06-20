using System.Numerics;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Tablet;

namespace OpenTabletDriver.SmoothingFilters.Filters
{
    [PluginName("purpnode's Double Exp Smoothing")]
    public class DoubleExponentialSmoothing : SmoothingFilterBase
    {
        public static new readonly string[] PredictionModeOptions = SmoothingFilterBase.PredictionModeOptions;
        public static new readonly string[] ACModeOptions = SmoothingFilterBase.ACModeOptions;
        private Vector2 _level;
        private Vector2 _trend;
        private bool _initialized;

        protected override Vector2 FilterPosition(Vector2 pos)
        {
            if (!_initialized)
            {
                _level = pos;
                _trend = Vector2.Zero;
                _initialized = true;
                return pos;
            }

            var prevLevel = _level;

            _level = Alpha * pos + (1f - Alpha) * (_level + _trend);
            _trend = Beta * (_level - prevLevel) + (1f - Beta) * _trend;

            return _level + _trend * (1f + ForecastSteps);
        }

        [SliderProperty("Alpha (Level)", 0.01f, 1f, 0.3f)]
        [DefaultPropertyValue(0.3f)]
        [ToolTip("Level smoothing coefficient (alpha).\n" +
            "Higher = trusts new points more, responsive but less smooth.\n" +
            "Lower = stronger smoothing, more lag.\n" +
            "0.3 is a good starting value for drawing.")]
        public float Alpha { get; set; } = 0.3f;

        [SliderProperty("Beta (Trend)", 0.001f, 0.5f, 0.05f)]
        [DefaultPropertyValue(0.05f)]
        [ToolTip("Trend smoothing coefficient (beta).\n" +
            "Higher = trend adapts faster to direction changes.\n" +
            "Lower = trend is more stable, less jitter.\n" +
            "Should be significantly lower than alpha.\n" +
            "0.05 is a good default.")]
        public float Beta { get; set; } = 0.05f;

        [SliderProperty("Forecast Steps", 0f, 5f, 0f)]
        [DefaultPropertyValue(0f)]
        [ToolTip("Additional trend-based prediction steps.\n" +
            "0 = no extra prediction (pure Holt smoothing).\n" +
            "1-3 = forward extrapolation along the trend to reduce latency.\n" +
            "Higher = less lag, but possible overshoot.")]
        public float ForecastSteps { get; set; } = 0f;
    }
}
