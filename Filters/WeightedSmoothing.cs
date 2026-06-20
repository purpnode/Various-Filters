using System.Numerics;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Tablet;

namespace OpenTabletDriver.SmoothingFilters.Filters
{
    [PluginName("purpnode's Weighted Smoothing")]
    public class WeightedSmoothing : SmoothingFilterBase
    {
        public static new readonly string[] PredictionModeOptions = SmoothingFilterBase.PredictionModeOptions;
        public static new readonly string[] ACModeOptions = SmoothingFilterBase.ACModeOptions;

        private readonly List<Vector2> _buffer = new List<Vector2>();
        private int _windowSize = 7;

        protected override Vector2 FilterPosition(Vector2 pos)
        {
            _buffer.Add(pos);
            while (_buffer.Count > _windowSize)
                _buffer.RemoveAt(0);

            if (_buffer.Count < 1)
                return pos;

            var result = Vector2.Zero;
            float totalWeight = 0;

            for (int i = 0; i < _buffer.Count; i++)
            {
                var weight = GaussianWeight(i, _buffer.Count);
                result += _buffer[i] * weight;
                totalWeight += weight;
            }

            return totalWeight > 0 ? result / totalWeight : pos;
        }

        private float GaussianWeight(int index, int count)
        {
            if (count <= 1)
                return 1f;

            var sigma = Sigma > 0 ? Sigma : 1f;
            var center = (count - 1) / 2f;
            var dist = index - center;
            return MathF.Exp(-(dist * dist) / (2 * sigma * sigma));
        }

        [SliderProperty("Window Size", 1f, 32f, 7f)]
        [DefaultPropertyValue(7f)]
        [ToolTip("Gaussian weighting window size.\n" +
            "Larger = smoother curves but more latency.\n" +
            "Need at least 3-5 points for meaningful weighting.\n" +
            "Odd values produce symmetric weighting around the center.")]
        public float WindowSizeValue
        {
            get => _windowSize;
            set => _windowSize = Math.Clamp((int)value, 1, 32);
        }

        [SliderProperty("Sigma (Spread)", 0.5f, 5f, 1.5f)]
        [DefaultPropertyValue(1.5f)]
        [ToolTip("Spread of the Gaussian bell curve (sigma).\n" +
            "Lower (0.5-1.0) — weight concentrated on recent points. More responsive, less smooth.\n" +
            "Higher (2.0-5.0) — weight spread evenly. Smoother, less responsive.\n" +
            "1.5 is a balanced default for most tasks.")]
        public float Sigma { get; set; } = 1.5f;
    }
}
