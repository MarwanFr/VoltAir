using Avalonia.Controls;
using System;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Threading;
using System.Threading.Tasks;
using LibreHardwareMonitor.Hardware;
using System.Net.NetworkInformation;
using Avalonia.Threading;

namespace VoltAir.Views.Pages
{
    public partial class Performances : UserControl, IDisposable
    {
        // Static performance counters
        private readonly PerformanceCounter _cpuCounter;
        private readonly PerformanceCounter _ramCounter;
        private readonly PerformanceCounter _diskReadCounter;
        private readonly PerformanceCounter _diskWriteCounter;

        // Hardware monitor
        private readonly Computer _computer;

        // Cancellation token for the update loop
        private CancellationTokenSource _updateLoopCts;

        // Caches for static hardware info
        private string _cpuNameCache;
        private string _gpuNameCache;
        private string _storageNameCache;
        private string _networkCardCache;
        private string _totalRamCache;
        private ulong _totalRamBytes;

        // Caches for dynamic values to reduce allocations
        private float _lastCpuUsage;
        private float? _lastCpuTemp;
        private float? _lastGpuTemp;
        private float? _lastGpuUsage;
        private (string usageText, string infoText) _lastRamUsage;
        private (string usageText, string infoText) _lastStorageUsage;
        private float _lastReadSpeed;
        private float _lastWriteSpeed;

        // State tracking
        private readonly object _updateLock = new object();
        private bool _isUpdating;
        private bool _isVisible = false;
        private readonly int _updateIntervalMs = 3000; // Reduced update frequency
        
        // String format caches to avoid repeated allocations
        private static readonly string[] SpeedFormatCache = {
            "{0:F2} B/s", "{0:F2} KB/s", "{0:F2} MB/s", "{0:F2} GB/s"
        };

        public Performances()
        {
            InitializeComponent();

            try
            {
                // Create performance counters once
                _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total", true);
                _ramCounter = new PerformanceCounter("Memory", "Available MBytes", true);
                _diskReadCounter = new PerformanceCounter("PhysicalDisk", "Disk Read Bytes/sec", "_Total", true);
                _diskWriteCounter = new PerformanceCounter("PhysicalDisk", "Disk Write Bytes/sec", "_Total", true);
                
                // First call to NextValue() initializes counters
                _cpuCounter.NextValue();
                _ramCounter.NextValue();
                _diskReadCounter.NextValue();
                _diskWriteCounter.NextValue();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error initializing performance counters: {ex.Message}");
            }

            // More selective hardware monitoring - enable only what's needed
            _computer = new Computer
            {
                IsCpuEnabled = true,
                IsGpuEnabled = true,
                IsMemoryEnabled = false,
                IsStorageEnabled = true,
                IsNetworkEnabled = false,
                IsControllerEnabled = false,
                IsBatteryEnabled = false,
                IsPsuEnabled = false,
                IsMotherboardEnabled = false
            };
            _computer.Open();

            InitUIPlaceholders();

            // Start/stop update loop based on visibility
            AttachedToVisualTree += (s, e) => 
            {
                _isVisible = true;
                StartUpdateLoop();
            };
            
            DetachedFromVisualTree += (s, e) => 
            {
                _isVisible = false;
                StopUpdateLoop();
            };
        }

        private void StartUpdateLoop()
        {
            if (!_isVisible) return;
            
            _updateLoopCts?.Cancel();
            _updateLoopCts = new CancellationTokenSource();
            Task.Run(async () => await UpdateLoopAsync(_updateLoopCts.Token));
        }

        private void StopUpdateLoop()
        {
            _updateLoopCts?.Cancel();
        }

