using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using VoltAir.Views.Pages.SettingsView;
using Avalonia.Controls.Primitives;

namespace VoltAir.Views.Pages
{
    public partial class Settings : UserControl
    {
        public Settings()
        {
            InitializeComponent();
            
            // Load default content
            LoadDefaultContent();
        }

        private void LoadDefaultContent()
        {
            ContentGrid.Children.Clear();
            ContentGrid.Children.Add(new P1());
            HomeButton.IsChecked = true;
        }

        private void NavButton_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            // Uncheck all buttons first
            HomeButton.IsChecked = false;
            ThermoButton.IsChecked = false;
            CpuButton.IsChecked = false;
            SettingsButton.IsChecked = false;

            // Check the clicked button
            if (sender is ToggleButton clickedButton)
            {
                clickedButton.IsChecked = true;
                
                // Clear current content
                ContentGrid.Children.Clear();
                
                // Load appropriate content
                switch (clickedButton.Name)
                {
                    case "HomeButton":
                        ContentGrid.Children.Add(new P1());
                        break;
                    case "ThermoButton":
                        ContentGrid.Children.Add(new P2());
                        break;
                    case "CpuButton":
                        ContentGrid.Children.Add(new P3());
                        break;
                    case "SettingsButton":
                        ContentGrid.Children.Add(new P4());
                        break;
                }
            }
        }
    }
}