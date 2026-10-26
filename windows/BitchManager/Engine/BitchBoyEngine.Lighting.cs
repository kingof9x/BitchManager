using System.Windows.Threading;
using BitchManager.Models;
using BitchManager.Platform;

namespace BitchManager.Engine;

/// <summary>The 15 LED shows, rainbow refresh, reactive key FX and the custom FX studio.</summary>
public sealed partial class BitchBoyEngine
{
    private static readonly int[] RainbowVel = HardwareColor.Rainbow;
    private DispatcherTimer? _fxTimer;
    private DispatcherTimer? _reactiveTimer;
    private readonly int[] _lastSentReactive = Enumerable.Repeat(255, 49).ToArray();

    // ---- Reactive FX ----
    public ReactiveKeyFXMode ReactiveKeyFXMode { get => G(ReactiveKeyFXMode.Ripple); set => S(value); }
    public double ReactiveFXSpeed { get => G(1.4); set => S(value); }
    public int ReactiveFXColorVelocity { get => G(78); set => S(value); }
    public bool ReactiveFXRainbowColor { get => G(true); set => S(value); }
    public List<ReactivePulseEvent> ActiveReactivePulses { get; } = [];
    public int LastReactiveOriginKey { get => G(-1); set => S(value); }

    // ---- Custom FX studio ----
    public string RightWingTab { get => G("SEQUENCER"); set => S(value); }
    public List<CustomLightEffect> CustomEffects { get; private set; } = [];
    public CustomLightEffect EditingCustomFX { get => G(CustomLightEffect.BuildStarterEffects()[0]); set => S(value); }
    public int EditingFrameIndex { get => G(0); set => S(value); }
    public int CustomBrushVelocity { get => G(78); set => S(value); }
    public bool PaintOnMainChassis { get => G(false); set => S(value); }
    public bool IsPlayingCustomFX { get => G(false); set => S(value); }
    public string ActiveCustomFXName { get => G(""); set => S(value); }
    private int _customFXStep;
    private int _customFXDirection = 1;

    /// <summary>Signals that EditingCustomFX was mutated in place (frames painted etc).</summary>
    public void TouchEditingFX() => Raise(nameof(EditingCustomFX));

    private void SetLed(int k, int vel)
    {
        var c = Controls[k];
        if (c.CurrentLED == vel) return;
        c.CurrentLED = vel;
        SendNoteOn(c.Number, vel);
    }

    private void StopFxTimer()
    {
        _fxTimer?.Stop();
        _fxTimer = null;
    }

    private void StopAllTimers()
    {
        StopFxTimer();
        _reactiveTimer?.Stop();
        _reactiveTimer = null;
    }

    private void RunFx(double baseInterval, Action tick)
        => _fxTimer = StartTimer(baseInterval / Math.Max(0.2, FxSpeed), tick);

    // ------------------------------------------------------------------------
    // FX modes
    // ------------------------------------------------------------------------
    public void CycleFXMode()
    {
        var modes = Enum.GetValues<LEDFXMode>();
        ApplyFXMode(modes[(Array.IndexOf(modes, CurrentFXMode) + 1) % modes.Length]);
    }

