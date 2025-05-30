﻿﻿using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace VoltAir_Setup.Views.Pages
{
    public partial class RequirementsPage : UserControl, IDisposable
    {
        private const string SegoeFluentIconsUrl = "https://aka.ms/SegoeFluentIcons";
        private const string SegoeFluentIconsZipPath = "SegoeFluentIcons.zip";
        private const string SegoeFluentIconsExtractPath = "SegoeFluentIcons";
        private const string SegoeFluentIconsTtfName = "Segoe Fluent Icons.ttf";

        private readonly HttpClient _httpClient;
        private bool _fontInstalled;
        private bool _isDisposed;

        public RequirementsPage()
        {
            InitializeComponent();

            _httpClient = new HttpClient();
            LogText.Text = "Initialization...\n";

            // Run checks in parallel
            Task.Run(async () =>
            {
                await CheckRequirements();
            });
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            // Ensure all elements are found after loading XAML
            InstallFontButton = this.FindControl<Button>("InstallFontButton");
            ProgressBar = this.FindControl<ProgressBar>("ProgressBar");
            ProgressText = this.FindControl<TextBlock>("ProgressText");
            LogText = this.FindControl<TextBlock>("LogText");
            LogScroller = this.FindControl<ScrollViewer>("LogScroller");
            StatusMessage = this.FindControl<TextBlock>("StatusMessage");
            BackButton = this.FindControl<Button>("BackButton");
            NextButton = this.FindControl<Button>("NextButton");
            CurrentStep = this.FindControl<TextBlock>("CurrentStep");
        }

        private void OnBackClicked(object? sender, RoutedEventArgs e)
        {
            if (this.Parent is ContentControl contentControl)
            {
                contentControl.Content = new WelcomePage();
            }
        }

        private void OnNextClicked(object? sender, RoutedEventArgs e)
        {
            // Redirect to the next page if all prerequisites are installed
            if (_fontInstalled)
            {
                if (this.Parent is ContentControl contentControl)
                {
                    contentControl.Content = new InstallationPage();
                }
                AddLogMessage("Navigating to the next page...");
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

        private void UpdateProgress(int value)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                ProgressBar.Value = value;
                ProgressText.Text = $"{value}%";
            });
        }

        private void UpdateStatus(string message, bool isError = false)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                StatusMessage.Text = message;
                StatusMessage.Foreground = isError
                    ? new SolidColorBrush(Colors.Red)
                    : new SolidColorBrush(Colors.White);
            });
        }

        private async Task CheckRequirements()
        {
            AddLogMessage("Checking prerequisites...");

            var fontTask = Task.Run(() => IsFontInstalled("Segoe Fluent Icons"));

            try
            {
                // Check font
                _fontInstalled = await fontTask;
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (_fontInstalled)
                    {
                        AddLogMessage("Segoe Fluent Icons is installed ✓");
                        UpdateProgress(100);
                    }
                    else
                    {
                        AddLogMessage("Segoe Fluent Icons is not installed ✗");
                        InstallFontButton.IsVisible = true;
                    }
                });

                // Check if everything is installed
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    CheckAllRequirements();
                });
            }
            catch (Exception ex)
            {
                AddLogMessage($"Error checking prerequisites: {ex.Message}");
                UpdateStatus($"Error: {ex.Message}", true);
            }
        }

        private void CheckAllRequirements()
        {
            if (_fontInstalled)
            {
                CurrentStep.Text = "All prerequisites are met";
                UpdateStatus("Setup is ready. You can continue.");
                NextButton.IsVisible = true;
                BackButton.IsVisible = false;
            }
            else
            {
                CurrentStep.Text = "Missing prerequisites";
                UpdateStatus("Please install the missing components.");
            }
        }

        private bool IsFontInstalled(string fontName)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                try
                {
                    using (var fontsKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Fonts"))
                    {
                        if (fontsKey == null) return false;

                        foreach (var valueName in fontsKey.GetValueNames())
                        {
                            if (valueName.Contains(fontName))
                            {
                                return true;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    AddLogMessage($"Error checking font: {ex.Message}");
                    return false;
                }
            }
            else
            {
                AddLogMessage("Font check not available on this operating system");
            }
            return false;
        }

        public async void InstallFontButton_Click(object sender, RoutedEventArgs e)
        {
            InstallFontButton.IsEnabled = false;
            BackButton.IsEnabled = false;
            UpdateStatus("Installing font...");
            AddLogMessage("Downloading Segoe Fluent Icons...");
            CurrentStep.Text = "Installing Segoe Fluent Icons";

            try
            {
                await DownloadFontZip();
                string ttfPath = ExtractFontZip();
                await InstallFont(ttfPath);

                await Task.Delay(2000);
                _fontInstalled = await Task.Run(() => IsFontInstalled("Segoe Fluent Icons"));

                if (_fontInstalled)
                {
                    AddLogMessage("Segoe Fluent Icons installed successfully ✓");
                    UpdateProgress(100);
                    InstallFontButton.IsVisible = false;
                    CheckAllRequirements();
                }
                else
                {
                    AddLogMessage("Segoe Fluent Icons installation failed ✗");
                    UpdateStatus("Failed to install font. Please try again.", true);
                    InstallFontButton.IsVisible = true;
                }
            }
            catch (Exception ex)
            {
                AddLogMessage($"Error: {ex.Message}");
                UpdateStatus($"Error: {ex.Message}", true);
            }
            finally
            {
                InstallFontButton.IsEnabled = true;
                BackButton.IsEnabled = true;
                CleanupTempFiles();
            }
        }

        private async Task DownloadFontZip()
        {
            UpdateProgress(0);

            using (var response = await _httpClient.GetAsync(SegoeFluentIconsUrl))
            {
                response.EnsureSuccessStatusCode();
                long totalBytes = response.Content.Headers.ContentLength ?? -1;

                using (var contentStream = await response.Content.ReadAsStreamAsync())
                using (var fileStream = new FileStream(SegoeFluentIconsZipPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    var buffer = new byte[8192];
                    long totalBytesRead = 0;
                    int bytesRead;

                    while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, bytesRead);

                        totalBytesRead += bytesRead;
                        if (totalBytes > 0)
                        {
                            int percentage = (int)((double)totalBytesRead / totalBytes * 100);
                            await Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                ProgressBar.Value = percentage;
                                ProgressText.Text = $"{percentage}%";
                            });
                        }
                    }
                }
            }
        }

        private string ExtractFontZip()
        {
            if (!File.Exists(SegoeFluentIconsZipPath))
                throw new FileNotFoundException("Archive not found");

            AddLogMessage("Extracting archive...");

            try
            {
                if (!Directory.Exists(SegoeFluentIconsExtractPath))
                    Directory.CreateDirectory(SegoeFluentIconsExtractPath);

                ZipFile.ExtractToDirectory(SegoeFluentIconsZipPath, SegoeFluentIconsExtractPath, true);

                var ttfFiles = Directory.GetFiles(SegoeFluentIconsExtractPath, "*.ttf", SearchOption.AllDirectories);
                if (ttfFiles.Length == 0)
                    throw new FileNotFoundException("No TTF files found in the archive");

                foreach (var ttfFile in ttfFiles)
                {
                    if (Path.GetFileName(ttfFile).Equals(SegoeFluentIconsTtfName, StringComparison.OrdinalIgnoreCase))
                    {
                        AddLogMessage($"TTF file found: {Path.GetFileName(ttfFile)}");
                        return ttfFile;
                    }
                }

                AddLogMessage($"Specific font not found, using: {Path.GetFileName(ttfFiles[0])}");
                return ttfFiles[0];
            }
            catch (Exception ex)
            {
                throw new Exception($"Error extracting: {ex.Message}", ex);
            }
        }

        private async Task InstallFont(string ttfPath)
        {
            if (!File.Exists(ttfPath))
                throw new FileNotFoundException("TTF file not found");

            AddLogMessage("Installing font...");

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                throw new PlatformNotSupportedException("Font installation is only supported on Windows");

            try
            {
                string fontsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Fonts");
                string destPath = Path.Combine(fontsDir, Path.GetFileName(ttfPath));

                // Check if elevation is needed to copy the file
                try
                {
                    File.Copy(ttfPath, destPath, true);
                }
                catch (UnauthorizedAccessException)
                {
                    // If we don't have permissions, use an elevated process
                    await RunElevatedFontInstaller(ttfPath);
                    return;
                }

                using (var fontsKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Fonts", true))
                {
                    if (fontsKey == null)
                        throw new Exception("Unable to access Windows registry for fonts");

                    fontsKey.SetValue("Segoe Fluent Icons (TrueType)", Path.GetFileName(ttfPath));
                }

                NativeMethods.AddFontResource(destPath);
                NativeMethods.SendMessage(NativeMethods.HWND_BROADCAST, NativeMethods.WM_FONTCHANGE, IntPtr.Zero, IntPtr.Zero);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error installing font: {ex.Message}");
            }
        }

        private async Task RunElevatedFontInstaller(string ttfPath)
        {
            AddLogMessage("Launching font installation with elevated privileges...");

            // Create a temporary PowerShell script to install the font
            string scriptPath = Path.Combine(Path.GetTempPath(), "InstallFont.ps1");
            string fontFileName = Path.GetFileName(ttfPath);

            // Write the script
            File.WriteAllText(scriptPath, @$"
$fontFileName = '{fontFileName}'
$targetPath = [System.IO.Path]::Combine($env:windir, 'Fonts', $fontFileName)

# Copy the font file
Copy-Item -Path '{ttfPath.Replace("\\", "\\\\")}' -Destination $targetPath -Force

# Add the registry entry
New-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Fonts' -Name 'Segoe Fluent Icons (TrueType)' -Value $fontFileName -PropertyType String -Force

# Notify the system
Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;

public class FontUtils
{{
    [DllImport(""gdi32.dll"", SetLastError = true)]
    public static extern int AddFontResource(string lpszFilename);

    [DllImport(""user32.dll"", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    public static readonly IntPtr HWND_BROADCAST = (IntPtr)0xFFFF;
    public const uint WM_FONTCHANGE = 0x001D;
}}
'@

[FontUtils]::AddFontResource($targetPath)
[FontUtils]::SendMessage([FontUtils]::HWND_BROADCAST, [FontUtils]::WM_FONTCHANGE, [IntPtr]::Zero, [IntPtr]::Zero)

Write-Output 'Installation complete'
");

            // Execute the script with PowerShell as an administrator
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "powershell",
                    Arguments = $"-ExecutionPolicy Bypass -File \"{scriptPath}\"",
                    UseShellExecute = true,
                    Verb = "runas",
                    CreateNoWindow = false
                }
            };

            try
            {
                process.Start();
                await Task.Run(() => process.WaitForExit());

                if (process.ExitCode != 0)
                {
                    throw new Exception($"PowerShell script failed with exit code: {process.ExitCode}");
                }

                AddLogMessage("Font installation completed via PowerShell");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error executing PowerShell script: {ex.Message}");
            }
            finally
            {
                try
                {
                    if (File.Exists(scriptPath))
                        File.Delete(scriptPath);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }

        private void CleanupTempFiles()
        {
            try
            {
                if (File.Exists(SegoeFluentIconsZipPath))
                    File.Delete(SegoeFluentIconsZipPath);
            }
            catch (Exception ex)
            {
                AddLogMessage($"Error deleting zip: {ex.Message}");
            }

            try
            {
                if (Directory.Exists(SegoeFluentIconsExtractPath))
                    Directory.Delete(SegoeFluentIconsExtractPath, true);
            }
            catch (Exception ex)
            {
                AddLogMessage($"Error deleting directory: {ex.Message}");
            }
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    _httpClient?.Dispose();
                    CleanupTempFiles();
                }

                _isDisposed = true;
            }
        }
    }

    internal static class NativeMethods
    {
        [DllImport("gdi32.dll", SetLastError = true)]
        public static extern int AddFontResource(string lpszFilename);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        public static readonly IntPtr HWND_BROADCAST = (IntPtr)0xFFFF;
        public const uint WM_FONTCHANGE = 0x001D;
    }
}