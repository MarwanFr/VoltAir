using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.IO;
using System.Diagnostics;
using Avalonia.Threading;
using VoltAir.Views.Components;
using System.Management;

namespace VoltAir.Views.Pages
{
    public partial class Boost : UserControl
    {
        private readonly string regPath = Path.Combine(Directory.GetCurrentDirectory(), "Resources", "Regedit");
        private ToastService _toastService;

        public Boost()
        {
            InitializeComponent();
            TelemetryToggle.IsChecked = IsTelemetryEnabled();
            WindowsUpdateToggle.IsChecked = IsServiceRunning("wuauserv") || IsServiceRunning("WaaSMedicSvc") || IsServiceRunning("DoSvc");
            
            // Initialize toast notifications
            _toastService = new ToastService(this.FindControl<Panel>("ToastContainer"));

            if (_toastService == null)
            {
                Debug.WriteLine("ToastContainer not found!");
            }
        }

        private void OnOpenCleanFoldersWindowClick(object sender, RoutedEventArgs e)
        {
            var cleanFoldersWindow = new BoostWindows.CleanFolders();
            cleanFoldersWindow.Show();
        }

        private async void OnUninstallEdgeButtonClick(object sender, RoutedEventArgs e)
        {
            if (_toastService == null)
            {
                Debug.WriteLine("ToastService is null!");
                return;
            }

            string status = "in progress";
            try
            {
                string exePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "RemoveEdge.exe");
                if (!File.Exists(exePath)) return;

                Process.Start(new ProcessStartInfo
                {
                    FileName = exePath,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                });
                status = "done";
                await _toastService.ShowSuccess("Microsoft Edge removed successfully", "Edge Removal");
            }
            catch (Exception ex)
            {
                status = "error";
                await _toastService.ShowError($"Error removing Microsoft Edge: {ex.Message}", "Edge Removal Error");
            }
            Debug.WriteLine($"status: {status}");
        }

        private async void OnTelemetryToggled(object sender, RoutedEventArgs e)
        {
            if (_toastService == null)
            {
                Debug.WriteLine("ToastService is null!");
                return;
            }

            string status = "in progress";
            try
            {
                if (TelemetryToggle.IsChecked == true)
                {
                    RunRegFile("EnableTelemetry.reg");
                    await _toastService.ShowInfo("Telemetry enabled", "Telemetry");
                }
                else
                {
                    RunRegFile("DisableTelemetry.reg");
                    await _toastService.ShowInfo("Telemetry disabled", "Telemetry");
                }

                status = "done";
            }
            catch (Exception ex)
            {
                status = "error";
                await _toastService.ShowError($"Error toggling telemetry: {ex.Message}", "Telemetry Error");
            }
            Debug.WriteLine($"status: {status}");
        }
        
        private async void OnPowerSaverToggled(object sender, RoutedEventArgs e)
        {
            if (_toastService == null) return;

            try
            {
                if (PowerSaverToggle.IsChecked == true)
                {
                    RunPowerCfgCommand("/setactive a1841308-3541-4fab-bc81-f71556f20b4a"); // Power Saver plan
                    await _toastService.ShowInfo("Power Saver mode enabled", "Power Mode");
                }
                else
                {
                    RunPowerCfgCommand("/setactive 381b4222-f694-41f0-9685-ff5bb260df2e"); // Balanced plan
                    await _toastService.ShowInfo("Balanced mode enabled", "Power Mode");
                }
            }
            catch (Exception ex)
            {
                await _toastService.ShowError($"Error switching power mode: {ex.Message}", "Power Mode Error");
            }
        }

        private void RunPowerCfgCommand(string arguments)
        {
            var process = new ProcessStartInfo
            {
                FileName = "powercfg.exe",
                Arguments = arguments,
                Verb = "runas", // requires admin rights
                UseShellExecute = true,
                CreateNoWindow = true
            };

            Process.Start(process);
        }
        
