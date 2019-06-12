﻿using System;
using System.Globalization;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Input;

using Apollo.Core;
using Apollo.Structures;

namespace Apollo.Components {
    public class LaunchpadGrid: UserControl {
        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
        
        StackPanel Root;
        UniformGrid Grid;
        Path TopLeft, TopRight, BottomLeft, BottomRight;
        Rectangle ModeLight;

        public delegate void PadChangedEventHandler(int index);
        public event PadChangedEventHandler PadStarted;
        public event PadChangedEventHandler PadFinished;
        public event PadChangedEventHandler PadPressed;
        public event PadChangedEventHandler PadReleased;

        public delegate void PadModsChangedEventHandler(int index, InputModifiers mods);
        public event PadModsChangedEventHandler PadModsPressed;

        public static int GridToSignal(int index) => (index == -1)? 99 : ((9 - (index / 10)) * 10 + index % 10);
        public static int SignalToGrid(int index) => (index == 99)? -1 : ((9 - (index / 10)) * 10 + index % 10);

        public void SetColor(int index, SolidColorBrush color) {
            if (index == 0 || index == 9 || index == 90 || index == 99) return;

            if (index == -1) ModeLight.Fill = color;
            else if (LowQuality) ((Path)Grid.Children[index]).Fill = color;
            else ((Path)Grid.Children[index]).Stroke = Preferences.Phantom? color : ((Path)Grid.Children[index]).Fill = color;
        }

        private void Update_Phantom(bool value) {
            if (LowQuality) return;

            for (int i = 0; i < 100; i++) {
                if (i == 0 || i == 9 || i == 90 || i == 99) continue;

                ((Path)Grid.Children[i]).Fill = value? SolidColorBrush.Parse("Transparent") : ((Path)Grid.Children[i]).Stroke;
            }
        }

        private double _scale = 1;
        public double Scale {
            get => _scale;
            set {
                value = Math.Max(0, value);
                if (value != _scale) {
                    _scale = value;

                    this.Resources["PadSize"] = 15 * Scale;
                    if (!LowQuality) this.Resources["PadThickness"] = 1 * Scale;
                    this.Resources["PadCut1"] = 3 * Scale;
                    this.Resources["PadCut2"] = 12 * Scale;
                    this.Resources["ModeWidth"] = 4 * Scale;
                    this.Resources["ModeHeight"] = 2 * Scale;
                    this.Resources["PadMargin"] = new Thickness(1 * Scale);
                    this.Resources["ModeMargin"] = new Thickness(0, 5 * Scale, 0, 0);
                    
                    DrawPath();
                }
            }
        }
        
        private bool _lowQuality = false;
        public bool LowQuality {
            get => _lowQuality;
            set {
                if (value != _lowQuality) {
                    _lowQuality = value;

                    this.Resources["PadThickness"] = LowQuality? 0 : 1 * Scale;
                    ModeLight.Opacity = Convert.ToInt32(!LowQuality);
                    
                    DrawPath();
                }
            }
        }

        private readonly Geometry LowQualityGeometry = Geometry.Parse("M 0,0 L 0,1 1,1 1,0 Z");

        public Geometry SquareGeometry => Geometry.Parse(String.Format("M {1},{1} L {1},{0} {0},{0} {0},{1} Z",
            ((double)this.Resources["PadSize"] - (double)this.Resources["PadThickness"] / 2).ToString(CultureInfo.InvariantCulture),
            ((double)this.Resources["PadThickness"] / 2).ToString(CultureInfo.InvariantCulture)
        ));

        public Geometry CircleGeometry => Geometry.Parse(String.Format("M {0},{1} A {2},{2} 180 1 1 {0},{3} A {2},{2} 180 1 1 {0},{1} Z",
            ((double)this.Resources["PadSize"] / 2).ToString(CultureInfo.InvariantCulture),
            ((double)this.Resources["PadSize"] / 8 + (double)this.Resources["PadThickness"] / 2).ToString(CultureInfo.InvariantCulture),
            ((double)this.Resources["PadSize"] * 3 / 8 - (double)this.Resources["PadThickness"] / 2).ToString(CultureInfo.InvariantCulture),
            ((double)this.Resources["PadSize"] * 7 / 8 - (double)this.Resources["PadThickness"] / 2).ToString(CultureInfo.InvariantCulture)
        ));

