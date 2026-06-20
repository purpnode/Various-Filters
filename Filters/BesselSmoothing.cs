using System.Numerics;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Tablet;

namespace OpenTabletDriver.SmoothingFilters.Filters
{
    [PluginName("purpnode's Bessel Smoothing")]
    public class BesselSmoothing : SmoothingFilterBase
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
            var theta = w0 * dt;

            // Bessel 2nd-order coefficients are designed for linear phase:
            // b0 = 3, b1 = 0, b2 = 0
            // a0 = 3 + 3*theta + theta^2
            // a1 = -6 + 6*theta + 2*theta^2
            // a2 = 3 - 3*theta + theta^2
            // But we need to normalize.

            var besselA0 = 3f;
            var besselA1 = 3f * theta;
            var besselA2 = theta * theta + besselA0 + besselA1; // 3 + 3*theta + theta^2

            var a0 = besselA0 + besselA1 + besselA2;
            var a1 = (-2f * besselA0 - besselA1 + besselA2) / a0;
            var a2 = (besselA0 - besselA1 + besselA2) / a0;
            var b0_val = besselA2 / a0;
            var b1_val = 2f * besselA2 / a0;
            var b2_val = b0_val;

            var y = b0_val * pos + b1_val * _x1 + b2_val * _x2 - a1 * _y1 - a2 * _y2;

            _x2 = _x1;
            _x1 = pos;
            _y2 = _y1;
            _y1 = y;

            return y;
        }

        [SliderProperty("Cutoff Frequency", 1f, 200f, 25f)]
        [DefaultPropertyValue(25f)]
        [Unit("hz")]
        [ToolTip("Bessel filter cutoff frequency (Hz).\n" +
            "Lower = stronger smoothing, more latency.\n" +
            "Higher = more responsive, less smoothing.\n" +
            "2nd-order filter with linear phase response — same delay at all frequencies.\n" +
            "Preserves signal shape better than Butterworth.\n" +
            "Recommended: 10-40 Hz for drawing.")]
        public float CutoffHz { get; set; } = 25f;
    }
}
