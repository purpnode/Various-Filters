using System.Numerics;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin.Tablet;

namespace OpenTabletDriver.SmoothingFilters.Filters
{
    [PluginName("purpnode's Noise Reduction")]
    public class NoiseReduction : IPositionedPipelineElement<IDeviceReport>
    {
        private readonly List<Vector2> _buffer = new List<Vector2>();
        private Vector2 _lastOutput;
        private bool _initialized;
        private int _bufferSize = 5;

        public PipelinePosition Position => PipelinePosition.PreTransform;

        public event Action<IDeviceReport> Emit = delegate { };

        public void Consume(IDeviceReport report)
        {
            if (report is ITabletReport tr)
            {
                tr.Position = FilterPosition(tr.Position);
                tr.Pressure = (uint)Math.Clamp(FilterPressure(tr.Pressure), 0f, 65535f);
            }

            Emit.Invoke(report);
        }

        private float FilterPressure(float pressure)
        {
            return pressure;
        }

        private Vector2 FilterPosition(Vector2 point)
        {
            _buffer.Add(point);
            while (_buffer.Count > _bufferSize)
                _buffer.RemoveAt(0);

            if (!_initialized)
            {
                _lastOutput = point;
                _initialized = true;
                return point;
            }

            var median = GetGeometricMedian();
            var distance = Vector2.Distance(point, median);

            if (distance > DistanceThreshold)
            {
                var maxDist = Math.Max(DistanceThreshold, DistanceMaximum);
                var range = maxDist - DistanceThreshold;
                var ratio = range > 0 ? Math.Min(1f, (distance - DistanceThreshold) / range) : 1f;

                _lastOutput.X += (point.X - _lastOutput.X) * ratio;
                _lastOutput.Y += (point.Y - _lastOutput.Y) * ratio;
            }
            else
            {
                _lastOutput.X += (median.X - _lastOutput.X) * 0.5f;
                _lastOutput.Y += (median.Y - _lastOutput.Y) * 0.5f;
            }

            return _lastOutput;
        }

        private Vector2 GetGeometricMedian()
        {
            if (_buffer.Count == 0)
                return Vector2.Zero;

            var candidate = Vector2.Zero;
            for (int i = 0; i < _buffer.Count; i++)
                candidate += _buffer[i];
            candidate /= _buffer.Count;

            const int iterations = 10;
            const float minDist = 0.001f;

            for (int iter = 0; iter < iterations; iter++)
            {
                double denominator = 0;
                var next = Vector2.Zero;

                for (int i = 0; i < _buffer.Count; i++)
                {
                    var dist = Math.Max(Vector2.Distance(candidate, _buffer[i]), minDist);
                    denominator += 1.0 / dist;
                }

                for (int i = 0; i < _buffer.Count; i++)
                {
                    var dist = Math.Max(Vector2.Distance(candidate, _buffer[i]), minDist);
                    var weight = (1.0 / dist) / denominator;
                    next.X += (float)(_buffer[i].X * weight);
                    next.Y += (float)(_buffer[i].Y * weight);
                }

                candidate = next;
            }

            return candidate;
        }

        [SliderProperty("Buffer Size", 1f, 20f, 5f)]
        [DefaultPropertyValue(5f)]
        [ToolTip("Number of recent positions retained for noise analysis.\n" +
            "Larger = smoother output but higher lag.\n" +
            "Smaller = more responsive but less noise reduction.\n" +
            "Recommended: 3-7 for typical use.\n" +
            "Affects the quality of the geometric median computation.")]
        public float BufferSizeValue
        {
            get => _bufferSize;
            set => _bufferSize = Math.Clamp((int)value, 1, 20);
        }

        [SliderProperty("Distance Threshold", 0.1f, 20f, 2f)]
        [DefaultPropertyValue(2f)]
        [ToolTip("Distance (in tablet pixels) that triggers pass-through mode.\n" +
            "When the pen moves farther than this from the median, the filter lets motion through\n" +
            "instead of suppressing it.\n" +
            "Lower = faster response to intentional movement, more noise passes.\n" +
            "Higher = more aggressive noise reduction, may feel sluggish.")]
        [Unit("px")]
        public float DistanceThreshold { get; set; } = 2f;

        [SliderProperty("Distance Max", 1f, 50f, 10f)]
        [DefaultPropertyValue(10f)]
        [ToolTip("Distance for full raw position passthrough.\n" +
            "Between Distance Threshold and this value, movement is blended proportionally.\n" +
            "Higher = more movement needed before the filter fully trusts the input.\n" +
            "Lower = unfiltered response kicks in sooner.")]
        [Unit("px")]
        public float DistanceMaximum { get; set; } = 10f;
    }
}
