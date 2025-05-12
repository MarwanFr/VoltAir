using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Transformation;
using Avalonia.Threading;
using System;
using Avalonia.VisualTree;

namespace VoltAir.Views.Components
{
    public partial class LoadingWindow : Window
    {
        private double _adjustedThickness = 0.5;
        public double AdjustedThickness
        {
            get => _adjustedThickness;
            set
            {
                if (_adjustedThickness != value)
                {
                    _adjustedThickness = value;
                }
            }
        }

        public LoadingWindow()
        {
            InitializeComponent();
            this.Opened += OnWindowOpened;
            Opacity = 0;
            LoadingBorder.RenderTransform = TransformOperations.Parse("scale(0.9)");
        }

        protected override void OnOpened(EventArgs e)
        {
            base.OnOpened(e);

            // Animate the appearance of the window
            Dispatcher.UIThread.Post(() =>
            {
                Opacity = 1;
                LoadingBorder.RenderTransform = TransformOperations.Parse("scale(1)");
            }, DispatcherPriority.Render);
        }

        public new void Close()
        {
            // Animate the disappearance of the window
            LoadingBorder.RenderTransform = TransformOperations.Parse("scale(1)");

            var timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(200)
            };

            timer.Tick += (s, e) =>
            {
                LoadingBorder.RenderTransform = TransformOperations.Parse("scale(0.9)");
                Opacity = 0;

                var hideTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(200)
                };

                hideTimer.Tick += (s2, e2) =>
                {
                    hideTimer.Stop();
                    base.Close();
                };

                hideTimer.Start();
                timer.Stop();
            };

            timer.Start();
        }
        
        private void OnWindowOpened(object? sender, EventArgs e)
        {
            var scaling = this.GetVisualRoot()?.RenderScaling ?? 1.0;
            double adjustedThickness = scaling <= 1.0 ? 1.0 : 0.5;
            var dpi = 96 * scaling;
    
            var resources = this.Resources;
            if (resources != null)
            {
                resources["ThicknessResource"] = new Thickness(adjustedThickness);
            }
    
            // Console.WriteLine($"Scaling: {scaling}, Adjusted Thickness: {adjustedThickness}, Calculated DPI: {dpi}");
        }

    }
}