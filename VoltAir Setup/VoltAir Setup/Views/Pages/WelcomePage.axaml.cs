using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using System.Diagnostics;

namespace VoltAir_Setup.Views.Pages
{
    public partial class WelcomePage : UserControl
    {
        public WelcomePage()
        {
            InitializeComponent();
        }

        private void OnNextClicked(object sender, RoutedEventArgs e)
        {
            var mainWindow = this.FindAncestorOfType<MainWindow>();
            if (mainWindow != null)
            {
                mainWindow.NavigateToRequirements();
            }
        }

        private void OnTermsChecked(object sender, RoutedEventArgs e)
        {
            NextButton.IsEnabled = true;
        }

        private void OnTermsUnchecked(object sender, RoutedEventArgs e)
        {
            NextButton.IsEnabled = false;
        }

        private void OnTermsOfServiceClicked(object sender, RoutedEventArgs e)
        {
            var url = "https://voltair.pages.dev/terms";
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch
            {
            }
        }


    }
}