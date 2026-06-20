using System.Numerics;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Tablet;

namespace OpenTabletDriver.SmoothingFilters.Filters
{
    [PluginName("purpnode's One Euro Smoothing")]
    public class OneEuroSmoothing : SmoothingFilterBase
    {
        public static new readonly string[] PredictionModeOptions = SmoothingFilterBase.PredictionModeOptions;
        public static new readonly string[] ACModeOptions = SmoothingFilterBase.ACModeOptions;

        private Vector2 _filteredPos;
        private DateTime _lastTime;
        private bool _initialized;

        protected override Vector2 FilterPosition(Vector2 pos)
        {
            if (!_initialized)
            {
                _filteredPos = pos;
                _lastTime = DateTime.UtcNow;
                _initialized = true;
                return pos;
            }

            var now = DateTime.UtcNow;
            var dt = (float)(now - _lastTime).TotalSeconds;
            _lastTime = now;

            if (dt > 0f && dt < 1f)
            {
                var speed = Vector2.Distance(pos, _filteredPos) / dt;
                var cutoff = MinCutoff + Beta * speed;
                var alpha = CalculateAlpha(dt, cutoff);
                _filteredPos.X += (pos.X - _filteredPos.X) * alpha;
                _filteredPos.Y += (pos.Y - _filteredPos.Y) * alpha;
            }

            return _filteredPos;
        }

        private static float CalculateAlpha(float dt, float cutoff)
        {
            var tau = 1f / (2f * MathF.PI * Math.Max(cutoff, 0.01f));
            return 1f / (1f + tau / dt);
        }

        [SliderProperty("Min Cutoff", 0.1f, 100f, 5f)]
        [DefaultPropertyValue(5f)]
        [ToolTip("Minimum cutoff frequency (Hz) for the low-pass filter.\n" +
            "Low (0.1-5 Hz) = very smooth during slow movements, ideal for line art.\n" +
            "High = more responsive base behavior.\n" +
            "Cutoff rises automatically with pen speed via Beta.\n" +
            "This sets the 'resting' smoothness level when the pen is idle.")]
        [Unit("hz")]
        public float MinCutoff { get; set; } = 5f;

        [SliderProperty("Beta", 0f, 2f, 0.5f)]
        [DefaultPropertyValue(0.5f)]
        [ToolTip("How strongly the cutoff frequency rises with pen speed.\n" +
            "0 = constant smoothing regardless of speed (standard low-pass filter).\n" +
            "0.5-2.0 = less smoothing during fast moves, more during slow.\n" +
            "Provides responsive feel for quick strokes + smoothness for details.\n" +
            "Start at 0.5, increase for more dynamic response.")]
        public float Beta { get; set; } = 0.5f;
    }
}
