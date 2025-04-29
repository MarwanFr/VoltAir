using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Voltair_Uninstall.Views
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

        private void NoButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private async void YesButton_Click(object sender, RoutedEventArgs e)
        {
            StatusText.Text = "Uninstalling Voltair. Please wait...";
            YesButton.IsVisible = false;
            NoButton.IsVisible = false;
            UninstallProgress.IsVisible = true;
    
            await UninstallVoltair();
        }

        private async Task UninstallVoltair()
        {
            try
            {
                // Remove shortcuts
                await DeleteFileIfExists(@"C:\Users\Public\Desktop\Voltair.lnk");
                await DeleteFileIfExists(@"C:\ProgramData\Microsoft\Windows\Start Menu\Programs\Voltair.lnk");

                // Kill running processes
                await RunProcessAsync("taskkill", "/f /im \"Voltair.exe\"");
        
                // Stop and remove service if exists
                try
                {
                    await RunProcessAsync("net", "stop VoltairService");
                    await RunProcessAsync("sc", "delete VoltairService");
                }
                catch { /* Service might not exist */ }

                // Remove scheduled tasks
                await RunProcessAsync("schtasks", "/Delete /TN \"Voltair Tray Launch\" /F");

                // Remove registry entry
                await RunProcessAsync("reg", "delete \"HKLM\\SOFTWARE\\WOW6432Node\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\Voltair\" /f");

                // Remove Windows Defender exclusion
                await RunProcessAsync("powershell", "Remove-MpPreference -ExclusionPath 'C:\\Program Files (x86)\\Voltair'");
                await RunProcessAsync("powershell", "Remove-MpPreference -ExclusionPath 'C:\\Program Files\\Voltair'");

                // Remove installation directories
                await RunProcessAsync("cmd.exe", "/c timeout /t 1 & rmdir /q /s \"C:\\Program Files (x86)\\Voltair\"");
                await RunProcessAsync("cmd.exe", "/c timeout /t 1 & rmdir /q /s \"C:\\Program Files\\Voltair\"");

                StatusText.Text = "Uninstallation complete. This window will close automatically.";
                await Task.Delay(2000);
                Close();
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Error during uninstallation: {ex.Message}";
            }
        }

        private async Task DeleteFileIfExists(string path)
        {
            if (File.Exists(path))
            {
                try
                {
                    File.Delete(path);
                    await Task.Delay(100); // Small delay between operations
                }
                catch { /* Continue if deletion fails */ }
            }
        }

        private async Task RunProcessAsync(string fileName, string arguments)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    WindowStyle = ProcessWindowStyle.Hidden
                }
            };

            process.Start();
            await process.WaitForExitAsync();
        }
    }
}