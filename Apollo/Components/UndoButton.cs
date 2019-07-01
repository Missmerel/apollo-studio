﻿using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.VisualTree;

using Apollo.Core;
using Apollo.Windows;

namespace Apollo.Components {
    public class UndoButton: IconButton {
        void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        void Update_Position(int position) => Enabled = position != 0;

        protected override IBrush Fill {
            get => (IBrush)this.Resources["Brush"];
            set => this.Resources["Brush"] = value;
        }

        public UndoButton() {
            InitializeComponent();

            AllowRightClick = true;
            base.MouseLeave(this, null);

            Program.Project.Undo.PositionChanged += Update_Position;
            Update_Position(Program.Project.Undo.Position);
        }

        protected override void Unloaded(object sender, VisualTreeAttachmentEventArgs e) {
            base.Unloaded(sender, e);
            Program.Project.Undo.PositionChanged -= Update_Position;
        }

        protected override void Click(PointerReleasedEventArgs e) {
            if (e.MouseButton == MouseButton.Left) Program.Project.Undo.Undo();
            else if (e.MouseButton == MouseButton.Right) UndoWindow.Create((Window)this.GetVisualRoot());
        }
    }
}