    public void ApplyFXMode(LEDFXMode mode)
    {
        IsPlayingCustomFX = false;
        ActiveCustomFXName = "";
        CurrentFXMode = mode;
        VuMeterLEDsEnabled = mode == LEDFXMode.VuMeter;
        StopFxTimer();

        var pos = KeyMap.Positions;
        switch (mode)
        {
            case LEDFXMode.StaticFX:
                RestorePresetLEDs();
                Log("💡 FX Mode: STATIC PRESET (Restored LEDs)");
                break;
            case LEDFXMode.RainbowContinuous:
            {
                var step = 0;
                RunFx(0.07, () => { step++; for (var k = 0; k < 49; k++) SetLed(k, RainbowVel[(step + pos[k].Col) % RainbowVel.Length]); });
                Log("🌈 FX Mode: CONTINUOUS RAINBOW WAVE");
                break;
            }
            case LEDFXMode.KnightRider:
            {
                var step = 0;
                RunFx(0.05, () =>
                {
                    step = (step + 1) % 20;
                    var active = step <= 10 ? step : 20 - step;
                    for (var k = 0; k < 49; k++)
                    {
                        var d = Math.Abs(pos[k].Col - active);
                        SetLed(k, d == 0 ? 72 : d == 1 ? 61 : d == 2 ? 83 : 0);
                    }
                });
                Log("⚡️ FX Mode: CYLON SCANNER");
                break;
            }
            case LEDFXMode.MatrixRain:
            {
                var step = 0;
                RunFx(0.07, () =>
                {
                    step++;
                    for (var k = 0; k < 49; k++)
                    {
                        var row = pos[k].Row;
                        var drop = (step + pos[k].Col * 3) % 7;
                        SetLed(k, row == drop ? 3 : row == (drop + 6) % 7 ? 87 : row == (drop + 5) % 7 ? 33 : 0);
                    }
                });
                Log("🌧️ FX Mode: DIGITAL MATRIX RAIN");
                break;
            }
            case LEDFXMode.CyberPulse:
            {
                var phase = 0.0;
                RunFx(0.04, () =>
                {
                    phase += 0.12;
                    var vel = (int)((Math.Sin(phase) + 1.0) * 0.5 * 115.0 + 12.0);
                    for (var k = 0; k < 49; k++) SetLed(k, vel);
                });
                Log("🔮 FX Mode: CYBERPUNK PULSE");
                break;
            }
            case LEDFXMode.VuMeter:
            {
                var tick = 0;
                RunFx(0.08, () => UpdateVUMeterFaders(++tick));
                Log("🎚️ FX Mode: LIVE FADER VU METER");
                break;
            }
            case LEDFXMode.FireEmbers:
            {
                int[] pal = [0, 83, 72, 61, 96, 74, 3];
                RunFx(0.08, () =>
                {
                    for (var k = 0; k < 49; k++)
                        SetLed(k, pal[Math.Clamp(4 - pos[k].Row + Random.Shared.Next(-1, 2), 0, pal.Length - 1)]);
                });
                Log("🔥 FX Mode: FIRE & EMBERS");
                break;
            }
            case LEDFXMode.WaterRipple:
            {
                var phase = 0.0;
                int[] pal = [0, 41, 33, 78, 3];
                RunFx(0.05, () =>
                {
                    phase += 0.25;
                    for (var k = 0; k < 49; k++)
                    {
                        var dist = Math.Sqrt(Math.Pow(pos[k].Col - 3.5, 2) + Math.Pow(pos[k].Row - 2.0, 2));
                        var norm = Math.Max(0, Math.Sin(dist * 1.6 - phase));
                        SetLed(k, pal[Math.Clamp((int)(norm * (pal.Length - 1)), 0, pal.Length - 1)]);
                    }
                });
                Log("💧 FX Mode: CENTER RIPPLE");
                break;
            }
            case LEDFXMode.Starfield:
            {
                var stars = new int[49];
                int[] starPal = [3, 78, 33, 41, 74, 95];
                RunFx(0.08, () =>
                {
                    for (var i = 0; i < 49; i++) stars[i] = stars[i] > 14 ? stars[i] - 14 : 0;
                    var n = Random.Shared.Next(2, 5);
                    for (var j = 0; j < n; j++) stars[Random.Shared.Next(49)] = starPal[Random.Shared.Next(starPal.Length)];
                    for (var k = 0; k < 49; k++) SetLed(k, stars[k]);
                });
                Log("✨ FX Mode: TWINKLE STARS");
                break;
            }
            case LEDFXMode.AudioStrobe:
            {
                var count = 0;
                RunFx(0.07, () => { count++; var v = count % 3 == 0 ? 3 : 0; for (var k = 0; k < 49; k++) SetLed(k, v); });
                Log("⚡️ FX Mode: STROBE FLASH");
                break;
            }
            case LEDFXMode.Heartbeat:
            {
                var step = 0;
                RunFx(0.05, () =>
                {
                    step = (step + 1) % 24;
                    var v = step switch { 0 or 4 => 72, 1 or 5 => 61, 2 or 6 => 83, _ => 0 };
                    for (var k = 0; k < 49; k++) SetLed(k, v);
                });
                Log("💓 FX Mode: HEARTBEAT");
                break;
            }
            case LEDFXMode.PlasmaWave:
            {
                var t = 0.0;
                int[] pal = [41, 67, 95, 78, 33, 87, 74, 61, 72];
                RunFx(0.06, () =>
                {
                    t += 0.16;
                    for (var k = 0; k < 49; k++)
                    {
                        double c = pos[k].Col, r = pos[k].Row;
                        var norm = (Math.Sin(c * 0.7 + t) + Math.Cos(r * 0.8 + t * 0.9) + Math.Sin((c + r) * 0.5 + t * 1.1) + 3.0) / 6.0;
                        SetLed(k, pal[Math.Clamp((int)(norm * (pal.Length - 1)), 0, pal.Length - 1)]);
                    }
                });
                Log("🌀 FX Mode: PLASMA FIELD");
                break;
            }
            case LEDFXMode.SpiralVortex:
            {
                var angle = 0.0;
                int[] pal = [0, 41, 67, 78, 33, 3];
                RunFx(0.06, () =>
                {
                    angle += 0.32;
                    for (var k = 0; k < 49; k++)
                    {
                        var keyAngle = Math.Atan2(pos[k].Row - 2.0, pos[k].Col - 4.0);
                        var diff = Mod(keyAngle - angle + Math.PI * 4, Math.PI * 2);
                        var norm = Math.Max(0, 1.0 - diff / (Math.PI * 2));
                        SetLed(k, pal[Math.Clamp((int)(norm * (pal.Length - 1)), 0, pal.Length - 1)]);
                    }
                });
                Log("🌪️ FX Mode: SPIRAL VORTEX");
                break;
            }
            case LEDFXMode.EqDance:
            {
                int[] heights = [1, 2, 3, 4, 3, 2, 4, 2];
                RunFx(0.08, () =>
                {
                    for (var c = 0; c < 8; c++) heights[c] = Math.Clamp(heights[c] + Random.Shared.Next(-2, 3), 0, 5);
                    for (var k = 0; k < 49; k++)
                    {
                        if (pos[k].Col < 8)
                        {
                            var level = 4 - pos[k].Row;
                            SetLed(k, level < heights[pos[k].Col] ? 40 + level * 20 : 0);
                        }
                        else SetLed(k, Random.Shared.Next(20, 71));
                    }
                });
                Log("📊 FX Mode: EQ DANCE");
                break;
            }
            case LEDFXMode.ColorChase:
            {
                var head = 0;
                int[] trail = [3, 78, 87, 74, 61, 72, 95];
                RunFx(0.05, () =>
                {
                    head = (head + 1) % 49;
                    for (var k = 0; k < 49; k++)
                    {
                        var d = (head - k + 49) % 49;
                        SetLed(k, d < trail.Length ? trail[d] : 0);
                    }
                });
                Log("🐍 FX Mode: COLOR RUNNER");
                break;
            }
        }
    }

