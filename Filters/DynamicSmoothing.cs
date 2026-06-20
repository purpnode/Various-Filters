using System.Numerics;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Tablet;

namespace OpenTabletDriver.SmoothingFilters.Filters
{
    [PluginName("purpnode's Dynamic Smoothing")]
    public class DynamicSmoothing : SmoothingFilterBase
    {
        public static new readonly string[] PredictionModeOptions = SmoothingFilterBase.PredictionModeOptions;
        public static new readonly string[] ACModeOptions = SmoothingFilterBase.ACModeOptions;

        private Vector2 _lastPos;
        private Vector2 _smoothedPos;
        private bool _initialized;
        private DateTime _lastTime;

        protected override Vector2 FilterPosition(Vector2 pos)
        {
            if (!_initialized)
            {
                _lastPos = pos;
                _smoothedPos = pos;
                _lastTime = DateTime.UtcNow;
                _initialized = true;
                return pos;
            }

            var now = DateTime.UtcNow;
            var dt = (float)(now - _lastTime).TotalSeconds;
            _lastTime = now;

            if (dt > 0f)
            {
                var speed = Vector2.Distance(pos, _lastPos) / dt;
                var minS = MinSpeed > 0f ? MinSpeed : 1f;
                var maxS = Math.Max(MaxSpeed, minS + 1f);
                var speedFactor = Math.Clamp((speed - minS) / (maxS - minS), 0f, 1f);

                var maxAlpha = MaxResponsiveness / 100f;
                var minAlpha = MinResponsiveness / 100f;
                var alpha = minAlpha + (maxAlpha - minAlpha) * (1f - speedFactor);

                _smoothedPos.X += (pos.X - _smoothedPos.X) * alpha;
                _smoothedPos.Y += (pos.Y - _smoothedPos.Y) * alpha;
            }

            _lastPos = pos;
            return _smoothedPos;
        }

        [SliderProperty("Max Responsiveness", 1f, 100f, 80f)]
        [DefaultPropertyValue(80f)]
        [ToolTip("Maximum responsiveness (alpha) at high pen speed.\n" +
            "100% = zero smoothing during fast strokes, pen follows instantly.\n" +
            "Lower = some smoothing persists even at high speed.\n" +
            "Useful if you want a minimum smoothing floor at all speeds.")]
        [Unit("%")]
        public float MaxResponsiveness { get; set; } = 80f;

        [SliderProperty("Min Responsiveness", 1f, 100f, 10f)]
        [DefaultPropertyValue(10f)]
        [ToolTip("Minimum responsiveness (alpha) during slow or stationary pen.\n" +
            "Low (1-15%) = maximum smoothing for slow detailed work (inking).\n" +
            "High = less smoothing during slow movements, more direct fine control.\n" +
            "Works with Min Speed to define the slow-speed smoothing zone.")]
        [Unit("%")]
        public float MinResponsiveness { get; set; } = 10f;

        [SliderProperty("Min Speed", 1f, 5000f, 10f)]
        [DefaultPropertyValue(10f)]
        [ToolTip("Speed threshold (px/s) for the maximum smoothing zone.\n" +
            "Movements slower than this get the highest smoothing level.\n" +
            "Adjust based on your typical slow-drawing speed.\n" +
            "Lower values (5-50) = only very slow motion is heavily smoothed.")]
        [Unit("px/s")]
        public float MinSpeed { get; set; } = 10f;

        [SliderProperty("Max Speed", 10f, 50000f, 1000f)]
        [DefaultPropertyValue(1000f)]
        [ToolTip("Speed threshold (px/s) for the minimum smoothing zone.\n" +
            "Movements faster than this get the lowest (most responsive) smoothing.\n" +
            "Higher = filter stays in smoothing mode at normal drawing speed.\n" +
            "Lower = becomes responsive earlier, even at moderate speeds.")]
        [Unit("px/s")]
        public float MaxSpeed { get; set; } = 1000f;
    }
}
