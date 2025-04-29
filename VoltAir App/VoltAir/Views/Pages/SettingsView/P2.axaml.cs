using System;
using System.Diagnostics;
using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using VoltAir.Views.Components;

namespace VoltAir.Views.Pages.SettingsView;

public partial class P2 : UserControl
{
    // Toast service for notifications
    private ToastService _toastService;

    public P2()
    {
        InitializeComponent();
        string voltAirPath = Path.Combine(Path.GetTempPath(), "VoltAir");
        CacheDirectoryPathTextBlock.Text = $"Cache Directory: {voltAirPath}";
        
        // Initialize the toast service
        _toastService = new ToastService(ToastContainer);
    }

    private async void OnClearCacheClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            string voltAirPath = Path.Combine(Path.GetTempPath(), "VoltAir");
            DirectoryInfo directory = new DirectoryInfo(voltAirPath);
            
            if (directory.Exists)
            {
                directory.Delete(true);

                // Show success toast instead of updating status text
                await _toastService.ShowSuccess("Cache cleared successfully", "Cache Clear");
                
                // Still update status text for additional feedback
            }
            else
            {
                // Show warning toast
                await _toastService.ShowWarning("VoltAir cache directory not found", "Directory Not Found");
            }
        }
        catch (Exception ex)
        {
            // Show error toast
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
                
                // Show info toast
                await _toastService.ShowInfo("Opening cache directory", "File Explorer");
            }
            else
            {
                // Show warning toast
                await _toastService.ShowWarning("VoltAir cache directory not found", "Directory Not Found");
            }
        }
        catch (Exception ex)
        {
            // Show error toast
            await _toastService.ShowError($"Error accessing cache directory: {ex.Message}", "Error");
        }
    }
}