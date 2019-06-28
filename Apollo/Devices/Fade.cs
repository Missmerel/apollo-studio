using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

using Apollo.Core;
using Apollo.DeviceViewers;
using Apollo.Elements;
using Apollo.Enums;
using Apollo.Structures;

namespace Apollo.Devices {
    public class Fade: Device {
        public static readonly new string DeviceIdentifier = "fade";

        private class FadeInfo {
            public Color Color;
            public double Time;

            public FadeInfo(Color color, double time) {
                Color = color;
                Time = time;
            }
        }

        private List<Color> _colors = new List<Color>();
        private List<decimal> _positions = new List<decimal>();
        private List<FadeInfo> fade;

        public Color GetColor(int index) => _colors[index];
        public void SetColor(int index, Color color) {
            if (_colors[index] != color) {
                _colors[index] = color;
                Generate();

                if (Viewer?.SpecificViewer != null) ((FadeViewer)Viewer.SpecificViewer).SetColor(index, _colors[index]);
            }
        }

        public decimal GetPosition(int index) => _positions[index];
        public void SetPosition(int index, decimal position) {
            if (_positions[index] != position) {
                _positions[index] = position;
                Generate();
                
                if (Viewer?.SpecificViewer != null) ((FadeViewer)Viewer.SpecificViewer).SetPosition(index, _positions[index]);
            }
        }
        
        private ConcurrentDictionary<Signal, int> buffer = new ConcurrentDictionary<Signal, int>();
        private ConcurrentDictionary<Signal, object> locker = new ConcurrentDictionary<Signal, object>();
        private ConcurrentDictionary<Signal, List<Courier>> timers = new ConcurrentDictionary<Signal, List<Courier>>();

        private Time _time;
        public Time Time {
            get => _time;
            set {
                if (_time != null) {
                    _time.FreeChanged -= FreeChanged;
                    _time.ModeChanged -= ModeChanged;
                    _time.StepChanged -= StepChanged;
                }

                _time = value;

                if (_time != null) {
                    _time.Minimum = 10;
                    _time.Maximum = 30000;

                    _time.FreeChanged += FreeChanged;
                    _time.ModeChanged += ModeChanged;
                    _time.StepChanged += StepChanged;
                }
            }
        }

        private void FreeChanged(int value) {
            Generate();
            if (Viewer?.SpecificViewer != null) ((FadeViewer)Viewer.SpecificViewer).SetDurationValue(value);
        }

        private void ModeChanged(bool value) {
            Generate();
            if (Viewer?.SpecificViewer != null) ((FadeViewer)Viewer.SpecificViewer).SetMode(value);
        }

        private void StepChanged(Length value) {
            Generate();
            if (Viewer?.SpecificViewer != null) ((FadeViewer)Viewer.SpecificViewer).SetDurationStep(value);
        }

        private decimal _gate;
        public decimal Gate {
            get => _gate;
            set {
                if (0.01M <= value && value <= 4) {
                    _gate = value;
                    Generate();
                    
                    if (Viewer?.SpecificViewer != null) ((FadeViewer)Viewer.SpecificViewer).SetGate(Gate);
                }
            }
        }

        private FadePlaybackType _mode;
        public FadePlaybackType PlayMode {
            get => _mode;
            set {
                _mode = value;

                if (Viewer?.SpecificViewer != null) ((FadeViewer)Viewer.SpecificViewer).SetPlaybackMode(PlayMode);
            }
        }

        public delegate void GeneratedEventHandler();
        public event GeneratedEventHandler Generated;

        private void Generate() => Generate(Preferences.FadeSmoothness);

        private void Generate(double smoothness) {
            if (_colors.Count < 2 || _positions.Count < 2) return;

            List<Color> _steps = new List<Color>();
            List<int> _counts = new List<int>();
            List<int> _cutoffs = new List<int>() {0};

            for (int i = 0; i < _colors.Count - 1; i++) {
                int max = new int[] {
                    Math.Abs(_colors[i].Red - _colors[i + 1].Red),
                    Math.Abs(_colors[i].Green - _colors[i + 1].Green),
                    Math.Abs(_colors[i].Blue - _colors[i + 1].Blue),
                    1
                }.Max();

                for (int k = 0; k < max; k++) {
                    _steps.Add(new Color(
                        (byte)(_colors[i].Red + (_colors[i + 1].Red - _colors[i].Red) * k / max),
                        (byte)(_colors[i].Green + (_colors[i + 1].Green - _colors[i].Green) * k / max),
                        (byte)(_colors[i].Blue + (_colors[i + 1].Blue - _colors[i].Blue) * k / max)
                    ));
                }

                _counts.Add(max);
                _cutoffs.Add(max + _cutoffs.Last());
            }

            _steps.Add(_colors.Last());

            if (_steps.Last().Lit) {
                _steps.Add(new Color(0));
                _cutoffs[_cutoffs.Count - 1]++;
            }

            fade = new List<FadeInfo>() {new FadeInfo(_steps[0], 0)};

            int j = 0;
            for (int i = 1; i < _steps.Count; i++) {
                if (_cutoffs[j + 1] == i) j++;

                if (j < _colors.Count - 1) {
                    double time = (double)((_positions[j] + (_positions[j + 1] - _positions[j]) * (i - _cutoffs[j]) / _counts[j]) * _time * _gate);
                    if (fade.Last().Time + smoothness < time) fade.Add(new FadeInfo(_steps[i], time));
                }
            }

            fade.Add(new FadeInfo(_steps.Last(), (double)(_time * _gate)));
            
            Generated?.Invoke();
        }