    private static double Mod(double a, double b) => a - b * Math.Floor(a / b);

    public void UpdateVUMeterFaders(int bgTick = 0)
    {
        double[] thresholds = [15, 40, 65, 90, 115];
        int[] colors = [87, 87, 74, 61, 72];
        for (var col = 0; col < 8; col++)
        {
            double val = Controls[49 + col].LiveValue;
            int[] keys = [32 + col, 24 + col, 16 + col, 8 + col, col];
            for (var seg = 0; seg < 5; seg++) SetLed(keys[seg], val >= thresholds[seg] ? colors[seg] : 0);
        }
        var pad = (int)((Math.Sin(bgTick * 0.2) + 1.0) * 0.5 * 40.0 + 10.0);
        for (var p = 40; p < 49; p++) SetLed(p, pad);
    }

    public void RestorePresetLEDs()
    {
        for (var i = 0; i < 49; i++)
        {
            var c = Controls[i];
            c.CurrentLED = c.IdleVelocity;
            SendNoteOn(c.Number, c.IdleVelocity);
        }
    }

    public async void RunRainbowWaveRefresh()
    {
        if (IsRainbowRunning) return;
        IsRainbowRunning = true;
        Log("✨ Sweeping Rainbow Wave Hardware LED Refresh...");
        for (var step = 0; step < 22; step++)
        {
            for (var k = 0; k < 49; k++)
            {
                var wave = step - KeyMap.Positions[k].Col;
                if (wave >= 0 && wave < RainbowVel.Length) SetLed(k, RainbowVel[wave]);
                else if (wave >= RainbowVel.Length) SetLed(k, Controls[k].IdleVelocity);
            }
            await Task.Delay(50);
        }
        if (CurrentFXMode == LEDFXMode.StaticFX && !IsPlayingCustomFX) RestorePresetLEDs();
        IsRainbowRunning = false;
        Log("✅ Rainbow Wave Refresh Complete");
    }

