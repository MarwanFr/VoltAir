using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Transformation;
using Avalonia.Threading;
using System;

namespace VoltAir.Views.Components
{
    public partial class LoadingWindow : Window
    {
        public LoadingWindow()
        {
            InitializeComponent();
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
    }
}