using Apollo.Core;
using Apollo.DeviceViewers;
using Apollo.Elements;
using Apollo.Structures;

namespace Apollo.Devices {
    public class Output: Device {
        public static readonly new string DeviceIdentifier = "output";

        public delegate void TargetChangedEventHandler(int value);
        public event TargetChangedEventHandler TargetChanged;
        
        private int _target;
        public int Target {
            get => _target;
            set {
                if (_target != value) {
                    Program.Project.Tracks[_target].ParentIndexChanged -= IndexChanged;
                    Program.Project.Tracks[_target].Disposing -= IndexRemoved;
                    
                    _target = value;
                    Program.Project.Tracks[_target].ParentIndexChanged += IndexChanged;
                    Program.Project.Tracks[_target].Disposing += IndexRemoved;
                    
                    if (Viewer?.SpecificViewer != null) ((OutputViewer)Viewer.SpecificViewer).Update_Target(Target);
                }
            }
        }

        private void IndexChanged(int value) {
            _target = value;
            TargetChanged?.Invoke(_target);
        }

        private void IndexRemoved() {
            Target = Track.Get(this).ParentIndex.Value;
            TargetChanged?.Invoke(_target);
        }

        public Launchpad Launchpad => Program.Project.Tracks[Target].Launchpad;

        public override Device Clone() => new Output(Target) {
            Collapsed = Collapsed,
            Enabled = Enabled
        };

        public Output(int target = -1): base(DeviceIdentifier) {
            if (target < 0) target = Track.Get(this).ParentIndex.Value;
            _target = target;

            if (Program.Project == null) Program.ProjectLoaded += Initialize;
            else Initialize();
        }

        private void Initialize() {
            Program.Project.Tracks[_target].ParentIndexChanged += IndexChanged;
            Program.Project.Tracks[_target].Disposing += IndexRemoved;
        }

        public override void MIDIProcess(Signal n) {
            n.Source = Launchpad;
            MIDIExit?.Invoke(n);
        }
    }
}