using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using VoltAir_Setup.Views.Pages;

namespace VoltAir_Setup.Views
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

        public MainWindow()
        {
            InitializeComponent();
            this.Opened += OnWindowOpened;
        }

        private void OnPointerPressed(object sender, PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
                BeginMoveDrag(e);
        }

        private void CloseWindow(object sender, RoutedEventArgs e) => Close();
        private void MinimizeWindow(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        
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

        // Navigation
        public void NavigateToRequirements()
        {
            PageContent.Content = new RequirementsPage();
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