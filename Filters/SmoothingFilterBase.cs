using System.Numerics;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin.Tablet;

namespace OpenTabletDriver.SmoothingFilters.Filters
{
    public abstract class SmoothingFilterBase : IPositionedPipelineElement<IDeviceReport>
    {
        public PipelinePosition Position => PipelinePosition.PreTransform;
        public event Action<IDeviceReport> Emit = delegate { };

        protected abstract Vector2 FilterPosition(Vector2 pos);
        protected virtual float FilterPressure(float pressure) => pressure;

        // --- Prediction state ---
        private Vector2 _predPrevPos;
        private DateTime _predPrevTime;
        private bool _predInit;
        private readonly List<float> _predSpeeds = new List<float>();
        private readonly List<float> _predDirections = new List<float>();

        // --- Position antichatter state ---
        private Vector2 _acStablePos;
        private bool _acPosInit;
        private readonly List<float> _acPosDists = new List<float>();

        // --- Pressure antichatter state ---
        private float _acLastPressure;
        private uint _acLastStable;
        private bool _acPressInit;
        private readonly List<float> _acPressDiffs = new List<float>();

        public void Consume(IDeviceReport report)
        {
            if (report is ITabletReport tr)
            {
                var pos = FilterPosition(tr.Position);
                pos = ApplyPositionAC(pos);

                if (PredictionMode != "Off")
                    pos = ApplyPrediction(pos);

                tr.Position = pos;

                var pressure = FilterPressure(tr.Pressure);
                pressure = ApplyPressureAC(pressure);

                tr.Pressure = (uint)Math.Clamp(pressure, 0f, 65535f);
            }

            Emit.Invoke(report);
        }

        // ─── Position AntiChatter ───────────────────────────────────────────────

        private Vector2 ApplyPositionAC(Vector2 pos)
        {
            var mode = ParseMode(ACMode);
            if (mode == 0)
                return pos;

            if (!_acPosInit)
            {
                _acStablePos = pos;
                _acPosInit = true;
                return pos;
            }

            var dist = Vector2.Distance(pos, _acStablePos);
            var deadzone = ACDeadzone;
            var factor = ACSmoothingFactor / 100f;

            switch (mode)
            {
                case 1:
                {
                    if (dist <= deadzone)
                        return _acStablePos;

                    var alpha = Math.Max(factor, 0.1f);
                    _acStablePos = _acStablePos + (pos - _acStablePos) * alpha;
                    return _acStablePos;
                }

                case 2:
                {
                    var alpha = factor;
                    _acStablePos = _acStablePos + (pos - _acStablePos) * alpha;
                    return _acStablePos;
                }

                case 3:
                {
                    var effDeadzone = deadzone * 2f;
                    if (dist <= effDeadzone)
                        return _acStablePos;

                    var alpha = Math.Min(factor * 0.5f, 0.1f);
                    _acStablePos = _acStablePos + (pos - _acStablePos) * alpha;
                    return _acStablePos;
                }

                case 4:
                {
                    _acPosDists.Add(dist);
                    if (_acPosDists.Count > 10)
                        _acPosDists.RemoveAt(0);

                    var avgDist = 0f;
                    for (int i = 0; i < _acPosDists.Count; i++)
                        avgDist += _acPosDists[i];
                    avgDist /= _acPosDists.Count;

                    var adDeadzone = Math.Max(deadzone, avgDist * 0.5f);
                    if (dist <= adDeadzone)
                        return _acStablePos;

                    var alpha = Math.Min(factor * (1f + avgDist / Math.Max(deadzone, 1f)), 0.5f);
                    _acStablePos = _acStablePos + (pos - _acStablePos) * alpha;
                    return _acStablePos;
                }

                case 5:
                {
                    // Devocub — speed-adaptive power-law smoothing.
                    // At low speed → alpha near factor (max smoothing).
                    // At high speed → alpha approaches 1 (responsive).
                    var speedRatio = dist / Math.Max(deadzone, 1f);
                    var power = MathF.Pow(speedRatio, ACDevocubStrength);
                    var alpha = (factor + power) / (1f + power);
                    alpha = Math.Clamp(alpha, 0.01f, 0.99f);

                    _acStablePos = _acStablePos + (pos - _acStablePos) * alpha;
                    return _acStablePos;
                }

                default:
                    return pos;
            }
        }

        // ─── Pressure AntiChatter ──────────────────────────────────────────────