    public void BlackoutPanic()
    {
        StopFxTimer();
        CurrentFXMode = LEDFXMode.StaticFX;
        for (var i = 0; i < 49; i++)
        {
            Controls[i].IdleVelocity = 0;
            Controls[i].CurrentLED = 0;
            SendNoteOn(Controls[i].Number, 0);
        }
        LastStreamMessage = "🚨 BLACKOUT / PANIC ALL LEDS OFF";
        Log("🚨 PANIC BLACKOUT TRIGGERED");
    }

    // ------------------------------------------------------------------------
    // Reactive key FX (40 fps)
    // ------------------------------------------------------------------------
    public void TriggerReactiveKeyFX(int keyIndex)
    {
        if (keyIndex is < 0 or >= 49 || ReactiveKeyFXMode == ReactiveKeyFXMode.Off) return;
        LastReactiveOriginKey = keyIndex;
        var p = KeyMap.Positions[keyIndex];
        var vel = ReactiveFXRainbowColor
            ? RainbowVel[(keyIndex * 3 + ActiveReactivePulses.Count * 2) % RainbowVel.Length]
            : ReactiveFXColorVelocity;

        int[] targets = [];
        if (ReactiveKeyFXMode == ReactiveKeyFXMode.ArcLightning)
            targets = Enumerable.Range(0, 49).Where(i => i != keyIndex).OrderBy(_ => Random.Shared.Next()).Take(8).ToArray();

        ActiveReactivePulses.Add(new ReactivePulseEvent
        {
            OriginKeyIndex = keyIndex, OriginCol = p.Col, OriginRow = p.Row, Mode = ReactiveKeyFXMode,
            BaseVelocity = vel, Age = 0, LightningTargets = targets,
        });
        if (ActiveReactivePulses.Count > 12) ActiveReactivePulses.RemoveRange(0, ActiveReactivePulses.Count - 12);

        if (_reactiveTimer == null)
        {
            Array.Fill(_lastSentReactive, 255);
            _reactiveTimer = StartTimer(0.025, StepReactiveKeyFX);
        }
        StepReactiveKeyFX();
    }

    public void TriggerRandomDemoReactiveFX()
    {
        int[] center = [11, 12, 19, 20, 18, 21, 44];
        if (ReactiveKeyFXMode == ReactiveKeyFXMode.Off) ReactiveKeyFXMode = ReactiveKeyFXMode.Ripple;
        TriggerReactiveKeyFX(center[Random.Shared.Next(center.Length)]);
        Play(SoundEffect.KeyTap);
    }

