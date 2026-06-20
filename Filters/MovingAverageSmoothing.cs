using System.Numerics;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Tablet;

namespace OpenTabletDriver.SmoothingFilters.Filters
{
    [PluginName("purpnode's Moving Average Smoothing")]
    public class MovingAverageSmoothing : SmoothingFilterBase
    {
        public static new readonly string[] PredictionModeOptions = SmoothingFilterBase.PredictionModeOptions;
        public static new readonly string[] ACModeOptions = SmoothingFilterBase.ACModeOptions;

        private readonly List<Vector2> _buffer = new List<Vector2>();
        private Vector2 _sum;
        private int _windowSize = 8;

        protected override Vector2 FilterPosition(Vector2 pos)
        {
            _buffer.Add(pos);
            _sum += pos;

            while (_buffer.Count > _windowSize)
            {
                _sum -= _buffer[0];
                _buffer.RemoveAt(0);
            }

            return _buffer.Count >= 1 ? _sum / _buffer.Count : pos;
        }

        [SliderProperty("Window Size", 1f, 64f, 8f)]
        [DefaultPropertyValue(8f)]
        [ToolTip("Number of recent positions to average together.\n" +
            "Larger (16-64) = very smooth but more lag.\n" +
            "Smaller (2-8) = responsive but less smoothing.\n" +
            "Depends on tablet report rate: 8-12 for 200Hz+, 4-8 for lower rates.\n" +
            "Simply put: how many previous points are averaged.")]
        public float WindowSizeValue
        {
            get => _windowSize;
            set => _windowSize = Math.Clamp((int)value, 1, 64);
        }
    }
}
