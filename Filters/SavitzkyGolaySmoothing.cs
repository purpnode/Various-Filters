using System.Numerics;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Tablet;

namespace OpenTabletDriver.SmoothingFilters.Filters
{
    [PluginName("purpnode's Savitzky-Golay Smoothing")]
    public class SavitzkyGolaySmoothing : SmoothingFilterBase
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

            if (_buffer.Count < 3)
                return pos;

            var n = _buffer.Count;
            var coeffs = GetQuadraticCoefficients(n);

            var result = Vector2.Zero;
            for (int i = 0; i < n; i++)
                result += _buffer[i] * coeffs[i];

            return result;
        }

        private static float[] GetQuadraticCoefficients(int n)
        {
            var coeffs = new float[n];
            var m = (n - 1) / 2;
            var denom = (2 * m + 3) * (2 * m + 1) * (2 * m - 1);

            if (denom <= 0)
            {
                for (int i = 0; i < n; i++)
                    coeffs[i] = 1f / n;
                return coeffs;
            }

            for (int i = -m; i <= m; i++)
            {
                var idx = i + m;
                coeffs[idx] = 3f * (3 * m * m + 3 * m - 1 - 5 * i * i) / denom;
            }

            return coeffs;
        }

        [SliderProperty("Window Size", 3f, 25f, 7f)]
        [DefaultPropertyValue(7f)]
        [ToolTip("Fitting window size (must be odd).\n" +
            "Fits a quadratic polynomial over the window — preserves motion details\n" +
            "better than simple averaging.\n" +
            "Large (11-25) = very smooth while preserving stroke shape, more lag.\n" +
            "Minimum 3. Values under 7 may let jitter through.")]
        public float WindowSizeValue
        {
            get => _windowSize;
            set
            {
                var val = (int)Math.Round(value, MidpointRounding.AwayFromZero);
                if (val % 2 == 0) val++;
                _windowSize = Math.Clamp(val, 3, 25);
            }
        }
    }
}