    private void StepReactiveKeyFX()
    {
        var dt = 0.025 * Math.Max(0.3, ReactiveFXSpeed);
        foreach (var pulse in ActiveReactivePulses) pulse.Age += dt;
        ActiveReactivePulses.RemoveAll(p => p.Age > 1.05);

        for (var k = 0; k < 49; k++)
        {
            var c = Controls[k];
            var baseVel = c.IsPressed || (c.FeedbackMode == FeedbackMode.Toggle && c.ToggleLatched) ? c.ActiveVelocity : c.IdleVelocity;
            var effectVel = 0;
            var maxI = 0.0;
            double kc = KeyMap.Positions[k].Col, kr = KeyMap.Positions[k].Row;

            foreach (var p in ActiveReactivePulses)
            {
                var dist = KeyMap.Distances[k, p.OriginKeyIndex];
                double dx = kc - p.OriginCol, dy = kr - p.OriginRow;
                var intensity = 0.0;
                switch (p.Mode)
                {
                    case ReactiveKeyFXMode.Ripple:
                    {
                        var ring = Math.Abs(dist - p.Age * 11.5);
                        if (ring < 1.45) intensity = (1.0 - ring / 1.45) * Math.Max(0, 1.0 - p.Age * 0.75);
                        break;
                    }
                    case ReactiveKeyFXMode.CrossLaser:
                    {
                        var beam = p.Age * 11.0;
                        if ((int)kr == (int)p.OriginRow)
                        {
                            var d = Math.Abs(Math.Abs(dx) - beam);
                            if (d < 1.6) intensity = (1.0 - d / 1.6) * (1.0 - p.Age * 0.65);
                        }
                        if ((int)kc == (int)p.OriginCol)
                        {
                            var d = Math.Abs(Math.Abs(dy) - beam * 0.65);
                            if (d < 1.3) intensity = Math.Max(intensity, (1.0 - d / 1.3) * (1.0 - p.Age * 0.65));
                        }
                        break;
                    }
                    case ReactiveKeyFXMode.Supernova:
                    {
                        var maxR = 6.5 * Math.Sin(p.Age * Math.PI);
                        if (dist <= maxR) intensity = (1.0 - dist / Math.Max(0.5, maxR)) * (1.0 - p.Age * 0.5);
                        break;
                    }
                    case ReactiveKeyFXMode.HeatTrail:
                        if (k == p.OriginKeyIndex) intensity = Math.Max(0, 1.0 - p.Age);
                        else if (dist <= 1.5) intensity = Math.Max(0, (0.65 - p.Age * 0.85) * (1.0 - dist / 1.6));
                        break;
                    case ReactiveKeyFXMode.MeteorRain:
                        if ((int)kc == (int)p.OriginCol)
                        {
                            var drop = (p.OriginRow + p.Age * 7.5) % 5.0;
                            var d = Math.Abs(kr - drop);
                            if (d < 1.35) intensity = (1.0 - d / 1.35) * (1.0 - p.Age * 0.65);
                        }
                        break;
                    case ReactiveKeyFXMode.ArcLightning:
                        if (k == p.OriginKeyIndex || p.LightningTargets.Contains(k))
                            intensity = (1.0 - p.Age * 0.85) * (Math.Sin(p.Age * 42.0 + k) > -0.2 ? 1.0 : 0.35);
                        break;
                }

                if (intensity > maxI)
                {
                    maxI = intensity;
                    if (ReactiveFXRainbowColor)
                    {
                        var idx = Math.Abs((int)Math.Round(dist + p.Age * 8.0) + p.OriginKeyIndex) % RainbowVel.Length;
                        effectVel = intensity > 0.72 ? 3 : RainbowVel[idx];
                    }
                    else effectVel = intensity > 0.82 ? 3 : p.BaseVelocity;
                }
            }

            var final = maxI > 0.14 ? effectVel : baseVel;
            c.CurrentLED = final;
            if (_lastSentReactive[k] != final)
            {
                _lastSentReactive[k] = final;
                SendNoteOn(c.Number, final);
            }
        }

        if (ActiveReactivePulses.Count == 0)
        {
            _reactiveTimer?.Stop();
            _reactiveTimer = null;
        }
    }

    // ------------------------------------------------------------------------
    // Custom FX studio
    // ------------------------------------------------------------------------
    private int SafeFrameIndex => Math.Clamp(EditingFrameIndex, 0, Math.Max(0, EditingCustomFX.Frames.Count - 1));
    private int[] CurrentFrame => EditingCustomFX.Frames[SafeFrameIndex];

    private void LoadCustomEffectsFromDisk()
    {
        ConnectEffectEnabled = _settings.ConnectFXEnabled;
        if (!string.IsNullOrEmpty(_settings.ConnectFXName)) ConnectCustomFXName = _settings.ConnectFXName;
        var decoded = Storage.Load<List<CustomLightEffect>>("effects.json");
        if (decoded is { Count: > 0 })
        {
            if (!decoded.Any(e => e.Name == "BITCHBOY CONNECT IGNITION")) decoded.Insert(0, CustomLightEffect.BuildDefaultConnectEffect());
            CustomEffects = decoded;
        }
        else
        {
            CustomEffects = CustomLightEffect.BuildStarterEffects();
            SaveCustomEffectsToDisk();
        }
        EditingCustomFX = CustomEffects[0].Clone();
    }

    public void SaveCustomEffectsToDisk() => Storage.Save("effects.json", CustomEffects);

    public void SaveEditingCustomFXToLibrary()
    {
        var idx = CustomEffects.FindIndex(e => e.Id == EditingCustomFX.Id);
        if (idx >= 0) CustomEffects[idx] = EditingCustomFX.Clone();
        else
        {
            var copy = EditingCustomFX.Clone();
            copy.Id = Guid.NewGuid();
            CustomEffects.Insert(0, copy);
            EditingCustomFX = copy.Clone();
        }
        SaveCustomEffectsToDisk();
        Raise(nameof(CustomEffects));
        Play(SoundEffect.PresetSwitch);
        Log($"💾 Saved Custom Light Show: '{EditingCustomFX.Name}'");
    }

