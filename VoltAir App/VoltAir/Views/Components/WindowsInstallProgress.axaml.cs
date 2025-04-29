using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Net.Http;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace VoltAir.Views.Components
{
    public partial class WindowsInstallProgress : Window
    {
        public event EventHandler<InstallationResultEventArgs>? InstallationCompleted;
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private bool _isInstalling = false;

        public WindowsInstallProgress()
        {
            InitializeComponent();
            LogText.Text = "Preparing installation...\n";
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

        // To trigger the completion event with a result
        public class InstallationResultEventArgs : EventArgs
        {
            public bool Success { get; set; }
            public string Message { get; set; } = string.Empty;
        }

        public async Task StartInstallationAsync()
        {
            _cancellationTokenSource = new CancellationTokenSource(); // Reset the token source
            _isInstalling = true;
            CancelButton.Content = "Cancel";

            try
            {
                // Check for cancellation before starting
                if (_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    AddLogMessage("Installation cancelled by the user.");
                    StatusMessage.Text = "Installation cancelled";
                    InstallationCompleted?.Invoke(this, new InstallationResultEventArgs
                    {
                        Success = false,
                        Message = "Installation cancelled by the user."
                    });
                    return;
                }

                await DownloadAndInstallWindows11IoT();
            }
            catch (OperationCanceledException)
            {
                AddLogMessage("Installation cancelled by the user.");
                StatusMessage.Text = "Installation cancelled";
                InstallationCompleted?.Invoke(this, new InstallationResultEventArgs
                {
                    Success = false,
                    Message = "Installation cancelled by the user."
                });
                Close(); // Close the window automatically
            }
            catch (Exception ex)
            {
                AddLogMessage($"ERROR: {ex.Message}");
                StatusMessage.Text = "Installation failed";
                InstallationCompleted?.Invoke(this, new InstallationResultEventArgs
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            finally
            {
                _isInstalling = false;
                CancelButton.Content = "Close";
            }
        }

        private void AddLogMessage(string message)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                LogText.Text += $"[{DateTime.Now:HH:mm:ss}] {message}\n";
                
                LogScroller.ScrollToEnd();
            });
        }

        private async Task DownloadAndInstallWindows11IoT()
        {
            string tempPath = Path.Combine(Path.GetTempPath(), "VoltAir");
            string isoFilePath = Path.Combine(tempPath, "WinInstall.iso");

            // Create temporary directory if it doesn't exist
            if (!Directory.Exists(tempPath))
            {
                Directory.CreateDirectory(tempPath);
                AddLogMessage($"Temporary directory created: {tempPath}");
            }

            // URL for downloading Windows 11 IoT ISO
            string downloadUrl =
                "https://oemsoc.download.prss.microsoft.com/dbazure/X23-81951_26100.1742.240906-0331.ge_release_svc_refresh_CLIENT_ENTERPRISES_OEM_x64FRE_en-us.iso_640de540-87c4-427f-be87-e6d53a3a60b4?t=2c3b664b-b119-4088-9db1-ccff72c6d22e&P1=102816950270&P2=601&P3=2&P4=OC448onxqdmdUsBUApAiE8pj1FZ%2bEPTU3%2bC6Quq29MVwMyyDUtR%2fsbiy7RdVoZOHaZRndvzeOOnIwJZ2x3%2bmP6YK9cjJSP41Lvs0SulF4SVyL5C0DdDmiWqh2QW%2bcDPj2Xp%2bMrI9NOeElSBS5kkOWP8Eiyf2VkkQFM3g5vIk3HJVvu5sWo6pFKpFv4lML%2bHaIiTSuwbPMs5xwEQTfScuTKfigNlUZPdHRMp1B3uKLgIA3r0IbRpZgHYMXEwXQ%2fSLMdDNQthpqQvz1PThVkx7ObD55CXgt0GNSAWRfjdURWb8ywWk1gT7ozAgpP%2fKNm56U5nh33WZSuMZIuO1SBM2vw%3d%3d";

            // Get file size for progress tracking
            long fileSize = await GetFileSizeAsync(downloadUrl);
            if (fileSize <= 0)
            {
                AddLogMessage(
                    "Unable to determine file size. Download will continue without accurate progress display.");
                fileSize = 5_159_232_256; // Approximate size (5 GB)
            }
            else
            {
                AddLogMessage($"ISO file size: {FormatFileSize(fileSize)}");
            }

            // Set the current step
            await Dispatcher.UIThread.InvokeAsync(() => { CurrentStep.Text = "Downloading ISO image..."; });

            // Download the ISO file
            await DownloadFileWithProgressAsync(downloadUrl, isoFilePath, fileSize);

            // Mount the ISO
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                CurrentStep.Text = "Mounting ISO image...";
                ProgressBar.IsIndeterminate = true;
            });

            AddLogMessage("Download complete. Mounting ISO image...");
            string driveLetter = await MountIsoAsync(isoFilePath);

            if (string.IsNullOrEmpty(driveLetter))
            {
                throw new Exception("Unable to mount the ISO image or retrieve the drive letter.");
            }

            AddLogMessage($"ISO image mounted on drive {driveLetter}:");

            // Start the setup
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                CurrentStep.Text = "Starting Windows 11 IoT installation...";
            });

            string setupPath = Path.Combine($"{driveLetter}:", "setup.exe");
            if (!File.Exists(setupPath))
            {
                throw new Exception("setup.exe file not found in the mounted ISO image.");
            }

            AddLogMessage($"Installation file found: {setupPath}");
            AddLogMessage("Starting Windows 11 IoT installation...");

            // Start the installation
            Process.Start(new ProcessStartInfo
            {
                FileName = setupPath,
                Verb = "runas",
                UseShellExecute = true
            });

            // Finish the process
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                CurrentStep.Text = "Installation successfully started!";
                ProgressBar.IsIndeterminate = false;
                ProgressBar.Value = 100;
                StatusMessage.Text = "Installation successfully started!";
                StatusMessage.Foreground = Avalonia.Media.Brushes.Green;
            });

            AddLogMessage("Windows 11 IoT installation successfully started!");

            InstallationCompleted?.Invoke(this, new InstallationResultEventArgs
            {
                Success = true,
                Message = "Installation successfully started!"
            });
        }

        private async Task<long> GetFileSizeAsync(string url)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "HEAD";
            try
            {
                using (WebResponse response = await request.GetResponseAsync())
                {
                    return response.ContentLength;
                }
            }
            catch (Exception ex)
            {
                AddLogMessage($"Unable to determine file size: {ex.Message}");
                return -1;
            }
        }

        private async Task DownloadFileWithProgressAsync(string url, string destinationFile, long fileSize)
        {
            using (HttpClient client = new HttpClient())
            {
                DateTime startTime = DateTime.Now;
                long lastBytesReceived = 0;
                DateTime lastSpeedUpdate = DateTime.Now;
                double averageSpeed = 0;
                int speedSamples = 0;

                using (var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead,
                           _cancellationTokenSource.Token))
                {
                    await using (var contentStream = await response.Content.ReadAsStreamAsync())
                    using (var fileStream = new FileStream(destinationFile, FileMode.Create, FileAccess.Write,
                               FileShare.None, 81920, true))
                    {
                        byte[] buffer = new byte[81920];
                        int bytesRead;
                        long totalBytesRead = 0;

                        while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length,
                                   _cancellationTokenSource.Token)) > 0)
                        {
                            // Check if cancellation is requested
                            if (_cancellationTokenSource.Token.IsCancellationRequested)
                            {
                                AddLogMessage("Download cancelled by the user.");
                                break;
                            }

                            await fileStream.WriteAsync(buffer, 0, bytesRead, _cancellationTokenSource.Token);
                            totalBytesRead += bytesRead;

                            double percentage = (double)totalBytesRead / fileSize * 100;

                            // Calculate download speed
                            TimeSpan elapsed = DateTime.Now - lastSpeedUpdate;
                            if (elapsed.TotalSeconds >= 0.5) // Update speed every 0.5 seconds
                            {
                                long bytesDownloaded = totalBytesRead - lastBytesReceived;
                                double bytesPerSecond = bytesDownloaded / elapsed.TotalSeconds;
                                double mbps = bytesPerSecond / (1024 * 1024);

                                // Calculate average speed
                                averageSpeed = (averageSpeed * speedSamples + mbps) / (speedSamples + 1);
                                speedSamples++;

                                // Calculate estimated remaining time
                                double remainingBytes = fileSize - totalBytesRead;
                                TimeSpan remainingTime =
                                    TimeSpan.FromSeconds(remainingBytes / (bytesPerSecond > 0 ? bytesPerSecond : 1));

                                // Update UI
                                Dispatcher.UIThread.InvokeAsync(() =>
                                {
                                    ProgressBar.Value = percentage;
                                    ProgressText.Text = $"{Math.Round(percentage)}%";
                                    DownloadSpeed.Text = $"{mbps:F2} MB/s";

                                    if (remainingTime.TotalHours >= 1)
                                    {
                                        EstimatedTime.Text =
                                            $"{(int)remainingTime.TotalHours}h {remainingTime.Minutes}m";
                                    }
                                    else if (remainingTime.TotalMinutes >= 1)
                                    {
                                        EstimatedTime.Text = $"{remainingTime.Minutes}m {remainingTime.Seconds}s";
                                    }
                                    else
                                    {
                                        EstimatedTime.Text = $"{remainingTime.Seconds}s";
                                    }
                                });

                                lastBytesReceived = totalBytesRead;
                                lastSpeedUpdate = DateTime.Now;

                                // Add occasional log messages
                                if (Math.Round(percentage) % 10 == 0 && Math.Round(percentage) > 0)
                                {
                                    AddLogMessage(
                                        $"Download: {Math.Round(percentage)}% - {FormatFileSize(totalBytesRead)}/{FormatFileSize(fileSize)}");
                                }
                            }
                        }
                    }
                }
            }
        }

        private async Task<string> MountIsoAsync(string isoFilePath)
        {
            string driveLetter = string.Empty;

            try
            {
                Process mountProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments =
                            $"Mount-DiskImage -ImagePath \"{isoFilePath}\" -PassThru | Get-Volume | ForEach-Object {{ $_.DriveLetter }}",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        Verb = "runas"
                    }
                };

                mountProcess.Start();
                driveLetter = await mountProcess.StandardOutput.ReadToEndAsync();
                await mountProcess.WaitForExitAsync();

                return driveLetter.Trim();
            }
            catch (Exception ex)
            {
                AddLogMessage($"Error mounting ISO: {ex.Message}");
                throw;
            }
        }

        private string FormatFileSize(long size)
        {
            if (size >= 1L << 30)
            {
                return $"{size / (1L << 30)} GB";
            }
            else if (size >= 1L << 20)
            {
                return $"{size / (1L << 20)} MB";
            }
            else
            {
                return $"{size / (1L << 10)} KB";
            }
        }

        private async void OnCancelButtonClick(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (_isInstalling)
            {
                // Cancel the installation process
                _cancellationTokenSource.Cancel();
                StatusMessage.Text = "Cancelling installation...";
                AddLogMessage("Cancellation requested by the user.");

                // Disable the cancel button to prevent multiple clicks
                CancelButton.IsEnabled = false;

                // Wait a short moment to allow current operations to cancel
                await Task.Delay(1000);

                // Delete the VoltAir folder from %temp%
                await DeleteTempFolderWithRetries();
            }
            else
            {
                // Close the window if no installation is in progress
                Close();
            }
        }

        private async Task DeleteTempFolderWithRetries()
        {
            string tempPath = Path.Combine(Path.GetTempPath(), "VoltAir");
            int maxRetries = 3;
            int delayBetweenRetries = 1000; // 1 second

            if (Directory.Exists(tempPath))
            {
                for (int i = 0; i < maxRetries; i++)
                {
                    try
                    {
                        await Task.Run(() => DeleteDirectory(tempPath));
                        AddLogMessage($"Successfully deleted the folder: {tempPath}");
                        break; // If successful, exit the loop
                    }
                    catch (Exception ex)
                    {
                        AddLogMessage($"Attempt {i + 1}/{maxRetries}: Error deleting folder - {ex.Message}");

                        if (i < maxRetries - 1)
                        {
                            await Task.Delay(delayBetweenRetries);
                        }
                    }
                }
            }

            // Close the window after cleanup
            Close();
        }

        private void DeleteDirectory(string directoryPath)
        {
            // Set directory attributes to normal in case they're read-only
            foreach (var dir in Directory.GetDirectories(directoryPath, "*", SearchOption.AllDirectories))
            {
                try
                {
                    File.SetAttributes(dir, FileAttributes.Normal);
                }
                catch
                {
                    /* Ignore if we can't change attributes */
                }
            }

            foreach (var file in Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories))
            {
                try
                {
                    File.SetAttributes(file, FileAttributes.Normal);
                    File.Delete(file);
                }
                catch
                {
                    /* Ignore if we can't delete */
                }
            }

            // Now try to delete the directories
            foreach (var dir in Directory.GetDirectories(directoryPath))
            {
                try
                {
                    Directory.Delete(dir, true);
                }
                catch
                {
                    /* Ignore if we can't delete */
                }
            }

            // Final attempt to delete the main directory
            try
            {
                Directory.Delete(directoryPath, true);
            }
            catch
            {
                /* Last attempt failed */
            }
        }
        
    }
}
