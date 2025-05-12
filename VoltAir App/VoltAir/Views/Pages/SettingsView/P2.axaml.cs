using System;
using System.Diagnostics;
using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using VoltAir.Views.Components;

namespace VoltAir.Views.Pages.SettingsView
{
    public partial class P2 : UserControl
    {
        private ToastService _toastService;

        public P2()
        {
            InitializeComponent();
            string voltAirPath = Path.Combine(Path.GetTempPath(), "VoltAir");
            CacheDirectoryPathTextBlock.Text = $"Cache Directory: {voltAirPath}";
            _toastService = new ToastService(ToastContainer);
        }

        private async void OnClearCacheClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                bool success = true;
                string voltAirPath = Path.Combine(Path.GetTempPath(), "VoltAir");
                string masPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VoltAir", "MAS");
                
                // Delete VoltAir temp directory
                if (Directory.Exists(voltAirPath))
                {
                    Directory.Delete(voltAirPath, true);
                    await _toastService.ShowSuccess("VoltAir cache cleared", "Success");
                }
                else
                {
                    success = false;
                    await _toastService.ShowWarning("VoltAir cache not found", "Notice");
                }

                // Delete MAS directory
                if (Directory.Exists(masPath))
                {
                    Directory.Delete(masPath, true);
                    await _toastService.ShowSuccess("MAS activation files cleared", "Success");
                }
                else
                {
                    if (!success) // Only show warning if neither directory was found
                    {
                        await _toastService.ShowWarning("No cache directories found", "Notice");
                    }
                }
            }
            catch (Exception ex)
            {
                await _toastService.ShowError($"Error clearing cache: {ex.Message}", "Error");
            }
        }

        private async void OnAccessCacheDirectoryClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                string voltAirPath = Path.Combine(Path.GetTempPath(), "VoltAir");
                
                if (Directory.Exists(voltAirPath))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = voltAirPath,
                        UseShellExecute = true
                    });
                    await _toastService.ShowInfo("Opening cache directory", "File Explorer");
                }
                else
                {
                    await _toastService.ShowWarning("Cache directory not found", "Notice");
                }
            }
            catch (Exception ex)
            {
                await _toastService.ShowError($"Error: {ex.Message}", "Error");
            }
        }
    }
}