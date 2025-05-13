using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Threading;
using Microsoft.Win32;

namespace VoltAir.Views.Components
{
    public partial class MicrosoftActivationScripts : Window
    {
        private Process _activationProcess;
        private readonly HttpClient _httpClient = new HttpClient();

        public MicrosoftActivationScripts()
        {
            InitializeComponent();
        }

        private async void OnActivateWindowsClick(object? sender, RoutedEventArgs e)
        {
            var logOutput = this.FindControl<TextBox>("LogOutput");
            logOutput.Text = "Checking Windows activation status...\n";

            if (IsWindowsActivated())
            {
                AddLog("Windows is already activated!");
                ShowToast("Windows is already activated", "Information", Colors.Blue);
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
                ShowToast("HWID Script ready", "Activation", Colors.Green);

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
                    ShowToast("Windows activated successfully", "Success", Colors.Green);
                }
                else
                {
                    AddLog($"Activation failed with code {_activationProcess.ExitCode}");
                    ShowToast("Activation failed", "Error", Colors.Red);
                }
            }
            catch (Exception ex)
            {
                AddLog($"CRITICAL ERROR: {ex.Message}");
                ShowToast($"Error: {ex.Message}", "Activation Failed", Colors.Red);
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

        private void ShowToast(string message, string title, Color color)
        {
            Dispatcher.UIThread.Post(async () =>
            {
                var toastContainer = this.FindControl<Panel>("ToastContainer");
                var toast = new Border
                {
                    Background = new SolidColorBrush(color),
                    CornerRadius = new CornerRadius(5),
                    Padding = new Thickness(10),
                    Margin = new Thickness(0, 0, 0, 5),
                    Child = new StackPanel
                    {
                        Children =
                        {
                            new TextBlock { Text = title, FontWeight = FontWeight.Bold },
                            new TextBlock { Text = message }
                        }
                    }
                };

                toastContainer.Children.Add(toast);
                await Task.Delay(5000);
                toastContainer.Children.Remove(toast);
            });
        }
    }
}