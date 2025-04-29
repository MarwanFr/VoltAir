using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;
using System.Management;
using System.Timers;
using Avalonia.Threading;
using System.Diagnostics;
using Microsoft.Win32;

namespace VoltAir.Views.Pages
{
    public partial class Home : UserControl, IDisposable
    {
        private Timer _updateTimer;
        private Timer _systemInfoTimer;
        private PerformanceCounter _cpuCounter;
        private bool _isInitialized = false;
        private bool _isDisposed = false;

        // Cache for system information that doesn't change frequently
        private string _windowsVersion;
        private string _windowsEdition;
        private string _architecture;
        private string _computerName;
        private DateTime _lastBootTime;

        public Home()
        {
            InitializeComponent();
            
            // Defer initialization until control is loaded
            this.AttachedToVisualTree += (s, e) => 
            {
                if (!_isInitialized)
                {
                    _isInitialized = true;
                    InitializePerformanceMonitor();
                    InitializeSystemInfo();
                }
            };
            
            this.DetachedFromVisualTree += (s, e) => 
            {
                Dispose();
            };
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;
                
            _isDisposed = true;
            
            // Stop timers
            if (_updateTimer != null)
            {
                _updateTimer.Stop();
                _updateTimer.Elapsed -= UpdatePerformance;
                _updateTimer.Dispose();
                _updateTimer = null;
            }
            
            if (_systemInfoTimer != null)
            {
                _systemInfoTimer.Stop();
                _systemInfoTimer.Elapsed -= UpdateSystemInfo;
                _systemInfoTimer.Dispose();
                _systemInfoTimer = null;
            }
            
            // Dispose performance counters
            if (_cpuCounter != null)
            {
                _cpuCounter.Dispose();
                _cpuCounter = null;
            }
        }

        private void InitializePerformanceMonitor()
        {
            try
            {
                _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                // Initial read to warm up
                _cpuCounter.NextValue();
                
                _updateTimer = new Timer(2000); // Update every 2 seconds instead of 1
                _updateTimer.Elapsed += UpdatePerformance;
                _updateTimer.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing performance monitor: {ex.Message}");
            }
        }

        private void InitializeSystemInfo()
        {
            try
            {
                // Load static system information once
                GetWindowsInfo();
                
                // Get boot time once
                _lastBootTime = GetLastBootTime();
                
                // Update the uptime display initially
                UpdateSystemInfo(null, null);
                
                // Setup timer for uptime updates - less frequent
                _systemInfoTimer = new Timer(10000); // Update every 10 seconds instead of 5
                _systemInfoTimer.Elapsed += UpdateSystemInfo;
                _systemInfoTimer.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing system info: {ex.Message}");
            }
        }

        private void UpdateSystemInfo(object sender, ElapsedEventArgs e)
        {
            try
            {
                if (_isDisposed)
                    return;
                    
                TimeSpan uptime = DateTime.Now - _lastBootTime;
                string uptimeStr = $"{uptime.Days}d {uptime.Hours}h {uptime.Minutes}m";

                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (_isDisposed)
                        return;
                        
                    UptimeText.Text = uptimeStr;
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating uptime: {ex.Message}");
            }
        }

        private DateTime GetLastBootTime()
        {
            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT LastBootUpTime FROM Win32_OperatingSystem"))
                using (ManagementObjectCollection collection = searcher.Get())
                {
                    foreach (ManagementObject obj in collection)
                    {
                        using (obj)
                        {
                            string lastBootUpTime = obj["LastBootUpTime"].ToString();
                            if (!string.IsNullOrEmpty(lastBootUpTime))
                            {
                                return ManagementDateTimeConverter.ToDateTime(lastBootUpTime);
                            }
                        }
                    }
                }
                return DateTime.Now;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving system boot time: {ex.Message}");
                return DateTime.Now;
            }
        }