    public void CreateNewBlankCustomFX()
    {
        EditingCustomFX = new CustomLightEffect
        {
            Name = $"CUSTOM SHOW #{CustomEffects.Count + 1}", Frames = [CustomLightEffect.EmptyFrame()], Fps = 10, LoopMode = CustomFXLoopMode.Loop,
        };
        EditingFrameIndex = 0;
        PreviewCurrentCustomFrameOnHardware();
        Log("✨ Created new blank Custom Light Show");
    }

    public void DeleteSavedCustomFX(CustomLightEffect fx)
    {
        CustomEffects.RemoveAll(e => e.Id == fx.Id);
        if (CustomEffects.Count == 0) CustomEffects = CustomLightEffect.BuildStarterEffects();
        SaveCustomEffectsToDisk();
        Raise(nameof(CustomEffects));
        Log($"🗑️ Deleted Custom Show '{fx.Name}'");
    }

    public void SelectCustomFXForEditing(CustomLightEffect fx)
    {
        EditingCustomFX = fx.Clone();
        EditingFrameIndex = 0;
        RightWingTab = fx.IsProcedural ? "WAVESYNTH" : "SEQUENCER";
        PreviewCurrentCustomFrameOnHardware();
        Log($"✏️ Loaded '{fx.Name}' into FX Studio");
    }

    public void PreviewCurrentCustomFrameOnHardware()
    {
        if (EditingCustomFX.Frames.Count == 0) return;
        var frame = CurrentFrame;
        for (var k = 0; k < Math.Min(49, frame.Length); k++)
        {
            Controls[k].CurrentLED = frame[k];
            SendNoteOn(Controls[k].Number, frame[k]);
        }
    }

    public void SetEditingFrame(int index)
    {
        EditingFrameIndex = Math.Clamp(index, 0, Math.Max(0, EditingCustomFX.Frames.Count - 1));
        PreviewCurrentCustomFrameOnHardware();
    }

    public void PaintCustomFXKey(int keyIndex, int velocity)
    {
        if (keyIndex is < 0 or >= 49) return;
        if (EditingCustomFX.Frames.Count == 0) EditingCustomFX.Frames = [CustomLightEffect.EmptyFrame()];
        EditingFrameIndex = SafeFrameIndex;
        var frame = CurrentFrame;
        var nv = frame[keyIndex] == velocity && velocity > 0 ? 0 : velocity;
        frame[keyIndex] = nv;
        Controls[keyIndex].CurrentLED = nv;
        SendNoteOn(Controls[keyIndex].Number, nv);
        TouchEditingFX();
        Play(SoundEffect.KeyTap);
    }

    private void FrameEdited(bool tick = false)
    {
        TouchEditingFX();
        PreviewCurrentCustomFrameOnHardware();
        if (tick) Play(SoundEffect.KnobTick);
    }

    public void AddCustomFXFrame()
    {
        EditingCustomFX.Frames.Add(CustomLightEffect.EmptyFrame());
        EditingFrameIndex = EditingCustomFX.Frames.Count - 1;
        FrameEdited(true);
    }

    public void DuplicateCustomFXFrame()
    {
        if (EditingCustomFX.Frames.Count == 0) return;
        var i = SafeFrameIndex;
        EditingCustomFX.Frames.Insert(i + 1, (int[])CurrentFrame.Clone());
        EditingFrameIndex = i + 1;
        FrameEdited(true);
    }

    public void DeleteCustomFXFrame()
    {
        if (EditingCustomFX.Frames.Count <= 1) { ClearCurrentCustomFXFrame(); return; }
        var i = SafeFrameIndex;
        EditingCustomFX.Frames.RemoveAt(i);
        EditingFrameIndex = Math.Max(0, i - 1);
        FrameEdited();
    }

    public void ClearCurrentCustomFXFrame() => FillCurrentCustomFXFrame(0);

    public void FillCurrentCustomFXFrame(int vel)
    {
        if (EditingCustomFX.Frames.Count == 0) return;
        EditingCustomFX.Frames[SafeFrameIndex] = CustomLightEffect.EmptyFrame(vel);
        FrameEdited();
    }

