using System.Numerics;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Tablet;

namespace OpenTabletDriver.SmoothingFilters.Filters
{
    [PluginName("purpnode's Lanczos Smoothing")]
    public class LanczosSmoothing : SmoothingFilterBase
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

            var n = _buffer.Count;
            if (n < 2)
                return pos;

            var a = (int)Lobes;
            var result = Vector2.Zero;
            float totalWeight = 0f;
            var center = n - 1;

            for (int i = 0; i < n; i++)
            {
                var dx = i - center;
                var weight = LanczosKernel(dx, a);
                result += _buffer[i] * weight;
                totalWeight += weight;
            }

            return totalWeight > 0f ? result / totalWeight : pos;
        }

        private static float LanczosKernel(float x, int a)
        {
            if (x == 0f)
                return 1f;
            if (Math.Abs(x) >= a)
                return 0f;

            var pix = MathF.PI * x;
            var pixA = MathF.PI * x / a;
            return (MathF.Sin(pix) / pix) * (MathF.Sin(pixA) / pixA);
        }

        [SliderProperty("Window Size", 2f, 21f, 7f)]
        [DefaultPropertyValue(7f)]
        [ToolTip("Window size for Lanczos resampling.\n" +
            "Larger = smoother, better high-frequency retention, more latency.\n" +
            "Smaller = more responsive, lower quality reconstruction.\n" +
            "Recommended: 5-11.")]
        public float WindowSizeValue
        {
            get => _windowSize;
            set => _windowSize = Math.Clamp((int)value, 2, 21);
        }

        [SliderProperty("Lobes", 2f, 5f, 3f)]
        [DefaultPropertyValue(3f)]
        [ToolTip("Number of Lanczos kernel lobes (a).\n" +
            "2 = sharper cutoff, less smoothing.\n" +
            "3 = standard value, good balance (recommended).\n" +
            "4-5 = smoother cutoff, stronger smoothing.")]
        public float Lobes { get; set; } = 3f;
    }
}