        public Geometry CreateCornerGeometry(string format) => Geometry.Parse(String.Format(format,
            ((double)this.Resources["PadSize"] - (double)this.Resources["PadThickness"] / 2).ToString(CultureInfo.InvariantCulture),
            ((double)this.Resources["PadCut1"] + (double)this.Resources["PadThickness"] / 2).ToString(CultureInfo.InvariantCulture),
            ((double)this.Resources["PadCut2"] - (double)this.Resources["PadThickness"] / 2).ToString(CultureInfo.InvariantCulture),
            ((double)this.Resources["PadThickness"] / 2).ToString(CultureInfo.InvariantCulture)
        ));

        public void DrawPath() {
            this.Resources["SquareGeometry"] = LowQuality? LowQualityGeometry : SquareGeometry;
            this.Resources["CircleGeometry"] = LowQuality? LowQualityGeometry : CircleGeometry;

            TopLeft.Data = LowQuality? LowQualityGeometry : CreateCornerGeometry("M {3},{3} L {3},{0} {2},{0} {0},{2} {0},{3} Z");
            TopRight.Data = LowQuality? LowQualityGeometry : CreateCornerGeometry("M {3},{3} L {3},{2} {1},{0} {0},{0} {0},{3} Z");
            BottomLeft.Data = LowQuality? LowQualityGeometry : CreateCornerGeometry("M {3},{3} L {3},{0} {0},{0} {0},{1} {2},{3} Z");
            BottomRight.Data = LowQuality? LowQualityGeometry : CreateCornerGeometry("M {3},{1} L {3},{0} {0},{0} {0},{3} {1},{3} Z");
        }

        public LaunchpadGrid() {
            InitializeComponent();

            Root = this.Get<StackPanel>("Root");
            Grid = this.Get<UniformGrid>("LaunchpadGrid");

            TopLeft = this.Get<Path>("TopLeft");
            TopRight = this.Get<Path>("TopRight");
            BottomLeft = this.Get<Path>("BottomLeft");
            BottomRight = this.Get<Path>("BottomRight");

            ModeLight = this.Get<Rectangle>("ModeLight");

            Preferences.PhantomChanged += Update_Phantom;
        }

        private void LayoutChanged(object sender, EventArgs e) => DrawPath();

        public void RenderFrame(Frame frame) {
            for (int i = 0; i < 100; i++)
                SetColor(SignalToGrid(i), frame.Screen[i].ToScreenBrush());
        }

        bool mouseHeld = false;
        Shape mouseOver = null;

        private void MouseDown(object sender, PointerPressedEventArgs e) {
            if (e.MouseButton.HasFlag(MouseButton.Left)) {
                mouseHeld = true;

                e.Device.Capture(Root);
                Root.Cursor = new Cursor(StandardCursorType.Hand);

                PadStarted?.Invoke(Grid.Children.IndexOf((IControl)sender));
                MouseMove(sender, e);
            }
        }

        private void MouseUp(object sender, PointerReleasedEventArgs e) {
            if (e.MouseButton.HasFlag(MouseButton.Left)) {
                MouseMove(sender, e);
                PadFinished?.Invoke(Grid.Children.IndexOf((IControl)sender));

                mouseHeld = false;
                if (mouseOver != null) MouseLeave(mouseOver);
                mouseOver = null;

                e.Device.Capture(null);
                Root.Cursor = new Cursor(StandardCursorType.Arrow);
            }
        }

        private void MouseEnter(Shape control, InputModifiers mods) {
            int index = Grid.Children.IndexOf((IControl)control);
            PadPressed?.Invoke(index);
            PadModsPressed?.Invoke(index, mods);
        }

        private void MouseLeave(Shape control) => PadReleased?.Invoke(Grid.Children.IndexOf((IControl)control));

        private void MouseMove(object sender, PointerEventArgs e) {
            if (mouseHeld) {
                IInputElement _over = Root.InputHitTest(e.Device.GetPosition(Root));

                if (_over is Shape over) {
                    if (mouseOver == null) MouseEnter(over, e.InputModifiers);
                    else if (mouseOver != over) {
                        MouseLeave(mouseOver);
                        MouseEnter(over, e.InputModifiers);
                    }

                    mouseOver = over;

                } else if (mouseOver != null) {
                    MouseLeave(mouseOver);
                    mouseOver = null;
                }
            }
        }
    }
}
