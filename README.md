# purpnode's Various Filters

<p align="center">
  <img src="https://img.shields.io/badge/OpenTabletDriver-0.6.x-blue?style=flat-square"/>
  <img src="https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square"/>
  <img src="https://img.shields.io/badge/license-LGPL--3.0-green?style=flat-square"/>
</p>

A set of **17 smoothing filters** for OpenTabletDriver with built-in antichatter (6 modes) and cursor prediction (5 modes). Each filter is a standalone plugin installable via OTD Plugin Manager.

---

## Table of Contents

- [Installation](#installation)
- [Shared Components](#shared-components)
  - [Antichatter (AC Mode)](#antichatter-ac-mode)
  - [Prediction (Prediction Mode)](#prediction-prediction-mode)
- [Smoothing Filters](#smoothing-filters)
  - [Classic](#classic)
  - [Adaptive](#adaptive)
  - [IIR Filters](#iir-filters)
  - [Splines & Resampling](#splines--resampling)
  - [Predictor-Corrector](#predictor-corrector)
  - [Specialized](#specialized)
- [Compatibility](#compatibility)

---

## Installation

1. Download `purpnode's Various Filters.dll`
2. Place it in the OTD plugins folder (`%LOCALAPPDATA%\OpenTabletDriver\Plugins`)
3. Restart OTD Daemon
4. Under **Filters**, select a filter from the dropdown

---

## Shared Components

Every filter (except Noise Reduction) inherits `SmoothingFilterBase` and automatically includes:

### Antichatter (AC Mode)

Noise suppression at low speed and pen hover. Applied **after** the main smoothing, independently to position and pressure.

| Mode | How it works |
|------|-------------|
| **Off** | Antichatter disabled. |
| **Hysteresis** | If deviation from stable position is below deadzone, position is **held** (noise gated). Above deadzone, smooth EMA blend. Minimal overhead — best choice for most cases. |
| **EMA** | Exponential moving average with constant smoothing factor (`AC Smoothing`). No deadzone gating — always smooths, but more lightly. |
| **Aggressive** | Doubled deadzone + half the smoothing factor. For very noisy tablets. |
| **Adaptive** | Tracks average deviation over last 10 reports. Dynamically adjusts deadzone and alpha: more smoothing when noise is high, less when low. |
| **Devocub** | Power-law speed-adaptive mode. Smoothing coefficient: `alpha = (baseline + power) / (1 + power)`, where `power = (distance / deadzone)^strength`. At zero speed `alpha = baseline` (maximum smoothing); as speed increases `alpha → 1` (no smoothing). **Devocub Strength** controls transition steepness. |

Common AC parameters:
- **AC Deadzone** (0–500) — noise threshold in tablet units for position; levels 0–65535 for pressure.
- **AC Smoothing** (1–100%) — base smoothing coefficient when deadzone is exceeded.
- **Devocub Strength** (0.1–10) — steepness of the power-law transition (Devocub only).

### Prediction (Prediction Mode)

Velocity-based cursor extrapolation to reduce perceived latency. Applied **after** antichatter, before final position.

| Mode | How it works |
|------|-------------|
| **Off** | Prediction disabled. |
| **Standard** | Basic linear extrapolation: `pos + velocity × PredictionMs × Response`. |
| **Conservative** | 50% of Standard. Lower overshoot risk, less latency reduction. |
| **Aggressive** | 150% of Standard. Maximum latency reduction, may overshoot on sharp turns. |
| **Adaptive** | Analyzes velocity history (8 samples) and direction history (5 samples). Reduces prediction factor when direction variance is high (unstable movement). Disables prediction when average speed is very low. Auto-adapts to drawing style. |

Common prediction parameters:
- **Prediction Amount** (0–50 ms) — extrapolation time window.
- **Prediction Response** (0.1–3.0) — prediction strength multiplier.

---

## Smoothing Filters

### Classic

#### Exponential Smoothing
Basic exponential moving average (EMA) for position and pressure.

```
filtered = filtered + (raw - filtered) × alpha
```

- **Smoothing Factor** (1–100%) — `alpha = factor / 100`. Higher = more responsive, lower = smoother.

#### Moving Average Smoothing
Simple moving average with running sum (O(1) per report).

```
filtered = sum(buffer) / buffer.Count
```

- **Window Size** (1–64) — number of averaged points. Larger = smoother but higher latency.

#### Weighted Smoothing
Gaussian-weighted average. Center points of the window have higher weight.

```
weight(i) = exp(-(i - center)² / (2σ²))
filtered = sum(buf[i] × weight[i]) / sum(weight)
```

- **Window Size** (1–32) — window size. Odd values produce symmetric weighting.
- **Sigma (Spread)** (0.5–5.0) — Gaussian spread. Lower = weight concentrated on center.

#### Savitzky-Golay Smoothing
Least-squares quadratic polynomial approximation. Preserves motion details (peaks, inflection points) better than simple averaging.

```
filtered = sum(buf[i] × C[i]), where C are SG filter coefficients
```

- **Window Size** (3–25, odd only) — approximation window size.

#### Pressure Smoothing
Pen pressure-only smoothing. Position passes through unchanged.

```
pressure = pressure + (raw - pressure) × alpha
if |raw - pressure| < deadzone → pressure = raw  (snap)
```

- **Smoothing Factor** (1–100%) — alpha.
- **Deadzone** (0–100) — snap zone: if smoothed value is close to raw, raw is used (zero latency on small changes).

---

### Adaptive

#### Dynamic Smoothing
Speed-adaptive smoothing: heavy smoothing at low speed, minimal at high speed.

```
speedFactor = clamp((speed - minSpeed) / (maxSpeed - minSpeed), 0, 1)
alpha = minAlpha + (maxAlpha - minAlpha) × (1 - speedFactor)
```

- **Max Responsiveness** (1–100%) — alpha at high speed.
- **Min Responsiveness** (1–100%) — alpha at low speed.
- **Min Speed** (1–5000 px/s) — maximum smoothing threshold.
- **Max Speed** (10–50000 px/s) — minimum smoothing threshold.

#### One Euro Smoothing
Adaptive first-order low-pass filter with dynamic cutoff frequency. Cutoff rises proportionally to pen speed.

```
cutoff = minCutoff + beta × speed
alpha = 1 / (1 + τ / Δt), where τ = 1 / (2π × cutoff)
```

- **Min Cutoff** (0.1–100 Hz) — minimum cutoff frequency (smoothing at rest).
- **Beta** (0–2.0) — speed influence coefficient. 0 = vanilla LPF.

---

### IIR Filters

#### Butterworth Smoothing
2nd-order Butterworth filter (biquad). Maximally flat passband, -12 dB/oct rolloff.

Implemented via bilinear transform:
```
α = sin(ω₀Δt) / √2
a₀ = 1 + α,  a₁ = -2cos(ω₀Δt) / a₀,  a₂ = (1 - α) / a₀
b₀ = (1 - cos(ω₀Δt)) / (2·a₀),  b₁ = 2·b₀,  b₂ = b₀
y = b₀x + b₁x₁ + b₂x₂ - a₁y₁ - a₂y₂
```

- **Cutoff Frequency** (1–200 Hz) — cutoff frequency.

#### Bessel Smoothing
2nd-order Bessel filter. Linear phase response — all frequencies are delayed by the same amount. Preserves signal shape better than Butterworth at the cost of a slightly gentler rolloff.

- **Cutoff Frequency** (1–200 Hz) — cutoff frequency.

---

### Splines & Resampling

#### Catmull-Rom Smoothing
Hermite cubic spline through 4 control points. Smooth curve passing through all points.

```
h₀ = -tension·t³ + 2·tension·t² - tension·t
h₁ = (2-tension)·t³ + (tension-3)·t² + 1
h₂ = (tension-2)·t³ + (3-2·tension)·t² + tension·t
h₃ = tension·t³ - tension·t²
filtered = p₀·h₀ + p₁·h₁ + p₂·h₂ + p₃·h₃
```

- **Buffer Size** (3–20) — number of points in buffer (minimum 4).
- **Tension** (0–1.0) — spline tension. 0 = smooth, 1 = sharp.
- **Interpolation** (0.1–0.9) — position on the segment between p₁ and p₂.

#### B-Spline Smoothing
Cubic B-spline approximation through weighted sum of basis functions.

```
basis = (-2·u³ + 3·u² + 3·u + 1) / 6,  where u is the local coordinate
filtered = sum(buf[i] × basis(i))
```

- **Window Size** (3–20) — window size (minimum 4 points).
- **Blend Param** (0–1.0) — blending parameter u.

#### Lanczos Smoothing
Resampling via the Lanczos kernel. Preserves high frequencies (stroke detail) well.

```
L(x) = sin(πx)·sin(πx/a) / (π²x²/a)  for |x| < a
L(0) = 1,  L(|x| ≥ a) = 0
```

- **Window Size** (2–21) — window size.
- **Lobes** (2–5) — kernel lobes (parameter a). 3 is standard.

#### Chaikin Smoothing
Iterative corner-cutting algorithm. Each iteration replaces each segment with two points at 25% and 75% of its length.

```
Q = P[i] × 0.75 + P[i+1] × 0.25
R = P[i] × 0.25 + P[i+1] × 0.75
```

- **Buffer Size** (2–12) — number of points in buffer.
- **Iterations** (1–4) — number of refinement iterations. More = smoother but quadratic point growth.

---

### Predictor-Corrector

#### Weiss Smoothing
Predictor-corrector model with acceleration tracking. Similar to a Kalman filter with fixed coefficients (constant gain).

```
predicted = state + velocity·Δt + 0.5·accel·Δt²
state = predicted + (measurement - predicted) × gain
velocity += accel·Δt + innovation × velocityGain / Δt
accel += (velocity - prevVelocity) × decay
```

- **Smoothing Gain** (0.01–1.0) — measurement correction coefficient.
- **Velocity Gain** (0.001–1.0) — velocity update rate.
- **Accel Decay** (0.001–0.5) — acceleration decay rate.

#### Kalman Smoothing
Discrete Kalman filter with constant parameters. Estimates state (position + velocity) through a predict-update cycle.

```
Predict:  x = x + v·dt,  P = P + Q
Update:   K = P / (P + R)
          x = x + K·(measurement - x)
          v = v + K·(measurement - x) / dt
          P = P · (1 - K)
```

- **Process Noise** (0.01–10) — model trust (Q). Higher = more responsive.
- **Measurement Noise** (0.01–10) — assumed measurement noise (R). Higher = more aggressive smoothing.

---

### Specialized

#### Double Exp Smoothing
Holt's double exponential smoothing. Tracks level and trend, enabling forward extrapolation.

```
level = α·raw + (1-α)·(level + trend)
trend = β·(level - prevLevel) + (1-β)·trend
output = level + trend × (1 + forecastSteps)
```

- **Alpha (Level)** (0.01–1.0) — level smoothing.
- **Beta (Trend)** (0.001–0.5) — trend smoothing.
- **Forecast Steps** (0–5) — trend extrapolation steps.

#### Noise Reduction
Standalone filter (does NOT inherit SmoothingFilterBase). Noise suppression via geometric median of the point buffer. No antichatter or prediction — a pure noise gate.

Finds the point minimizing the sum of distances to all buffer points (geometric median). If the current point is far from the median, movement passes through; if close, motion is smoothed toward the median.

- **Buffer Size** (1–20) — analysis buffer size.
- **Distance Threshold** (0.1–20 px) — movement pass-through threshold.
- **Distance Max** (1–50 px) — full pass-through threshold.

---

## Compatibility

- OpenTabletDriver **0.6.x** (built against v0.6.1 Plugin API)
- .NET **8.0**
- Platforms: Windows (WPF), Linux (GTK), macOS (MacOS)

---

<p align="center">
  <sub>Built with ❤️ for the osu!community</sub>
</p>