        private async void OnWindowsUpdateToggled(object sender, RoutedEventArgs e)
        {
            if (_toastService == null)
            {
                Debug.WriteLine("ToastService is null!");
                return;
            }

            string status = "in progress";
            try
            {
                if (WindowsUpdateToggle.IsChecked == true)
                {
                    StartService("wuauserv");
                    StartService("WaaSMedicSvc");
                    StartService("DoSvc");
                    await _toastService.ShowInfo("Windows Update enabled", "Windows Update");
                }
                else
                {
                    StopService("wuauserv");
                    StopService("WaaSMedicSvc");
                    StopService("DoSvc");
                    await _toastService.ShowInfo("Windows Update disabled", "Windows Update");
                }

                status = "done";
            }
            catch (Exception ex)
            {
                status = "error";
                await _toastService.ShowError($"Error toggling Windows Update: {ex.Message}", "Windows Update Error");
            }
            Debug.WriteLine($"status: {status}");
        }


        private void StartService(string serviceName)
        {
            var process = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"Start-Service -Name {serviceName} -Force",
                Verb = "runas",
                UseShellExecute = true,
                CreateNoWindow = true
            };

            Process.Start(process);
        }

        private void StopService(string serviceName)
        {
            var process = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"Stop-Service -Name {serviceName} -Force",
                Verb = "runas",
                UseShellExecute = true,
                CreateNoWindow = true
            };

            Process.Start(process);
        }

        private bool IsServiceRunning(string serviceName)
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher($"SELECT State FROM Win32_Service WHERE Name = '{serviceName}'"))
                {
                    foreach (var obj in searcher.Get())
                    {
                        var stateProperty = obj.Properties["State"];
                        if (stateProperty != null)
                        {
                            return stateProperty.Value.ToString() == "Running";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error checking service status: {ex.Message}");
            }
            return false;
        }

        
        private void RunRegFile(string fileName)
        {
            string filePath = Path.Combine(regPath, fileName);

            if (File.Exists(filePath))
            {
                var process = new ProcessStartInfo
                {
                    FileName = "regedit.exe",
                    Arguments = $"/s \"{filePath}\"",
                    Verb = "runas",
                    UseShellExecute = true
                };

                try
                {
                    Process.Start(process);
                }
                catch (System.ComponentModel.Win32Exception) { }
            }
        }

        private bool IsTelemetryEnabled()
        {
            try
            {
                string regKeyPath = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\DataCollection";
                string valueName = "AllowTelemetry";

                object regValue = Microsoft.Win32.Registry.GetValue(regKeyPath, valueName, null);
                return regValue != null && (int)regValue == 3;
            }
            catch
            {
                return false;
            }
        }

        private void OnAskInstallWindows11IOTClick(object? sender, RoutedEventArgs e)
        {
            if (DefenderConfirmPopup == null)
            {
                Debug.WriteLine("DefenderConfirmPopup is null!");
                return;
            }

            DefenderConfirmPopup.Show(
                "Are you sure you want to install Windows 11 IoT?",
                "This will start the Windows 11 IoT installation. Make sure you have backed up your data and followed the necessary preparation steps.\n\nSee our official website for more information."
            );
            DefenderConfirmPopup.Confirmed += OnConfirmInstallWindows11IOT;
            DefenderConfirmPopup.Canceled += OnCancelInstallWindows11IOT;
        }

        private void OnConfirmInstallWindows11IOT(object? sender, EventArgs e)
        {
            DefenderConfirmPopup.Confirmed -= OnConfirmInstallWindows11IOT;
            OnInstallWindows11IOTClick(null!, null!); // Start installation
        }

        private void OnCancelInstallWindows11IOT(object? sender, EventArgs e)
        {
            DefenderConfirmPopup.Canceled -= OnCancelInstallWindows11IOT;
            _ = _toastService?.ShowInfo("Windows 11 IoT installation canceled", "Installation Canceled");
        }

        private async void OnInstallWindows11IOTClick(object sender, RoutedEventArgs e)
        {
            if (_toastService == null)
            {
                Debug.WriteLine("ToastService is null!");
                return;
            }

            try
            {
                IsEnabled = false;
                var installWindow = new WindowsInstallProgress();

                installWindow.InstallationCompleted += (s, args) =>
                {
                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        IsEnabled = true;

                        if (args.Success)
                        {
                            _ = _toastService.ShowSuccess("Windows 11 IoT installation started successfully", "Installation Success");
                        }
                        else
                        {
                            _ = _toastService.ShowError($"Installation error: {args.Message}", "Installation Error");
                        }
                    });
                };

                installWindow.Closed += (s, args) =>
                {
                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        IsEnabled = true;
                    });
                };

                installWindow.Show();
                await installWindow.StartInstallationAsync();
            }
            catch (Exception ex)
            {
                IsEnabled = true;
                await _toastService.ShowError($"Error starting installation: {ex.Message}", "Installation Error");
            }
        }

        private async void OnUninstallDefenderButtonClick(object sender, RoutedEventArgs e)
        {
            if (_toastService == null)
            {
                Debug.WriteLine("ToastService is null!");
                return;
            }

            string status = "in progress";
            try
            {
                string exePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "DefenderRemover.exe");
                if (!File.Exists(exePath)) return;

                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = exePath,
                        UseShellExecute = false,
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden,
                        Verb = "runas"
                    }
                };

                process.Start();
                await process.StandardInput.WriteLineAsync("Y");
                process.StandardInput.Close();

                string output = await process.StandardOutput.ReadToEndAsync();
                string errors = await process.StandardError.ReadToEndAsync();

                await process.WaitForExitAsync();

                status = "done";
                await _toastService.ShowSuccess("Windows Defender removed successfully", "Defender Removal");
            }
            catch (Exception ex)
            {
                status = "error";
                await _toastService.ShowError($"Error removing Windows Defender: {ex.Message}", "Defender Removal Error");
            }
            Debug.WriteLine($"status: {status}");
        }

        private void OnAskRemoveDefenderClick(object? sender, RoutedEventArgs e)
        {
            DefenderConfirmPopup.Show(
                "Are you sure you want to disable Windows Defender?",
                "Disabling Windows Defender may help performance on low-end systems, but it lowers your system's security.\n\nSee our documentation for more information."
            );
            DefenderConfirmPopup.Confirmed += OnConfirmRemoveDefender;
            DefenderConfirmPopup.Canceled += OnCancelRemoveDefender;
        }

        private void OnConfirmRemoveDefender(object? sender, EventArgs e)
        {
            DefenderConfirmPopup.Confirmed -= OnConfirmRemoveDefender;
            OnUninstallDefenderButtonClick(null!, null!);
        }

        private void OnCancelRemoveDefender(object? sender, EventArgs e)
        {
            DefenderConfirmPopup.Canceled -= OnCancelRemoveDefender;
            _ = _toastService?.ShowInfo("Windows Defender removal canceled", "Removal Canceled");
        }

        private void OnAskRemoveEdgeClick(object? sender, RoutedEventArgs e)
        {
            DefenderConfirmPopup.Show(
                "Are you sure you want to remove Microsoft Edge?",
                "Removing Microsoft Edge may reduce resource usage, but some Windows features might rely on it.\n\nCheck our documentation for more information."
            );
            DefenderConfirmPopup.Confirmed += OnConfirmRemoveEdge;
            DefenderConfirmPopup.Canceled += OnCancelRemoveEdge;
        }

        private void OnConfirmRemoveEdge(object? sender, EventArgs e)
        {
            DefenderConfirmPopup.Confirmed -= OnConfirmRemoveEdge;
            OnUninstallEdgeButtonClick(null!, null!);
        }

        private void OnCancelRemoveEdge(object? sender, EventArgs e)
        {
            DefenderConfirmPopup.Canceled -= OnCancelRemoveEdge;
            _ = _toastService?.ShowInfo("Microsoft Edge removal canceled", "Removal Canceled");
        }
    }
}