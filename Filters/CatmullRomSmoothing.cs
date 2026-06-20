using System.Numerics;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Tablet;

namespace OpenTabletDriver.SmoothingFilters.Filters
{
    [PluginName("purpnode's Catmull-Rom Smoothing")]
    public class CatmullRomSmoothing : SmoothingFilterBase
    {
        public static new readonly string[] PredictionModeOptions = SmoothingFilterBase.PredictionModeOptions;
        public static new readonly string[] ACModeOptions = SmoothingFilterBase.ACModeOptions;

        private readonly List<Vector2> _buffer = new List<Vector2>();
        private int _bufferSize = 6;

        protected override Vector2 FilterPosition(Vector2 pos)
        {
            _buffer.Add(pos);
            while (_buffer.Count > _bufferSize + 1)
                _buffer.RemoveAt(0);

            if (_buffer.Count < 4)
                return pos;

            var p0 = _buffer[_buffer.Count - 4];
            var p1 = _buffer[_buffer.Count - 3];
            var p2 = _buffer[_buffer.Count - 2];
            var p3 = _buffer[_buffer.Count - 1];

            var t = InterpolationPoint;
            var tension = Tension;
            var t2 = t * t;
            var t3 = t2 * t;

            var h0 = (-tension * t3) + (2 * tension * t2) + (-tension * t);
            var h1 = (2 - tension) * t3 + (tension - 3) * t2 + 1;
            var h2 = (tension - 2) * t3 + (3 - 2 * tension) * t2 + (tension * t);
            var h3 = (tension * t3) + (-tension * t2);

            return p0 * h0 + p1 * h1 + p2 * h2 + p3 * h3;
        }

        [SliderProperty("Buffer Size", 3f, 20f, 6f)]
        [DefaultPropertyValue(6f)]
        [ToolTip("Point buffer size for spline construction.\n" +
            "Larger = smoother curve but higher latency.\n" +
            "Minimum 4 points for Catmull-Rom.\n" +
            "Recommended: 5-10 for drawing.")]
        public float BufferSizeValue
        {
            get => _bufferSize;
            set => _bufferSize = Math.Clamp((int)value, 3, 20);
        }

        [SliderProperty("Tension", 0f, 1f, 0f)]
        [DefaultPropertyValue(0f)]
        [ToolTip("Catmull-Rom spline tension parameter.\n" +
            "0 = standard spline, smooth bends.\n" +
            "0.5-1.0 = tighter corners, closer to control points.\n" +
            "Affects sharpness of curve turns.")]
        public float Tension { get; set; } = 0f;

        [SliderProperty("Interpolation", 0.1f, 0.9f, 0.5f)]
        [DefaultPropertyValue(0.5f)]
        [ToolTip("Interpolation position between the two most recent points.\n" +
            "0.5 = midpoint (default, smooth output).\n" +
            "Closer to 0 or 1 = less smooth but more responsive to the latest position.")]
        public float InterpolationPoint { get; set; } = 0.5f;
    }
}
