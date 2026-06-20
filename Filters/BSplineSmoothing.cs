using System.Numerics;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Tablet;

namespace OpenTabletDriver.SmoothingFilters.Filters
{
    [PluginName("purpnode's B-Spline Smoothing")]
    public class BSplineSmoothing : SmoothingFilterBase
    {
        public static new readonly string[] PredictionModeOptions = SmoothingFilterBase.PredictionModeOptions;
        public static new readonly string[] ACModeOptions = SmoothingFilterBase.ACModeOptions;

        private readonly List<Vector2> _buffer = new List<Vector2>();
        private int _windowSize = 5;

        protected override Vector2 FilterPosition(Vector2 pos)
        {
            _buffer.Add(pos);
            while (_buffer.Count > _windowSize + 1)
                _buffer.RemoveAt(0);

            var n = _buffer.Count;
            if (n < 4)
                return pos;

            var result = Vector2.Zero;
            var u = BlendParam;

            for (int i = 0; i < n; i++)
            {
                var basis = CubicBSplineBasis(n - 1, i, u);
                result += _buffer[i] * basis;
            }

            return result;
        }

        private static float CubicBSplineBasis(int k, int i, float u)
        {
            var knot = (float)i / Math.Max(k, 1);
            var d = u - knot;

            if (d < 0f || d > 1f)
                return 0f;

            var d2 = d * d;
            var d3 = d2 * d;

            return (1f / 6f) * (-2f * d3 + 3f * d2 + 3f * d + 1f);
        }

        [SliderProperty("Window Size", 3f, 20f, 5f)]
        [DefaultPropertyValue(5f)]
        [ToolTip("Window size for B-Spline approximation.\n" +
            "Larger = smoother approximation, higher latency.\n" +
            "Minimum 4 points for correct operation.\n" +
            "Recommended: 5-10.")]
        public float WindowSizeValue
        {
            get => _windowSize;
            set => _windowSize = Math.Clamp((int)value, 3, 20);
        }

        [SliderProperty("Blend Param", 0f, 1f, 0.5f)]
        [DefaultPropertyValue(0.5f)]
        [ToolTip("Blend parameter (u) for B-Spline basis calculation.\n" +
            "0 = start of curve, 1 = end of curve.\n" +
            "0.5 gives balanced output.\n" +
            "Lower values = closer to window start, higher = closer to most recent point.")]
        public float BlendParam { get; set; } = 0.5f;
    }
}
