using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using VoltAir.Views.Pages;
using VoltAir.Views.Components;
using System.Threading.Tasks;
using System;
using System.Threading;
using Avalonia;
using Avalonia.VisualTree;

namespace VoltAir.Views
{
    public partial class MainWindow : Window
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
        
        private LoadingWindow _loadingWindow;
        private CancellationTokenSource _loadingCts;

        public MainWindow()
        {
            InitializeComponent();
            this.Opened += OnWindowOpened;
        }

        private void OnPointerPressed(object sender, PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                this.BeginMoveDrag(e);
            }
        }

        // Close the window
        private void CloseWindow(object? sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // Minimize the window
        private void MinimizeWindow(object? sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        // Toggle maximize and normal
        private void ToggleMaximizeWindow(object? sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
                MaximizeButton.Content = "\ue922"; // Maximize Unicode
            }
            else
            {
                this.WindowState = WindowState.Maximized;
                MaximizeButton.Content = "\ue923"; // Restore Unicode
            }
        }

        private async void NavButton_Click(object sender, RoutedEventArgs e)
        {
            _loadingCts = new CancellationTokenSource();
            var loadingTask = ShowLoadingWindowAfterDelay(_loadingCts.Token);

            // Reset all buttons
            HomeButton.IsChecked = false;
            FanButton.IsChecked = false;
            PerformancesButton.IsChecked = false;
            SettingsButton.IsChecked = false;
            SearchButton.IsChecked = false;

            var clickedButton = sender as ToggleButton;
            if (clickedButton != null)
            {
                clickedButton.IsChecked = true;
            }

            // Simulate loading time without blocking the UI
            int delay = clickedButton == PerformancesButton ? 3000 : 300; // Longer delay for Performances page
            await Task.Delay(delay); // Simulate the actual loading time

            // Create the page on the UI thread
            Control newPage = null;
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (clickedButton == HomeButton)
                {
                    newPage = new Home();
                }
                else if (clickedButton == FanButton)
                {
                    newPage = new Boost();
                }
                else if (clickedButton == PerformancesButton)
                {
                    newPage = new Performances();
                }
                else if (clickedButton == SearchButton)
                {
                    newPage = new Search();
                }
                else if (clickedButton == SettingsButton)
                {
                    newPage = new Settings();
                }

                // Display the new page
                ContentGrid.Children.Clear();
                if (newPage != null)
                {
                    ContentGrid.Children.Add(newPage);
                }

                _loadingCts.Cancel(); // Cancel the loading task if it's still running
                _loadingWindow?.Close();
            });
        }

        private async Task ShowLoadingWindowAfterDelay(CancellationToken token)
        {
            await Task.Delay(1000, token); // Wait for 1 second
            if (!token.IsCancellationRequested)
            {
                _loadingWindow = new LoadingWindow();
                _loadingWindow.Show(this);
            }
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