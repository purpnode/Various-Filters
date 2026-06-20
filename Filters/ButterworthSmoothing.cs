using System.Numerics;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Tablet;

namespace OpenTabletDriver.SmoothingFilters.Filters
{
    [PluginName("purpnode's Butterworth Smoothing")]
    public class ButterworthSmoothing : SmoothingFilterBase
    {
        public static new readonly string[] PredictionModeOptions = SmoothingFilterBase.PredictionModeOptions;
        public static new readonly string[] ACModeOptions = SmoothingFilterBase.ACModeOptions;
        private Vector2 _x1, _x2;
        private Vector2 _y1, _y2;
        private DateTime _lastTime;
        private bool _initialized;

        protected override Vector2 FilterPosition(Vector2 pos)
        {
            if (!_initialized)
            {
                _x1 = _x2 = pos;
                _y1 = _y2 = pos;
                _lastTime = DateTime.UtcNow;
                _initialized = true;
                return pos;
            }

            var now = DateTime.UtcNow;
            var dt = (float)(now - _lastTime).TotalSeconds;
            _lastTime = now;

            if (dt <= 0f || dt > 0.1f)
                return pos;

            var fc = CutoffHz;
            var w0 = 2f * MathF.PI * fc;
            var alpha = MathF.Sin(w0 * dt) * 0.70710678f; // Q = 1/sqrt(2) for Butterworth

            var a0 = 1f + alpha;
            var a1 = -2f * MathF.Cos(w0 * dt) / a0;
            var a2 = (1f - alpha) / a0;
            var b0 = (1f - MathF.Cos(w0 * dt)) / (2f * a0);
            var b1 = (1f - MathF.Cos(w0 * dt)) / a0;
            var b2 = b0;

            var y = b0 * pos + b1 * _x1 + b2 * _x2 - a1 * _y1 - a2 * _y2;

            _x2 = _x1;
            _x1 = pos;
            _y2 = _y1;
            _y1 = y;

            return y;
        }

        [SliderProperty("Cutoff Frequency", 1f, 200f, 30f)]
        [DefaultPropertyValue(30f)]
        [Unit("hz")]
        [ToolTip("Butterworth filter cutoff frequency (Hz).\n" +
            "Lower = stronger smoothing, more latency.\n" +
            "Higher = more responsive, less smoothing.\n" +
            "2nd-order filter with maximally flat passband response.\n" +
            "Recommended: 15-50 Hz for drawing.")]
        public float CutoffHz { get; set; } = 30f;
    }
}