        private float ApplyPressureAC(float pressure)
        {
            var mode = ParseMode(ACMode);
            if (mode == 0)
                return pressure;

            if (!_acPressInit)
            {
                _acLastPressure = pressure;
                _acLastStable = (uint)pressure;
                _acPressInit = true;
                return pressure;
            }

            var diff = Math.Abs(pressure - _acLastPressure);
            var deadzone = ACDeadzone;
            var factor = ACSmoothingFactor / 100f;

            switch (mode)
            {
                case 1:
                {
                    if (diff <= deadzone)
                        return _acLastStable;

                    var alpha = Math.Max(factor, 0.1f);
                    _acLastPressure += (pressure - _acLastPressure) * alpha;
                    _acLastStable = (uint)Math.Round(_acLastPressure, MidpointRounding.AwayFromZero);
                    return _acLastStable;
                }

                case 2:
                {
                    var alpha = factor;
                    _acLastPressure += (pressure - _acLastPressure) * alpha;
                    _acLastStable = (uint)Math.Round(_acLastPressure, MidpointRounding.AwayFromZero);
                    return _acLastStable;
                }

                case 3:
                {
                    var effDeadzone = deadzone * 2f;
                    if (diff <= effDeadzone)
                        return _acLastStable;

                    var alpha = Math.Min(factor * 0.5f, 0.1f);
                    _acLastPressure += (pressure - _acLastPressure) * alpha;
                    _acLastStable = (uint)Math.Round(_acLastPressure, MidpointRounding.AwayFromZero);
                    return _acLastStable;
                }

                case 4:
                {
                    _acPressDiffs.Add(diff);
                    if (_acPressDiffs.Count > 10)
                        _acPressDiffs.RemoveAt(0);

                    var avgDiff = 0f;
                    for (int i = 0; i < _acPressDiffs.Count; i++)
                        avgDiff += _acPressDiffs[i];
                    avgDiff /= _acPressDiffs.Count;

                    var adDeadzone = Math.Max(deadzone, avgDiff * 0.5f);
                    if (diff <= adDeadzone)
                        return _acLastStable;

                    var alpha = Math.Min(factor * (1f + avgDiff / Math.Max(deadzone, 1f)), 0.5f);
                    _acLastPressure += (pressure - _acLastPressure) * alpha;
                    _acLastStable = (uint)Math.Round(_acLastPressure, MidpointRounding.AwayFromZero);
                    return _acLastStable;
                }

                case 5:
                {
                    var speedRatio = diff / Math.Max(deadzone, 1f);
                    var power = MathF.Pow(speedRatio, ACDevocubStrength);
                    var alpha = (factor + power) / (1f + power);
                    alpha = Math.Clamp(alpha, 0.01f, 0.99f);

                    _acLastPressure += (pressure - _acLastPressure) * alpha;
                    _acLastStable = (uint)Math.Round(_acLastPressure, MidpointRounding.AwayFromZero);
                    return _acLastStable;
                }

                default:
                    return pressure;
            }
        }

        private static int ParseMode(string mode) => mode switch
        {
            "Off" => 0,
            "Hysteresis" => 1,
            "EMA" => 2,
            "Aggressive" => 3,
            "Adaptive" => 4,
            "Devocub" => 5,
            _ => 0
        };

        // ─── Prediction ────────────────────────────────────────────────────────

        private Vector2 ApplyPrediction(Vector2 pos)
        {
            if (!_predInit)
            {
                _predPrevPos = pos;
                _predPrevTime = DateTime.UtcNow;
                _predInit = true;
                return pos;
            }

            var now = DateTime.UtcNow;
            var dt = (float)(now - _predPrevTime).TotalMilliseconds;
            if (dt <= 0 || dt >= 200)
            {
                _predPrevPos = pos;
                _predPrevTime = now;
                return pos;
            }

            var vel = (pos - _predPrevPos) / dt;
            var speed = vel.Length();
            var basePrediction = vel * PredictionMs * PredictionResponse;

            _predPrevPos = pos;
            _predPrevTime = now;

            switch (PredictionMode)
            {
                case "Off":
                    return pos;

                case "Conservative":
                    return pos + basePrediction * 0.5f;

                case "Aggressive":
                    return pos + basePrediction * 1.5f;

                case "Adaptive":
                {
                    _predSpeeds.Add(speed);
                    if (_predSpeeds.Count > 8)
                        _predSpeeds.RemoveAt(0);

                    _predDirections.Add(MathF.Atan2(vel.Y, vel.X));
                    if (_predDirections.Count > 5)
                        _predDirections.RemoveAt(0);

                    var avgSpeed = 0f;
                    for (int i = 0; i < _predSpeeds.Count; i++)
                        avgSpeed += _predSpeeds[i];
                    avgSpeed /= Math.Max(_predSpeeds.Count, 1);

                    if (avgSpeed < 0.5f)
                        return pos;

                    var dirVariance = 0f;
                    if (_predDirections.Count >= 2)
                    {
                        var meanDir = 0f;
                        for (int i = 0; i < _predDirections.Count; i++)
                            meanDir += _predDirections[i];
                        meanDir /= _predDirections.Count;

                        for (int i = 0; i < _predDirections.Count; i++)
                        {
                            var diff = _predDirections[i] - meanDir;
                            while (diff > MathF.PI) diff -= 2f * MathF.PI;
                            while (diff < -MathF.PI) diff += 2f * MathF.PI;
                            dirVariance += diff * diff;
                        }
                        dirVariance /= _predDirections.Count;
                    }

                    var confidence = Math.Clamp(1f - dirVariance / 2f, 0.1f, 1f);
                    var speedRatio = Math.Clamp(speed / Math.Max(avgSpeed, 0.1f), 0.5f, 2f);
                    var adaptiveFactor = confidence * (0.5f + speedRatio * 0.25f);

                    return pos + basePrediction * adaptiveFactor;
                }

                default:
                    return pos + basePrediction;
            }
        }

