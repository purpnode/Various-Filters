using System.Numerics;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Tablet;

namespace OpenTabletDriver.SmoothingFilters.Filters
{
    [PluginName("purpnode's Chaikin Smoothing")]
    public class ChaikinSmoothing : SmoothingFilterBase
    {
        public static new readonly string[] PredictionModeOptions = SmoothingFilterBase.PredictionModeOptions;
        public static new readonly string[] ACModeOptions = SmoothingFilterBase.ACModeOptions;
        private readonly List<Vector2> _buffer = new List<Vector2>();
        private int _bufferSize = 6;

        private Vector2[] _workArray = Array.Empty<Vector2>();
        private Vector2[] _refinedArray = Array.Empty<Vector2>();

        protected override Vector2 FilterPosition(Vector2 pos)
        {
            _buffer.Add(pos);
            while (_buffer.Count > _bufferSize + 2)
                _buffer.RemoveAt(0);

            if (_buffer.Count < 3)
                return pos;

            var count = _buffer.Count;
            if (_workArray.Length < count)
                _workArray = new Vector2[count * 4];
            for (int i = 0; i < count; i++)
                _workArray[i] = _buffer[i];
            var workLen = count;

            var iterations = (int)Iterations;

            for (int iter = 0; iter < iterations; iter++)
            {
                if (workLen < 2)
                    break;

                var refinedLen = (workLen - 1) * 2;
                if (_refinedArray.Length < refinedLen)
                    _refinedArray = new Vector2[refinedLen];

                for (int i = 0; i < workLen - 1; i++)
                {
                    var idx = i * 2;
                    _refinedArray[idx] = _workArray[i] * 0.75f + _workArray[i + 1] * 0.25f;
                    _refinedArray[idx + 1] = _workArray[i] * 0.25f + _workArray[i + 1] * 0.75f;
                }

                var tmp = _workArray;
                _workArray = _refinedArray;
                _refinedArray = tmp;
                workLen = refinedLen;
            }

            return workLen > 0 ? _workArray[workLen - 1] : pos;
        }

        [SliderProperty("Buffer Size", 2f, 12f, 6f)]
        [DefaultPropertyValue(6f)]
        [ToolTip("Number of points for Chaikin corner-cutting.\n" +
            "Larger = smoother but higher latency.\n" +
            "Minimum 2-3 points for meaningful results.\n" +
            "Recommended: 4-8.")]
        public float BufferSizeValue
        {
            get => _bufferSize;
            set => _bufferSize = Math.Clamp((int)value, 2, 12);
        }

        [SliderProperty("Iterations", 1f, 4f, 1f)]
        [DefaultPropertyValue(1f)]
        [ToolTip("Number of corner-cutting refinement iterations.\n" +
            "1 = moderate smoothing, low latency.\n" +
            "2-3 = strong smoothing, more latency.\n" +
            "4 = very smooth but may lag significantly.\n" +
            "Each iteration doubles the point count.")]
        public float Iterations { get; set; } = 1f;
    }
}
