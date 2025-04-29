using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using VoltAir_Setup.Views.Pages;

namespace VoltAir_Setup.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
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
    }
}