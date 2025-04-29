using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.Controls;
using Avalonia.Styling;

namespace VoltAir.Views.Components
{
    public enum ToastType
    {
        Success,
        Error,
        Info,
        Warning
    }

    public partial class ToastNotification : UserControl
    {
        private DispatcherTimer _autoCloseTimer;
        private TaskCompletionSource<bool> _closedTcs;

        public ToastNotification()
        {
            InitializeComponent();
            _autoCloseTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5)
            };
            _autoCloseTimer.Tick += AutoCloseTimer_Tick;
        }

        private void AutoCloseTimer_Tick(object sender, EventArgs e)
        {
            _autoCloseTimer.Stop();
            HideToast();
        }

        public async Task ShowToast(string message, string title = null, ToastType type = ToastType.Error, bool autoClose = true, int autoCloseDelay = 5000)
        {
            // Configure toast appearance based on type
            ConfigureToastType(type);
            
            // Set content
            ToastMessage.Text = message;
            ToastTitle.Text = title ?? GetDefaultTitle(type);
            
            _closedTcs = new TaskCompletionSource<bool>();
            
            // Show toast with animation
            var animation = new Animation
            {
                Duration = TimeSpan.FromMilliseconds(300),
                FillMode = FillMode.Forward,
                Easing = new CubicEaseOut()
            };

            animation.Children.Add(new KeyFrame
            {
                Cue = new Cue(0d),
                Setters = { new Setter { Property = OpacityProperty, Value = 0d } }
            });

            animation.Children.Add(new KeyFrame
            {
                Cue = new Cue(1d),
                Setters = { new Setter { Property = OpacityProperty, Value = 1d } }
            });

            await animation.RunAsync(this);

            if (autoClose)
            {
                _autoCloseTimer.Interval = TimeSpan.FromMilliseconds(autoCloseDelay);
                _autoCloseTimer.Start();
            }

            await _closedTcs.Task;
        }

        private string GetDefaultTitle(ToastType type)
        {
            return type switch
            {
                ToastType.Success => "Succès",
                ToastType.Error => "Erreur",
                ToastType.Info => "Information",
                ToastType.Warning => "Avertissement",
                _ => "Notification"
            };
        }

        private void ConfigureToastType(ToastType type)
        {
            // Reset all icons visibility
            IconSuccess.IsVisible = false;
            IconError.IsVisible = false;
            IconInfo.IsVisible = false;
            IconWarning.IsVisible = false;
            
            // Reset all classes
            ToastBorder.Classes.Clear();
            
            // Set appropriate style and icon
            switch (type)
            {
                case ToastType.Success:
                    ToastBorder.Classes.Add("success");
                    IconSuccess.IsVisible = true;
                    break;
                case ToastType.Error:
                    ToastBorder.Classes.Add("error");
                    IconError.IsVisible = true;
                    break;
                case ToastType.Info:
                    ToastBorder.Classes.Add("info");
                    IconInfo.IsVisible = true;
                    break;
                case ToastType.Warning:
                    ToastBorder.Classes.Add("warning");
                    IconWarning.IsVisible = true;
                    break;
            }
        }

        private async void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            await HideToast();
        }

        public async Task HideToast()
        {
            _autoCloseTimer.Stop();
            
            var animation = new Animation
            {
                Duration = TimeSpan.FromMilliseconds(200),
                FillMode = FillMode.Forward,
                Easing = new CubicEaseIn()
            };

            animation.Children.Add(new KeyFrame
            {
                Cue = new Cue(0d),
                Setters = { new Setter { Property = OpacityProperty, Value = 1d } }
            });

            animation.Children.Add(new KeyFrame
            {
                Cue = new Cue(1d),
                Setters = { new Setter { Property = OpacityProperty, Value = 0d } }
            });

            await animation.RunAsync(this);
            
            _closedTcs?.TrySetResult(true);
        }
    }
}