    public void ShiftCurrentFrame(int dx, int dy)
    {
        if (EditingCustomFX.Frames.Count == 0) return;
        var old = CurrentFrame;
        var nf = CustomLightEffect.EmptyFrame();
        for (var k = 0; k < 49; k++)
        {
            var p = KeyMap.Positions[k];
            var src = KeyMap.IndexAt((p.Col - dx + 11) % 11, (p.Row - dy + 5) % 5);
            if (src >= 0) nf[k] = old[src];
        }
        EditingCustomFX.Frames[SafeFrameIndex] = nf;
        FrameEdited(true);
    }

    public void InvertCurrentFrame()
    {
        if (EditingCustomFX.Frames.Count == 0) return;
        var f = CurrentFrame;
        for (var k = 0; k < 49; k++) f[k] = f[k] == 0 ? CustomBrushVelocity : 0;
        FrameEdited();
    }

    public void MirrorCurrentFrameHorizontal()
    {
        if (EditingCustomFX.Frames.Count == 0) return;
        var old = CurrentFrame;
        var nf = (int[])old.Clone();
        for (var k = 0; k < 49; k++)
        {
            var p = KeyMap.Positions[k];
            var src = KeyMap.IndexAt(p.Col < 8 ? 7 - p.Col : 18 - p.Col, p.Row);
            if (src >= 0) nf[k] = old[src];
        }
        EditingCustomFX.Frames[SafeFrameIndex] = nf;
        FrameEdited();
    }

    public void RandomizeCurrentFrameSparks()
    {
        if (EditingCustomFX.Frames.Count == 0) return;
        int[] pal = [0, 0, 72, 61, 96, 74, 87, 33, 78, 41, 67, 95, 3];
        var f = CurrentFrame;
        for (var k = 0; k < 49; k++) f[k] = pal[Random.Shared.Next(pal.Length)];
        FrameEdited();
    }

    public void ApplyRainbowToCurrentFrame()
    {
        if (EditingCustomFX.Frames.Count == 0) return;
        var i = SafeFrameIndex;
        var f = CurrentFrame;
        for (var k = 0; k < 49; k++) f[k] = RainbowVel[(KeyMap.Positions[k].Col + i) % RainbowVel.Length];
        FrameEdited();
    }

    public static int EvaluateProceduralWave(CustomLightEffect fx, int keyIndex, double phase)
    {
        var p = KeyMap.Positions[keyIndex];
        var x = p.Col / 10.0;
        var y = p.Row / 4.0;
        var cx = x - 0.45;
        var cy = y - 0.5;
        var radial = Math.Sqrt(cx * cx + cy * cy);
        var coord = fx.WaveDirection switch
        {
            CustomWaveDirection.TopToBottom => y,
            CustomWaveDirection.Diagonal => (x + y) * 0.5,
            CustomWaveDirection.CenterOut => radial * 1.4,
            CustomWaveDirection.OuterIn => 1.0 - Math.Min(1.0, radial * 1.4),
            _ => x,
        };
        var f = coord * fx.WaveFrequency - phase;
        var arg = f * 2.0 * Math.PI;
        var norm = fx.WaveShape switch
        {
            CustomWaveShape.Sawtooth => f - Math.Floor(f),
            CustomWaveShape.Triangle => (f - Math.Floor(f)) is var fr && fr < 0.5 ? fr * 2.0 : 1.0 - (fr - 0.5) * 2.0,
            CustomWaveShape.Square => Math.Sin(arg) >= 0 ? 1.0 : 0.0,
            CustomWaveShape.Radial => (Math.Sin((radial * 3.0 * fx.WaveFrequency - phase) * 2.0 * Math.PI) + 1.0) * 0.5,
            CustomWaveShape.Spiral => (Math.Sin((Math.Atan2(cy, cx) / (2.0 * Math.PI) * 2.0 + radial * fx.WaveFrequency - phase) * 2.0 * Math.PI) + 1.0) * 0.5,
            CustomWaveShape.Sparkle => Math.Sin(keyIndex * 37 + Math.Floor(phase * 12.0) * 17.3) is var h && h > 0.25 ? (h - 0.25) / 0.75 : 0.0,
            _ => (Math.Sin(arg) + 1.0) * 0.5,
        };
        double lo = Math.Min(fx.MinVelocity, fx.MaxVelocity), hi = Math.Max(fx.MinVelocity, fx.MaxVelocity);
        return Math.Clamp((int)Math.Round(lo + norm * (hi - lo)), 0, 127);
    }

