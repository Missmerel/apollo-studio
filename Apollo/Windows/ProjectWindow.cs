using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Text;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

using Newtonsoft.Json;

using Apollo.Core;
using Apollo.Elements;
using Apollo.Viewers;

namespace Apollo.Windows {
    public class ProjectWindow: Window {
        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        public ProjectWindow() {
            InitializeComponent();
            #if DEBUG
                this.AttachDevTools();
            #endif
            
            Icon = new WindowIcon(Assembly.GetExecutingAssembly().GetManifestResourceStream("Apollo.Resources.WindowIcon.png"));

            Program.Project.Window = this;

            Controls Contents = this.Get<StackPanel>("Contents").Children;
            
            foreach (Track track in Program.Project.Tracks)
                Contents.Add(new TrackViewer(track));
        }

        private void Unloaded(object sender, EventArgs e) {
            Program.Project.Window = null;
        }

        private void MoveWindow(object sender, PointerPressedEventArgs e) {
            BeginMoveDrag();
        }
    }
}