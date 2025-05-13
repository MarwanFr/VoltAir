using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Microsoft.Win32;

namespace VoltAir.Views.Components
{
    public partial class MicrosoftActivationScripts : Window
{
    private ToastService _toastService;
    private Process _activationProcess;
    private readonly HttpClient _httpClient = new HttpClient();

    public MicrosoftActivationScripts()
    {
        InitializeComponent();
        _toastService = new ToastService(this.FindControl<Panel>("ToastContainer"));
        this.Opened += OnWindowOpened;
    }

    private void OnPointerPressed(object sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            this.BeginMoveDrag(e);
        }
    }

    private void CloseWindow(object? sender, RoutedEventArgs e) => this.Close();

    private void MinimizeWindow(object? sender, RoutedEventArgs e) => this.WindowState = WindowState.Minimized;

    private void ToggleMaximizeWindow(object? sender, RoutedEventArgs e)
    {
        if (this.WindowState == WindowState.Maximized)
        {
            this.WindowState = WindowState.Normal;
            MaximizeButton.Content = "\ue922";
        }
        else
        {
            this.WindowState = WindowState.Maximized;
            MaximizeButton.Content = "\ue923";
        }
    }

    private async void OnActivateWindowsClick(object? sender, RoutedEventArgs e)
    {
        var logOutput = this.FindControl<TextBox>("LogOutput");
        logOutput.Text = "Checking Windows activation status...\n";

        if (IsWindowsActivated())
        {
            AddLog("Windows is already activated!");
            await _toastService.ShowInfo("Windows is already activated", "Information");
            return;
        }

        AddLog("Windows needs activation, proceeding...");

        string basePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "VoltAir",
            "MAS");

        string filePath = Path.Combine(basePath, "HWID_Activation.cmd");
        string url = "https://raw.githubusercontent.com/massgravel/Microsoft-Activation-Scripts/master/MAS/Separate-Files-Version/Activators/HWID_Activation.cmd";

        try
        {
            AddLog("Downloading HWID activation script...");

            if (!Directory.Exists(basePath))
                Directory.CreateDirectory(basePath);

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var scriptContent = await response.Content.ReadAsStringAsync();
            await File.WriteAllTextAsync(filePath, scriptContent);

            AddLog("Script downloaded successfully!");
            await _toastService.ShowSuccess("HWID Script ready", "Activation");

            AddLog("Starting activation process (admin required)...");

            _activationProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/C \"{filePath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    Verb = "runas"
                }
            };

            _activationProcess.OutputDataReceived += (s, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data))
                    AddLog(args.Data);
            };

            _activationProcess.ErrorDataReceived += (s, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data))
                    AddLog($"ERROR: {args.Data}");
            };

            _activationProcess.Start();
            _activationProcess.BeginOutputReadLine();
            _activationProcess.BeginErrorReadLine();

            await _activationProcess.WaitForExitAsync();

            if (_activationProcess.ExitCode == 0)
            {
                AddLog("HWID Activation completed successfully!");
                await _toastService.ShowSuccess("Windows activated successfully", "Success");
            }
            else
            {
                AddLog($"Activation failed with code {_activationProcess.ExitCode}");
                await _toastService.ShowError("Activation failed", "Error");
            }
        }
        catch (Exception ex)
        {
            AddLog($"CRITICAL ERROR: {ex.Message}");
            await _toastService.ShowError($"Error: {ex.Message}", "Activation Failed");
        }
    }

    private bool IsWindowsActivated()
    {
        try
        {
            using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\SoftwareProtectionPlatform"))
            {
                var value = key?.GetValue("NotificationRequired");
                return (value == null || (int)value == 0);
            }
        }
        catch
        {
            return false;
        }
    }

    private void AddLog(string message)
    {
        Dispatcher.UIThread.Post(() =>
        {
            var logOutput = this.FindControl<TextBox>("LogOutput");
            logOutput.Text += $"[{DateTime.Now:HH:mm:ss}] {message}\n";
            logOutput.CaretIndex = logOutput.Text.Length;

            if (logOutput.Parent is ScrollViewer scrollViewer)
            {
                scrollViewer.ScrollToEnd();
            }
        });
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