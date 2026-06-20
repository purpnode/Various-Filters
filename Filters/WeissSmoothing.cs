using System.Numerics;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Tablet;

namespace OpenTabletDriver.SmoothingFilters.Filters
{
    [PluginName("purpnode's Weiss Smoothing")]
    public class WeissSmoothing : SmoothingFilterBase
    {
        public static new readonly string[] PredictionModeOptions = SmoothingFilterBase.PredictionModeOptions;
        public static new readonly string[] ACModeOptions = SmoothingFilterBase.ACModeOptions;
        private Vector2 _predicted;
        private Vector2 _velocity;
        private Vector2 _accel;
        private DateTime _lastTime;
        private bool _initialized;

        protected override Vector2 FilterPosition(Vector2 pos)
        {
            if (!_initialized)
            {
                _predicted = pos;
                _velocity = Vector2.Zero;
                _accel = Vector2.Zero;
                _lastTime = DateTime.UtcNow;
                _initialized = true;
                return pos;
            }

            var now = DateTime.UtcNow;
            var dt = (float)(now - _lastTime).TotalSeconds;
            _lastTime = now;

            if (dt <= 0f || dt > 0.1f)
                return pos;

            // Predict step
            var predicted = _predicted + _velocity * dt + 0.5f * _accel * dt * dt;

            // Update step — blend prediction with measurement
            var innovation = pos - predicted;
            var gain = SmoothingGain;
            _predicted = predicted + innovation * gain;

            // Update velocity and acceleration
            var prevVel = _velocity;
            _velocity = _velocity + _accel * dt + innovation * (VelocityGain / dt);
            _accel = _accel + (_velocity - prevVel) * AccelDecay;

            return _predicted;
        }

        [SliderProperty("Smoothing Gain", 0.01f, 1f, 0.15f)]
        [DefaultPropertyValue(0.15f)]
        [ToolTip("Prediction correction gain.\n" +
            "Higher = trusts raw input more, more responsive.\n" +
            "Lower = trusts motion model more, smoother.\n" +
            "0.15 is a balanced default.")]
        public float SmoothingGain { get; set; } = 0.15f;

        [SliderProperty("Velocity Gain", 0.001f, 1f, 0.05f)]
        [DefaultPropertyValue(0.05f)]
        [ToolTip("How fast velocity is updated by the innovation.\n" +
            "Higher = faster adaptation to speed changes.\n" +
            "Lower = more stable velocity estimate, slower response.\n" +
            "Should be lower than Smoothing Gain.")]
        public float VelocityGain { get; set; } = 0.05f;

        [SliderProperty("Accel Decay", 0.001f, 0.5f, 0.02f)]
        [DefaultPropertyValue(0.02f)]
        [ToolTip("Acceleration decay rate.\n" +
            "Higher = acceleration resets faster, less inertia.\n" +
            "Lower = more inertia, smoother but may drift.")]
        public float AccelDecay { get; set; } = 0.02f;
    }
}
