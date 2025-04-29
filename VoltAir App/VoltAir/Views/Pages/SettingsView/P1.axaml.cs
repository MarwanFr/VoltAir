using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using System;
using System.Threading.Tasks;
using VoltAir.Models;
using Avalonia.Interactivity;

namespace VoltAir.Views.Pages.SettingsView
{
    public partial class P1 : UserControl
    {
        private readonly Easing _easing = new CubicEaseInOut();
        private readonly TimeSpan _duration = TimeSpan.FromMilliseconds(1000);
        private readonly Easing _fadeOutEasing = new ExponentialEaseOut();
        private readonly TimeSpan _fadeOutDuration = TimeSpan.FromMilliseconds(1500);

        private AppSettings _settings;

        public P1()
        {
            InitializeComponent();
            LoadSettingsAsync();
            Application.Current.ActualThemeVariantChanged += OnActualThemeVariantChanged;

            ConfirmDialog.Confirmed += async (s, e) =>
            {
                _settings = new AppSettings();
                await _settings.SaveAsync();
                await AnimateThemeChange(_settings.GetThemeVariant());
                ThemeSelector.SelectedIndex = 0;
                SetSystemThemeLabel();
            };

            ConfirmDialog.Canceled += (s, e) =>
            {
                // Nothing to do
            };
        }

        private async void LoadSettingsAsync()
        {
            _settings = await AppSettings.LoadAsync();
            ApplySavedTheme();
            SetSystemThemeLabel();
        }

        private void ApplySavedTheme()
        {
            if (Application.Current != null)
            {
                Application.Current.RequestedThemeVariant = _settings.GetThemeVariant();
            }
        }

        private void SetSystemThemeLabel()
        {
            SystemThemeItem.Content = "System";

            var requestedTheme = Application.Current.RequestedThemeVariant;
            var actualTheme = Application.Current.ActualThemeVariant;

            ThemeSelector.SelectedIndex = requestedTheme.Key switch
            {
                "Dark" => 2,
                "Light" => 1,
                _ => 0
            };

            if (requestedTheme == ThemeVariant.Default)
            {
                var mainWindow = (Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
                if (mainWindow != null)
                {
                    mainWindow.Foreground = actualTheme == ThemeVariant.Dark ?
                        new SolidColorBrush(Colors.White) :
                        new SolidColorBrush(Colors.Black);
                }
            }
        }

        private void OnActualThemeVariantChanged(object? sender, EventArgs e)
        {
            SetSystemThemeLabel();
        }

        private async void OnThemeSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
                sender is not ComboBox comboBox)
            {
                return;
            }

            var selection = comboBox.SelectedIndex;
            var newTheme = selection switch
            {
                1 => ThemeVariant.Light,
                2 => ThemeVariant.Dark,
                _ => ThemeVariant.Default
            };

            if (Application.Current.RequestedThemeVariant == newTheme)
                return;

            _settings.Theme = newTheme.Key switch
            {
                "Light" => "Light",
                "Dark" => "Dark",
                _ => "System"
            };

            await _settings.SaveAsync();
            await AnimateThemeChange(newTheme);
            SetSystemThemeLabel();
        }

        private void OnResetButtonClick(object? sender, RoutedEventArgs e)
        {
            ConfirmDialog.Show("Reset Settings", "Are you sure you want to reset all settings?");
        }

        private async Task AnimateThemeChange(ThemeVariant newTheme)
        {
            var mainWindow = (Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
            if (mainWindow == null) return;

            bool isSystemDark = Application.Current.ActualThemeVariant == ThemeVariant.Dark;
            bool isDarkTheme = newTheme == ThemeVariant.Dark || 
                               (newTheme == ThemeVariant.Default && isSystemDark);

            var overlay = new Border
            {
                Background = new SolidColorBrush(Colors.Transparent),
                Opacity = 0,
                IsHitTestVisible = false,
                ZIndex = 9999
            };

            try
            {
                var visualBrush = new VisualBrush(mainWindow.Content as Visual)
                {
                    TileMode = TileMode.None,
                    Stretch = Stretch.UniformToFill
                };
                overlay.Background = visualBrush;

                if (mainWindow.Content is Panel mainPanel)
                {
                    mainPanel.Children.Add(overlay);
                }

                var backgroundColorAnimation = new Animation
                {
                    FillMode = FillMode.Forward,
                    Easing = _easing,
                    Duration = _duration
                };

                var foregroundColorAnimation = new Animation
                {
                    FillMode = FillMode.Forward,
                    Easing = _easing,
                    Duration = _duration
                };

                var darkBackgroundColor = new SolidColorBrush(Color.FromRgb(32, 32, 32));
                var lightBackgroundColor = new SolidColorBrush(Colors.White);
                var darkForegroundColor = new SolidColorBrush(Colors.White);
                var lightForegroundColor = new SolidColorBrush(Colors.Black);

                if (isDarkTheme)
                {
                    backgroundColorAnimation.Children.Add(new KeyFrame { Cue = new Cue(0d), Setters = { new Setter(BackgroundProperty, mainWindow.Background) } });
                    backgroundColorAnimation.Children.Add(new KeyFrame { Cue = new Cue(1d), Setters = { new Setter(BackgroundProperty, darkBackgroundColor) } });

                    foregroundColorAnimation.Children.Add(new KeyFrame { Cue = new Cue(0d), Setters = { new Setter(ForegroundProperty, mainWindow.Foreground) } });
                    foregroundColorAnimation.Children.Add(new KeyFrame { Cue = new Cue(1d), Setters = { new Setter(ForegroundProperty, darkForegroundColor) } });
                }
                else
                {
                    backgroundColorAnimation.Children.Add(new KeyFrame { Cue = new Cue(0d), Setters = { new Setter(BackgroundProperty, mainWindow.Background) } });
                    backgroundColorAnimation.Children.Add(new KeyFrame { Cue = new Cue(1d), Setters = { new Setter(BackgroundProperty, lightBackgroundColor) } });

                    foregroundColorAnimation.Children.Add(new KeyFrame { Cue = new Cue(0d), Setters = { new Setter(ForegroundProperty, mainWindow.Foreground) } });
                    foregroundColorAnimation.Children.Add(new KeyFrame { Cue = new Cue(1d), Setters = { new Setter(ForegroundProperty, lightForegroundColor) } });
                }

                await Task.WhenAll(
                    backgroundColorAnimation.RunAsync(mainWindow),
                    foregroundColorAnimation.RunAsync(mainWindow)
                );

                Application.Current.RequestedThemeVariant = newTheme;

                visualBrush.Visual = mainWindow.Content as Visual;
                overlay.Opacity = 1;

                await Task.Delay(200);

                var fadeOutAnimation = new Animation
                {
                    FillMode = FillMode.Forward,
                    Easing = _fadeOutEasing,
                    Duration = _fadeOutDuration,
                    Children =
                    {
                        new KeyFrame { Cue = new Cue(0.0), Setters = { new Setter(OpacityProperty, 1.0) } },
                        new KeyFrame { Cue = new Cue(0.5), Setters = { new Setter(OpacityProperty, 0.5) } },
                        new KeyFrame { Cue = new Cue(1.0), Setters = { new Setter(OpacityProperty, 0.0) } }
                    }
                };

                await fadeOutAnimation.RunAsync(overlay);
            }
            finally
            {
                if (mainWindow.Content is Panel panel)
                {
                    panel.Children.Remove(overlay);
                }
            }
        }
    }
}