    public void BakeProceduralWaveToFrames()
    {
        var fx = EditingCustomFX;
        var baked = new List<int[]>();
        for (var i = 0; i < 12; i++)
        {
            var frame = CustomLightEffect.EmptyFrame();
            for (var k = 0; k < 49; k++) frame[k] = EvaluateProceduralWave(fx, k, i / 12.0);
            baked.Add(frame);
        }
        fx.Frames = baked;
        fx.IsProcedural = false;
        EditingFrameIndex = 0;
        RightWingTab = "SEQUENCER";
        FrameEdited();
        Play(SoundEffect.PresetSwitch);
        Log("⚡ Baked Procedural Wave into 12 editable keyframes!");
    }

    public void PlayCustomEffect(CustomLightEffect fx)
    {
        StopFxTimer();
        CurrentFXMode = LEDFXMode.StaticFX;
        VuMeterLEDsEnabled = false;
        EditingCustomFX = fx;
        IsPlayingCustomFX = true;
        ActiveCustomFXName = fx.Name;
        _customFXStep = 0;
        _customFXDirection = 1;
        var interval = Math.Max(0.035, 1.0 / Math.Max(1.0, fx.Fps * FxSpeed));
        var phase = 0.0;
        _fxTimer = StartTimer(interval, () =>
        {
            var active = EditingCustomFX;
            if (active.IsProcedural)
            {
                phase += 0.06 * FxSpeed;
                for (var k = 0; k < 49; k++) SetLed(k, EvaluateProceduralWave(active, k, phase));
                return;
            }
            var count = active.Frames.Count;
            if (count == 0) return;
            var idx = Math.Clamp(_customFXStep, 0, count - 1);
            if (EditingFrameIndex != idx) EditingFrameIndex = idx;
            var frame = active.Frames[idx];
            for (var k = 0; k < Math.Min(49, frame.Length); k++) SetLed(k, frame[k]);
            if (count <= 1) return;
            switch (active.LoopMode)
            {
                case CustomFXLoopMode.Loop:
                    _customFXStep = (_customFXStep + 1) % count;
                    break;
                case CustomFXLoopMode.PingPong:
                    var next = _customFXStep + _customFXDirection;
                    if (next >= count) { _customFXDirection = -1; _customFXStep = Math.Max(0, count - 2); }
                    else if (next < 0) { _customFXDirection = 1; _customFXStep = Math.Min(count - 1, 1); }
                    else _customFXStep = next;
                    break;
                case CustomFXLoopMode.OneShot:
                    if (_customFXStep + 1 < count) _customFXStep++;
                    else StopCustomEffect();
                    break;
            }
        });
        Log($"▶️ Playing Custom FX: {fx.Name}");
    }

    public void StopCustomEffect()
    {
        IsPlayingCustomFX = false;
        ActiveCustomFXName = "";
        StopFxTimer();
        RestorePresetLEDs();
        Log("⏹ Stopped Custom FX (Restored Preset LEDs)");
    }

    public void ExportCustomFXToClipboard()
    {
        try { System.Windows.Clipboard.SetText(Storage.Serialize(EditingCustomFX)); } catch { }
        Log($"⤓ Exported Custom FX '{EditingCustomFX.Name}' to Clipboard JSON");
    }

    public void ImportCustomFXFromClipboard()
    {
        string text;
        try { text = System.Windows.Clipboard.GetText(); } catch { text = ""; }
        var decoded = Storage.Deserialize<CustomLightEffect>(text);
        if (decoded == null || decoded.Frames.Count == 0)
        {
            Log("❌ Clipboard does not contain valid Custom FX JSON");
            return;
        }
        decoded.Id = Guid.NewGuid();
        CustomEffects.Insert(0, decoded);
        EditingCustomFX = decoded.Clone();
        EditingFrameIndex = 0;
        SaveCustomEffectsToDisk();
        Raise(nameof(CustomEffects));
        PreviewCurrentCustomFrameOnHardware();
        Log($"⤒ Imported Custom FX '{decoded.Name}'!");
    }
}