        // ─── Prediction properties ─────────────────────────────────────────────

        public static readonly string[] PredictionModeOptions = { "Off", "Standard", "Conservative", "Aggressive", "Adaptive" };

        [Property("Prediction Mode")]
        [PropertyValidated(nameof(PredictionModeOptions))]
        [DefaultPropertyValue("Standard")]
        [ToolTip("Selects the cursor prediction mode.\n" +
            "Off — prediction disabled.\n" +
            "Standard — balanced velocity extrapolation.\n" +
            "Conservative — 50% of Standard, lower overshoot risk.\n" +
            "Aggressive — 150% of Standard, more responsive, may overshoot.\n" +
            "Adaptive — auto-adjusts strength based on direction stability.")]
        public string PredictionMode { get; set; } = "Standard";

        [SliderProperty("Prediction Amount", 0f, 50f, 6f)]
        [DefaultPropertyValue(6f)]
        [ToolTip("Prediction look-ahead window in milliseconds.\n" +
            "Higher values reduce perceived latency more but increase overshoot risk.\n" +
            "Start at 6ms and adjust in 2ms steps.\n" +
            "Recommended: 4-8ms for drawing, 8-15ms for fast-paced games.")]
        [Unit("ms")]
        public float PredictionMs { get; set; } = 6f;

        [SliderProperty("Prediction Response", 0.1f, 3f, 1f)]
        [DefaultPropertyValue(1f)]
        [ToolTip("Multiplier for prediction strength.\n" +
            ">1.0 — stronger prediction, less latency, may overshoot.\n" +
            "<1.0 — weaker prediction, safer, less latency reduction.\n" +
            "Multiplied by the mode-specific coefficient from Prediction Mode.\n" +
            "Typical range 0.5-1.5, start at 1.0.")]
        public float PredictionResponse { get; set; } = 1f;

        // ─── AntiChatter properties ────────────────────────────────────────────

        public static readonly string[] ACModeOptions = { "Off", "Hysteresis", "EMA", "Aggressive", "Adaptive", "Devocub" };

        [Property("AC Mode")]
        [PropertyValidated(nameof(ACModeOptions))]
        [DefaultPropertyValue("Hysteresis")]
        [ToolTip("Anti-wobble noise filter applied to both cursor and pressure.\n" +
            "Off — no additional filtering.\n" +
            "Hysteresis — holds the last stable value within deadzone. Does not smooth,\n" +
            "  just gates noise. Best choice for most cases.\n" +
            "EMA — exponential moving average, continuously smooths all input.\n" +
            "Aggressive — double deadzone + heavy smoothing for very noisy tablets.\n" +
            "Adaptive — dynamically adjusts deadzone to current fluctuation level.\n" +
            "Devocub — speed-adaptive power-law smoothing. Low speed = heavy smoothing,\n" +
            "  high speed = responsive. Adjustable via Devocub Strength.")]
        public string ACMode { get; set; } = "Hysteresis";

        [SliderProperty("AC Deadzone", 0f, 500f, 20f)]
        [DefaultPropertyValue(20f)]
        [ToolTip("Noise deadzone for cursor and pressure.\n" +
            "Cursor: movement smaller than this distance (in tablet units) snaps to the last stable position.\n" +
            "Pressure: changes smaller than this threshold (levels 0-65535) are treated as noise.\n" +
            "Higher = stronger noise rejection, but lower sensitivity.\n" +
            "Start at 20 and increase if jitter remains. In Adaptive mode, this is the base for auto-tuning.")]
        [Unit("px")]
        public float ACDeadzone { get; set; } = 20f;

        [SliderProperty("AC Smoothing", 1f, 100f, 15f)]
        [DefaultPropertyValue(15f)]
        [ToolTip("Smoothing strength once the deadzone is exceeded.\n" +
            "Lower % = smoother but more sluggish response (good for noisy tablets).\n" +
            "Higher % = more responsive but may let noise through.\n" +
            "In Adaptive mode this value is dynamically modulated.\n" +
            "In Devocub mode this sets the minimum responsiveness at zero speed.\n" +
            "Recommended: 10-20% for Hysteresis, 20-40% for EMA.")]
        [Unit("%")]
        public float ACSmoothingFactor { get; set; } = 15f;

        [SliderProperty("Devocub Strength", 0.1f, 10f, 2f)]
        [DefaultPropertyValue(2f)]
        [ToolTip("Steepness of the Devocub speed-adaptive transition curve.\n" +
            "Higher (4-10) = sharp transition: heavy smoothing at low speed,\n" +
            "  almost no smoothing once past the deadzone.\n" +
            "Lower (0.1-1) = gradual transition: smoothing fades slowly\n" +
            "  as speed increases.\n" +
            "2 is a balanced default.\n" +
            "Only applies when AC Mode is set to Devocub.")]
        public float ACDevocubStrength { get; set; } = 2f;
    }
}