        private async Task UpdateLoopAsync(CancellationToken ct)
        {
            try
            {
                // Only load static info once when control is first shown
                await LoadStaticHardwareInfoAsync();

                // Initial sample of disk counters
                _diskReadCounter?.NextValue();
                _diskWriteCounter?.NextValue();

                // Main update loop
                while (!ct.IsCancellationRequested && _isVisible)
                {
                    if (_isUpdating)
                    {
                        await Task.Delay(100, ct);
                        continue;
                    }

                    try
                    {
                        lock (_updateLock)
                        {
                            _isUpdating = true;
                        }

                        // Get data first without any UI updates
                        await GatherPerformanceDataAsync();
                        
                        // Then update UI in a single dispatcher call
                        await UpdateUIAsync();
                        
                        await Task.Delay(_updateIntervalMs, ct);
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Update error: {ex.Message}");
                        await Task.Delay(5000, ct);
                    }
                    finally
                    {
                        lock (_updateLock)
                        {
                            _isUpdating = false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fatal update error: {ex.Message}");
            }
        }

        private async Task GatherPerformanceDataAsync()
        {
            try
            {
                // Read CPU usage first
                _lastCpuUsage = _cpuCounter?.NextValue() ?? 0;
                
                // Update hardware monitor - only update sensors we need
                await Task.Run(() => {
                    foreach (var hardware in _computer.Hardware)
                    {
                        // Only update hardware we're actually using
                        if (hardware.HardwareType == HardwareType.Cpu ||
                            hardware.HardwareType == HardwareType.GpuNvidia ||
                            hardware.HardwareType == HardwareType.GpuAmd ||
                            hardware.HardwareType == HardwareType.GpuIntel)
                        {
                            hardware.Update();
                        }
                    }
                });

                // Get all other values
                _lastCpuTemp = await GetCpuTemperatureAsync();
                _lastGpuTemp = await GetGPUTemperatureAsync();
                _lastGpuUsage = await GetGPUUsageAsync();
                _lastRamUsage = await GetRAMUsageAsync();
                _lastStorageUsage = await GetStorageUsageAsync();
                _lastReadSpeed = _diskReadCounter?.NextValue() ?? 0;
                _lastWriteSpeed = _diskWriteCounter?.NextValue() ?? 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error gathering performance data: {ex.Message}");
            }
        }

        private async Task UpdateUIAsync()
        {
            // Update UI in a single dispatcher call to reduce context switching
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                // CPU
                int cpuUsage = (int)_lastCpuUsage;
                CpuUsageText.Text = $"{cpuUsage}%";
                CpuTempText.Text = _lastCpuTemp.HasValue ? $"{_lastCpuTemp:F1}°C" : "N/A";
                CpuUsageBar.Value = cpuUsage;
                CpuTempBar.Value = _lastCpuTemp.HasValue ? Math.Min(_lastCpuTemp.Value, 100) : 0;

                // GPU
                GpuUsageText.Text = _lastGpuUsage.HasValue ? $"{_lastGpuUsage:F1}%" : "N/A";
                GpuTempText.Text = _lastGpuTemp.HasValue ? $"{_lastGpuTemp:F1}°C" : "N/A";
                if (_lastGpuUsage.HasValue) GpuUsageBar.Value = _lastGpuUsage.Value;
                if (_lastGpuTemp.HasValue) GpuTempBar.Value = Math.Min(_lastGpuTemp.Value, 100);

                // RAM
                if (double.TryParse(_lastRamUsage.usageText.Replace("%", ""), out double ramUsagePercentage))
                {
                    CircleProgressBarRam.Value = ramUsagePercentage;
                }
                RamUsageText.Text = _lastRamUsage.usageText;
                RamInfoText.Text = _lastRamUsage.infoText;

                // Storage
                if (double.TryParse(_lastStorageUsage.usageText.Replace("%", ""), out double storageUsagePercentage))
                {
                    CircleProgressBarStorage.Value = storageUsagePercentage;
                }
                StorageUsageText.Text = _lastStorageUsage.usageText;
                StorageInfoText.Text = _lastStorageUsage.infoText;

                // Disk Speed
                DiskReadSpeedText.Text = $"Read: {FormatSpeed(_lastReadSpeed)}";
                DiskWriteSpeedText.Text = $"Write: {FormatSpeed(_lastWriteSpeed)}";
            });
        }

        private async Task LoadStaticHardwareInfoAsync()
        {
            // Load static information only once and in sequence
            try
            {
                _cpuNameCache = await GetCPUNameAsync();
                (_totalRamCache, _totalRamBytes) = await GetTotalRAMAsync();
                _gpuNameCache = await GetGPUNameAsync();
                _storageNameCache = await GetStorageNameAsync();
                _networkCardCache = await GetNetworkCardNameAsync();
                
                // Update UI with static info in one batch
                await Dispatcher.UIThread.InvokeAsync(() => 
                {
                    CpuNameText.Text = $"CPU: {_cpuNameCache}";
                    GpuNameText.Text = _gpuNameCache;
                    StorageNameText.Text = $"Disk: {_storageNameCache}";
                    RamTotalText.Text = $"Total RAM: {_totalRamCache} GB";
                    NetworkCardText.Text = _networkCardCache;
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading static hardware info: {ex.Message}");
            }
        }

        private void InitUIPlaceholders()
        {
            // Set consistent placeholders
            CpuNameText.Text = "CPU: ...";
            GpuNameText.Text = "...";
            NetworkCardText.Text = "...";
            StorageNameText.Text = "Disk: ...";
            RamTotalText.Text = "Total RAM: ...";
            RamUsageText.Text = "...";
            CpuUsageText.Text = "0%";
            CpuTempText.Text = "N/A";
            GpuUsageText.Text = "0%";
            GpuTempText.Text = "N/A";
        }

        // Hardware information methods - optimized
        private Task<string> GetCPUNameAsync()
        {
            // If we already have the CPU name cached, return it immediately
            if (!string.IsNullOrEmpty(_cpuNameCache))
                return Task.FromResult(_cpuNameCache);
                
            return Task.Run(() =>
            {
                try
                {
                    using var searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_Processor");
                    using var collection = searcher.Get();
                    string fullName = collection.Cast<ManagementObject>().FirstOrDefault()?["Name"]?.ToString() ?? "Unknown";
                    return CleanCPUName(fullName);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error getting CPU name: {ex.Message}");
                    return "Unknown CPU";
                }
            });
        }

        private string CleanCPUName(string cpuName)
        {
            // Static list of words to remove for better performance
            string[] keywordsToRemove = { "CPU", "Processor", "(R)", "(TM)", "with", " @", "Hz" };

            foreach (var keyword in keywordsToRemove)
            {
                cpuName = cpuName.Replace(keyword, "", StringComparison.OrdinalIgnoreCase);
            }

            cpuName = cpuName.Trim();

            if (cpuName.Any(char.IsDigit))
            {
                cpuName += "Hz";
            }

            return cpuName;
        }

        private Task<float?> GetCpuTemperatureAsync()
        {
            return Task.Run(() =>
            {
                try
                {
                    foreach (var hardware in _computer.Hardware)
                    {
                        if (hardware.HardwareType == HardwareType.Cpu)
                        {
                            foreach (var sensor in hardware.Sensors)
                            {
                                if (sensor.SensorType == SensorType.Temperature &&
                                    sensor.Name.Contains("CPU Package", StringComparison.OrdinalIgnoreCase))
                                {
                                    return sensor.Value;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error getting CPU temperature: {ex.Message}");
                }
                return null;
            });
        }

        private Task<string> GetGPUNameAsync()
        {
            // Return cached value if available
            if (!string.IsNullOrEmpty(_gpuNameCache))
                return Task.FromResult(_gpuNameCache);
                
            return Task.Run(() =>
            {
                try
                {
                    string gpuName = null;
                
                    using var searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_VideoController");
                    using var collection = searcher.Get();
                    foreach (var obj in collection)
                    {
                        string name = obj["Name"]?.ToString();
                        if (!string.IsNullOrEmpty(name) && !IsVirtualGpu(name))
                        {
                            gpuName = name;
                            break;
                        }
                    }
                    
                    return gpuName ?? "N/A";
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error getting GPU name: {ex.Message}");
                    return "N/A";
                }
            });
        }

        // Pre-compiled keywords for virtual GPU detection
        private static readonly string[] VirtualGpuKeywords = {
            "Parsec", "Virtual", "Basic", "Microsoft", "VMware", "VirtualBox", 
            "Hyper-V", "QXL", "Citrix", "RDP"
        };
        
        private bool IsVirtualGpu(string gpuName)
        {
            foreach (var keyword in VirtualGpuKeywords)
            {
                if (gpuName.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private Task<float?> GetGPUTemperatureAsync()
        {
            return Task.Run(() =>
            {
                try
                {
                    foreach (var hardware in _computer.Hardware)
                    {
                        if (hardware.HardwareType == HardwareType.GpuNvidia ||
                            hardware.HardwareType == HardwareType.GpuAmd ||
                            hardware.HardwareType == HardwareType.GpuIntel)
                        {
                            foreach (var sensor in hardware.Sensors)
                            {
                                if (sensor.SensorType == SensorType.Temperature &&
                                    sensor.Name.Contains("GPU Core", StringComparison.OrdinalIgnoreCase))
                                {
                                    return sensor.Value;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error getting GPU temperature: {ex.Message}");
                }
                return null;
            });
        }

        private Task<float?> GetGPUUsageAsync()
        {
            return Task.Run(() =>
            {
                try
                {
                    foreach (var hardware in _computer.Hardware)
                    {
                        if (hardware.HardwareType == HardwareType.GpuNvidia ||
                            hardware.HardwareType == HardwareType.GpuAmd ||
                            hardware.HardwareType == HardwareType.GpuIntel)
                        {
                            foreach (var sensor in hardware.Sensors)
                            {
                                if (sensor.SensorType == SensorType.Load &&
                                    sensor.Name.Contains("GPU Core", StringComparison.OrdinalIgnoreCase))
                                {
                                    return sensor.Value;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error getting GPU usage: {ex.Message}");
                }
                return null;
            });
        }

        private Task<(string totalRam, ulong totalRamBytes)> GetTotalRAMAsync()
        {
            // Return cached values if available
            if (!string.IsNullOrEmpty(_totalRamCache) && _totalRamBytes > 0)
                return Task.FromResult((_totalRamCache, _totalRamBytes));
                
            return Task.Run(() =>
            {
                try
                {
                    using var searcher = new ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem");
                    using var collection = searcher.Get();
                    ulong totalRam = collection.Cast<ManagementObject>().FirstOrDefault()?["TotalPhysicalMemory"] as ulong? ?? 0;
                    double totalRamInGB = totalRam / (1024.0 * 1024.0 * 1024.0);

                    ulong roundedTotalRam = (ulong)(totalRamInGB >= Math.Floor(totalRamInGB) + 0.5
                        ? Math.Ceiling(totalRamInGB)
                        : Math.Floor(totalRamInGB));

                    return (roundedTotalRam.ToString(), totalRam);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error getting total RAM: {ex.Message}");
                    return ("N/A", 0UL);
                }
            });
        }

        private Task<(string usageText, string infoText)> GetRAMUsageAsync()
        {
            return Task.Run(() =>
            {
                try
                {
                    // Use cached total RAM value
                    ulong freeRam = (ulong)(_ramCounter?.NextValue() ?? 0) * 1024 * 1024;
                    ulong totalRam = _totalRamBytes;

                    if (totalRam == 0) return ("N/A", "N/A");

                    ulong usedRam = totalRam - freeRam;
                    double usedRamGB = usedRam / (1024.0 * 1024.0 * 1024.0);
                    double totalRamGB = totalRam / (1024.0 * 1024.0 * 1024.0);
                    double percentUsed = (1 - (double)freeRam / totalRam) * 100;

                    // Round the values to avoid floating point precision issues
                    ulong roundedUsedRam = (ulong)Math.Round(usedRamGB);
                    ulong roundedTotalRam = (ulong)Math.Round(totalRamGB);

                    string usageText = $"{percentUsed:F1}%";
                    string infoText = $"{roundedUsedRam}GB/{roundedTotalRam}GB";

                    return (usageText, infoText);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error getting RAM usage: {ex.Message}");
                    return ("N/A", "N/A");
                }
            });
        }

        private Task<string> GetStorageNameAsync()
        {
            // Return cached value if available
            if (!string.IsNullOrEmpty(_storageNameCache))
                return Task.FromResult(_storageNameCache);
                
            return Task.Run(() =>
            {
                try
                {
                    // Direct query for the system drive using one query instead of multiple
                    using var diskDriveSearcher = new ManagementObjectSearcher(
                        "SELECT Model FROM Win32_DiskDrive WHERE DeviceID = " +
                        "(SELECT Antecedent FROM Win32_DiskDriveToDiskPartition WHERE Dependent = " +
                        "(SELECT Antecedent FROM Win32_LogicalDiskToPartition WHERE Dependent = " +
                        "'Win32_LogicalDisk.DeviceID=\"C:\"'))");
                        
                    using var diskDrives = diskDriveSearcher.Get();
                    var diskDrive = diskDrives.Cast<ManagementObject>().FirstOrDefault();
                    string model = diskDrive?["Model"]?.ToString();
                    
                    return model ?? "Unknown";
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error getting storage name: {ex.Message}");
                    return "Unknown";
                }
            });
        }

        private Task<(string usageText, string infoText)> GetStorageUsageAsync()
        {
            return Task.Run(() =>
            {
                try
                {
                    using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Volume WHERE DriveLetter = 'C:'");
                    using var collection = searcher.Get();
                    var volume = collection.Cast<ManagementObject>().FirstOrDefault();
                    if (volume != null)
                    {
                        ulong totalSize = (ulong)volume["Capacity"];
                        ulong freeSpace = (ulong)volume["FreeSpace"];
                        ulong usedSpace = totalSize - freeSpace;

                        double usedSpaceGB = usedSpace / (1024.0 * 1024.0 * 1024.0);
                        double totalSizeGB = totalSize / (1024.0 * 1024.0 * 1024.0);
                        double percentUsed = (1 - (double)freeSpace / totalSize) * 100;

                        string usageText = $"{percentUsed:F1}%";
                        string infoText = $"{usedSpaceGB:F1} GB / {totalSizeGB:F1} GB";

                        return (usageText, infoText);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error getting storage usage: {ex.Message}");
                }
                return ("N/A", "N/A");
            });
        }

        private Task<string> GetNetworkCardNameAsync()
        {
            // Return cached value if available
            if (!string.IsNullOrEmpty(_networkCardCache))
                return Task.FromResult(_networkCardCache);
                
            return Task.Run(() =>
            {
                try
                {
                    var interfaces = NetworkInterface.GetAllNetworkInterfaces();
                    foreach (var adapter in interfaces)
                    {
                        if (adapter.OperationalStatus == OperationalStatus.Up)
                        {
                            if (adapter.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
                            {
                                return $"{adapter.Description} (Wi-Fi)";
                            }
                            else if (adapter.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                            {
                                return $"{adapter.Description} (Ethernet)";
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error getting network card info: {ex.Message}");
                }
                return "N/A";
            });
        }

        // Optimized speed formatting with pre-compiled format strings
        private string FormatSpeed(float speedInBytes)
        {
            const float KB = 1024;
            const float MB = KB * 1024;
            const float GB = MB * 1024;
            
            int formatIndex;
            float scaledValue;
            
            if (speedInBytes >= GB)
            {
                formatIndex = 3;
                scaledValue = speedInBytes / GB;
            }
            else if (speedInBytes >= MB)
            {
                formatIndex = 2;
                scaledValue = speedInBytes / MB;
            }
            else if (speedInBytes >= KB)
            {
                formatIndex = 1;
                scaledValue = speedInBytes / KB;
            }
            else
            {
                formatIndex = 0;
                scaledValue = speedInBytes;
            }
            
            return string.Format(SpeedFormatCache[formatIndex], scaledValue);
        }

        public void Dispose()
        {
            StopUpdateLoop();
            _updateLoopCts?.Dispose();
            _updateLoopCts = null;
            
            _cpuCounter?.Dispose();
            _ramCounter?.Dispose();
            _diskReadCounter?.Dispose();
            _diskWriteCounter?.Dispose();
            
            _computer?.Close();
        }
    }
}