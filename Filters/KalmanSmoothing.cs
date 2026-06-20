using System.Numerics;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Tablet;

namespace OpenTabletDriver.SmoothingFilters.Filters
{
    [PluginName("purpnode's Kalman Smoothing")]
    public class KalmanSmoothing : SmoothingFilterBase
    {
        public static new readonly string[] PredictionModeOptions = SmoothingFilterBase.PredictionModeOptions;
        public static new readonly string[] ACModeOptions = SmoothingFilterBase.ACModeOptions;

        private Vector2 _x;
        private Vector2 _v;
        private Vector2 _p;
        private DateTime _lastTime;
        private bool _initialized;

        protected override Vector2 FilterPosition(Vector2 pos)
        {
            if (!_initialized)
            {
                _x = pos;
                _v = Vector2.Zero;
                _p = new Vector2(1000f, 1000f);
                _lastTime = DateTime.UtcNow;
                _initialized = true;
                return pos;
            }

            var now = DateTime.UtcNow;
            var dt = (float)(now - _lastTime).TotalSeconds;
            _lastTime = now;

            if (dt > 0f && dt < 1f)
            {
                var q = ProcessNoise * dt * dt;
                var r = MeasureNoise;
                var d = Math.Clamp(dt, 0.001f, 0.1f);

                _x += _v * d;
                _p += new Vector2(q, q);

                var k = new Vector2(
                    _p.X / (_p.X + r),
                    _p.Y / (_p.Y + r)
                );

                var innov = new Vector2(pos.X - _x.X, pos.Y - _x.Y);
                _x += new Vector2(innov.X * k.X, innov.Y * k.Y);
                _v += new Vector2(innov.X * k.X / d, innov.Y * k.Y / d);
                _p = new Vector2(_p.X * (1f - k.X), _p.Y * (1f - k.Y));
            }

            return _x;
        }

        [SliderProperty("Process Noise", 0.01f, 10f, 0.5f)]
        [DefaultPropertyValue(0.5f)]
        [ToolTip("How much the Kalman filter trusts its motion model vs new measurements.\n" +
            "Low (0.01-0.5) = trusts the model more, smoother output with more lag.\n" +
            "High (1-10) = trusts new measurements more, responsive but less smooth.\n" +
            "Essentially the smoothness vs responsiveness trade-off knob.")]
        public float ProcessNoise { get; set; } = 0.5f;

        [SliderProperty("Measurement Noise", 0.01f, 10f, 2f)]
        [DefaultPropertyValue(2f)]
        [ToolTip("Assumed noise level of the raw pen input.\n" +
            "High = filter is skeptical of each new sample, aggressive smoothing.\n" +
            "Low = trusts samples more, faster response but may pass noise through.\n" +
            "Start at 2.0. Raise for jittery pens, lower for clean input.")]
        public float MeasureNoise { get; set; } = 2f;
    }
}
