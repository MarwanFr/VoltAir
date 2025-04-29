using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using Avalonia.Media;
using Avalonia.Media.Transformation;
using Avalonia.Threading;

namespace VoltAir.Views.Components
{
    public partial class ConfirmDialog : UserControl
    {
        public event EventHandler? Confirmed;
        public event EventHandler? Canceled;

        public ConfirmDialog()
        {
            InitializeComponent();
            IsVisible = false;
            Opacity = 0;
        }

        public void Show(string title, string description = "")
        {
            ConfirmText.Text = title;

            if (!string.IsNullOrEmpty(description))
            {
                DescriptionText.Text = description;
                DescriptionText.IsVisible = true;
            }
            else
            {
                DescriptionText.IsVisible = false;
            }

            IsVisible = true;
            Opacity = 0;
            PopupBorder.RenderTransform = TransformOperations.Parse("scale(0.9)");
            PopupBorder.InvalidateVisual();
            
            Dispatcher.UIThread.Post(() =>
            {
                Opacity = 1;
                PopupBorder.RenderTransform = TransformOperations.Parse("scale(1)");
            }, DispatcherPriority.Render);
        }

        private void OnConfirmClick(object? sender, RoutedEventArgs e)
        {
            HideWithAnimation();
            Confirmed?.Invoke(this, EventArgs.Empty);
        }

        private void OnCancelClick(object? sender, RoutedEventArgs e)
        {
            HideWithAnimation();
            Canceled?.Invoke(this, EventArgs.Empty);
        }

        private void HideWithAnimation()
        {
            PopupBorder.RenderTransform = TransformOperations.Parse("scale(1)");

            var timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(300)
            };

            timer.Tick += (s, e) =>
            {
                PopupBorder.RenderTransform = TransformOperations.Parse("scale(0.9)");
                Opacity = 0;

                var hideTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(300)
                };

                hideTimer.Tick += (s2, e2) =>
                {
                    IsVisible = false;
                    hideTimer.Stop();
                };

                hideTimer.Start();
                timer.Stop();
            };

            timer.Start();
        }
    }
}