        private void GetWindowsInfo()
        {
            try
            {
                _architecture = Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit";
                _computerName = Environment.MachineName;
                string activationStatus = "Unknown";
                
                // Get Windows version info
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT Version, Caption FROM Win32_OperatingSystem"))
                using (ManagementObjectCollection collection = searcher.Get())
                {
                    foreach (ManagementObject obj in collection)
                    {
                        using (obj)
                        {
                            _windowsVersion = obj["Version"]?.ToString() ?? "Unknown";
                            string caption = obj["Caption"]?.ToString() ?? "Unknown";
                            
                            if (!string.IsNullOrEmpty(caption) && caption.Contains("Windows"))
                            {
                                _windowsEdition = caption.Substring(caption.IndexOf("Windows"));
                            }
                            else
                            {
                                _windowsEdition = caption;
                            }
                        }
                    }
                }
                
                // Get marketing version
                string displayVersion = GetMarketingVersion();
                if (!string.IsNullOrEmpty(displayVersion))
                {
                    _windowsVersion += $" ({displayVersion})";
                }
                
                // Check activation status in a separate query
                try
                {
                    using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(@"root\CIMV2", "SELECT LicenseStatus FROM SoftwareLicensingProduct WHERE Name LIKE 'Windows%' AND LicenseStatus > 0"))
                    using (ManagementObjectCollection collection = searcher.Get())
                    {
                        foreach (ManagementObject obj in collection)
                        {
                            using (obj)
                            {
                                int licenseStatus = Convert.ToInt32(obj["LicenseStatus"]);
                                activationStatus = licenseStatus == 1 ? "Activated" : "Not Activated";
                                break;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error checking activation status: {ex.Message}");
                    activationStatus = "Error";
                }
                
                // Update UI with cached values
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (_isDisposed)
                        return;
                        
                    WindowsVersionText.Text = _windowsVersion;
                    WindowsEditionText.Text = _windowsEdition;
                    ActivationStatusText.Text = activationStatus;
                    ArchitectureText.Text = _architecture;
                    ComputerNameText.Text = _computerName;
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting system info: {ex.Message}");
                _windowsVersion = "Error";
                _windowsEdition = "Error";
                _architecture = "Error";
                _computerName = "Error";
                
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (_isDisposed)
                        return;
                        
                    WindowsVersionText.Text = "Error";
                    WindowsEditionText.Text = "Error";
                    ActivationStatusText.Text = "Error";
                    ArchitectureText.Text = "Error";
                    ComputerNameText.Text = "Error";
                });
            }
        }
        
        private string GetMarketingVersion()
        {
            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
                {
                    if (key != null)
                    {
                        object value = key.GetValue("DisplayVersion");
                        return value?.ToString() ?? string.Empty;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading DisplayVersion: {ex.Message}");
            }

            return string.Empty;
        }

        private void UpdatePerformance(object sender, ElapsedEventArgs e)
        {
            try
            {
                if (_isDisposed)
                    return;
                    
                double cpuUsage = GetCpuUsage();
                double ramUsage = GetRamUsage();
                double storageUsage = GetStorageUsage();

                double performanceScore = (cpuUsage + ramUsage + storageUsage) / 3;
                string performanceDescription = GetPerformanceDescription(performanceScore);

                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (_isDisposed)
                        return;
                        
                    PerformanceScoreText.Text = $"{performanceScore:F1}%";
                    PerformanceInfoText.Text = $"Performance Score: {performanceScore:F1}";
                    CpuUsageText.Text = $"CPU: {cpuUsage:F1}%";
                    RamUsageText.Text = $"RAM: {ramUsage:F1}%";
                    StorageUsageText.Text = $"Storage: {storageUsage:F1}%";
                    PerformanceDescriptionText.Text = performanceDescription;
                    CircleProgressBarPerformance.Value = performanceScore;
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating performance: {ex.Message}");
            }
        }

        private string GetPerformanceDescription(double score)
        {
            if (score < 20)
                return "Excellent";
            else if (score < 40)
                return "Good";
            else if (score < 60)
                return "Average";
            else if (score < 80)
                return "Bad";
            else
                return "Very Bad";
        }

        private double GetCpuUsage()
        {
            try
            {
                if (_cpuCounter == null)
                    return 0.0;
                    
                // Don't sleep here - use the timer's interval instead
                return _cpuCounter.NextValue();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving CPU usage: {ex.Message}");
                return 0.0;
            }
        }

        private double GetRamUsage()
        {
            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("select TotalVisibleMemorySize, FreePhysicalMemory from Win32_OperatingSystem"))
                using (ManagementObjectCollection collection = searcher.Get())
                {
                    foreach (ManagementObject obj in collection)
                    {
                        using (obj)
                        {
                            double totalVisibleMemory = Convert.ToDouble(obj["TotalVisibleMemorySize"]);
                            double freeMemory = Convert.ToDouble(obj["FreePhysicalMemory"]);
                            double usedMemory = totalVisibleMemory - freeMemory;
                            return (usedMemory / totalVisibleMemory) * 100.0;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving RAM usage: {ex.Message}");
            }
            return 0.0;
        }

        private double GetStorageUsage()
        {
            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("select Size, FreeSpace from Win32_LogicalDisk WHERE DriveType=3"))
                using (ManagementObjectCollection collection = searcher.Get())
                {
                    double totalSize = 0.0;
                    double freeSpace = 0.0;
                    
                    foreach (ManagementObject obj in collection)
                    {
                        using (obj) 
                        {
                            totalSize += Convert.ToDouble(obj["Size"]);
                            freeSpace += Convert.ToDouble(obj["FreeSpace"]);
                        }
                    }
                    
                    double usedSpace = totalSize - freeSpace;
                    return totalSize > 0 ? (usedSpace / totalSize) * 100.0 : 0.0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving storage usage: {ex.Message}");
            }
            return 0.0;
        }
    }
}