        public int Count {
            get => _colors.Count;
        }

        public override Device Clone() => new Fade(_time.Clone(), _gate, PlayMode, (from i in _colors select i.Clone()).ToList(), _positions.ToList()) {
            Collapsed = Collapsed,
            Enabled = Enabled
        };

        public void Insert(int index, Color color, decimal position) {
            _colors.Insert(index, color);
            _positions.Insert(index, position);

            if (Viewer?.SpecificViewer != null) {
                FadeViewer SpecificViewer = ((FadeViewer)Viewer.SpecificViewer);
                SpecificViewer.Contents_Insert(index, _colors[index]);

                SpecificViewer.Expand(index);
            }

            Generate();
        }

        public void Remove(int index) {
            _colors.RemoveAt(index);
            _positions.RemoveAt(index);

            if (Viewer?.SpecificViewer != null) ((FadeViewer)Viewer.SpecificViewer).Contents_Remove(index);

            Generate();
        }

        public Fade(Time time = null, decimal gate = 1, FadePlaybackType playmode = FadePlaybackType.Mono, List<Color> colors = null, List<decimal> positions = null): base(DeviceIdentifier) {
            Time = time?? new Time();
            Gate = gate;
            PlayMode = playmode;

            _colors = colors?? new List<Color>() {new Color(63), new Color(0)};
            _positions = positions?? new List<decimal>() {0, 1};

            if (Program.Project == null) Program.ProjectLoaded += Generate;
            else Generate();

            Preferences.FadeSmoothnessChanged += Generate;
        }

        private void FireCourier(Signal n, double time) {
            Courier courier;

            timers[n].Add(courier = new Courier() {
                Info = n,
                AutoReset = false,
                Interval = time,
            });
            courier.Elapsed += Tick;
            courier.Start();
        }

        private void Tick(object sender, EventArgs e) {
            if (Disposed) return;

            Courier courier = (Courier)sender;
            courier.Elapsed -= Tick;

            if (courier.Info.GetType() == typeof(Signal)) {
                Signal n = (Signal)courier.Info;

                lock (locker[n]) {
                    if (PlayMode == FadePlaybackType.Loop && !timers[n].Contains(courier)) return;

                    if (++buffer[n] == fade.Count - 1 && PlayMode == FadePlaybackType.Loop) {
                        Stop(n);
                        
                        for (int i = 1; i < fade.Count; i++)
                            FireCourier(n, fade[i].Time);
                    }
                    
                    if (buffer[n] < fade.Count) {
                        Signal m = n.Clone();
                        m.Color = fade[buffer[n]].Color.Clone();
                        MIDIExit?.Invoke(m);
                    }
                }
            }
        }

        private void Stop(Signal n) {
            if (!locker.ContainsKey(n)) locker[n] = new object();

            lock (locker[n]) {
                if (timers.ContainsKey(n))
                    for (int i = 0; i < timers[n].Count; i++)
                        timers[n][i].Dispose();
                
                if (PlayMode == FadePlaybackType.Loop && buffer.ContainsKey(n) && buffer[n] < fade.Count - 1) {
                    Signal m = n.Clone();
                    m.Color = fade.Last().Color.Clone();
                    MIDIExit?.Invoke(m);
                }

                timers[n] = new List<Courier>();
                buffer[n] = 0;
            }
        }

        public override void MIDIProcess(Signal n) {
            if (_colors.Count > 0) {
                bool lit = n.Color.Lit;
                n.Color = new Color();

                if (!locker.ContainsKey(n)) locker[n] = new object();

                lock (locker[n]) {
                    if ((PlayMode == FadePlaybackType.Mono && lit) || PlayMode == FadePlaybackType.Loop) Stop(n);

                    if (lit) {
                        Signal m = n.Clone();
                        m.Color = fade[0].Color.Clone();
                        MIDIExit?.Invoke(m);
                        
                        for (int i = 1; i < fade.Count; i++)
                            FireCourier(n, fade[i].Time);
                    }
                }
            }
        }

        protected override void Stop() {
            buffer.Clear();
            locker.Clear();
            
            foreach (List<Courier> i in timers.Values) {
                foreach (Courier j in i) j.Dispose();
                i.Clear();
            }
            timers.Clear();
        }

        public override void Dispose() {
            Generated = null;
            Preferences.FadeSmoothnessChanged -= Generate;

            Time.Dispose();
            base.Dispose();
        }
    }
}