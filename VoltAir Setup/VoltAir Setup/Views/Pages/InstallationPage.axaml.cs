using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace VoltAir_Setup.Views.Pages;

public partial class InstallationPage : UserControl
{

    // Installation directory
    private readonly string InstallDir = @"C:\Program Files (x86)\VoltAir";
    private readonly string TempDir = @"C:\Program Files (x86)\VoltAir\temp";
    
    public InstallationPage()
    {
        InitializeComponent();
        
        // Get UI elements
        progressBar = this.FindControl<ProgressBar>("progressBar");
        statusText = this.FindControl<TextBlock>("statusText");
        installButton = this.FindControl<Button>("installButton");
        completeButton = this.FindControl<Button>("completeButton");
        cancelButton = this.FindControl<Button>("cancelButton");
        desktopShortcutCheck = this.FindControl<CheckBox>("desktopShortcutCheck");
        startMenuCheck = this.FindControl<CheckBox>("startMenuCheck");
        autoStartCheck = this.FindControl<CheckBox>("autoStartCheck");
        
        // Initialize UI
        installButton.IsVisible = true;
        completeButton.IsVisible = false;
    }

    // Event handlers
    public async void Install_Click(object sender, RoutedEventArgs e)
    {
        // Hide install button during installation
        installButton.IsVisible = false;
        await InstallAsync();
    }

    public void Complete_Click(object sender, RoutedEventArgs e)
    {
        // Launch application or close installer
        Environment.Exit(0);
    }

    public void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Environment.Exit(0);
    }

    // Installation process
    private async Task InstallAsync()
    {
        try
        {
            // Check disk space
            if (!CheckDiskSpace())
            {
                ShowError("Not enough disk space. At least 100MB required.");
                installButton.IsVisible = true;
                return;
            }

            // Prepare directories
            await PrepareInstallationAsync();
            
            // Extract files
            await ExtractFilesAsync();
            
            // Install files
            await InstallFilesAsync();
            
            // Create shortcuts
            await CreateShortcutsAsync();
            
            // Finalize
            await FinalizeInstallationAsync();
            
            // Show complete button
            await Dispatcher.UIThread.InvokeAsync(() => 
            {
                completeButton.IsVisible = true;
            });
        }
        catch (Exception ex)
        {
            await Dispatcher.UIThread.InvokeAsync(() => 
            {
                UpdateStatus($"Error: {ex.Message}");
                ShowError($"Installation failed: {ex.Message}");
                installButton.IsVisible = true;
            });
        }
    }

    private bool CheckDiskSpace()
    {
        var drive = new DriveInfo("C");
        return drive.AvailableFreeSpace >= 100_000_000; // 100MB
    }

    private async Task PrepareInstallationAsync()
    {
        await Dispatcher.UIThread.InvokeAsync(() => 
        {
            UpdateStatus("Preparing installation...");
            UpdateProgress(5);
        });

        await Task.Delay(500); // UI update delay

        // Close any running instances of the application
        await TerminateProcessesAsync();

        // Create or clean directories
        await CreateOrCleanDirectoryAsync(InstallDir);
        await CreateOrCleanDirectoryAsync(TempDir);

        await Dispatcher.UIThread.InvokeAsync(() => UpdateProgress(15));
    }

    private async Task TerminateProcessesAsync()
    {
        string[] processes = { "VoltAir.exe", "VoltAirUpdater.exe" };
        foreach (var process in processes)
        {
            await RunProcessAsync("taskkill", $"/f /im \"{process}\"");
        }
    }

    private async Task ExtractFilesAsync()
    {
        await Dispatcher.UIThread.InvokeAsync(() => 
        {
            UpdateStatus("Extracting files...");
            UpdateProgress(25);
        });

        await Task.Delay(500); // UI update delay

        // Extract embedded resources - just install.zip now
        ExtractResource("VoltAir_Setup.Resources.Files.install.zip", Path.Combine(TempDir, "install.zip"));

        await Dispatcher.UIThread.InvokeAsync(() => UpdateProgress(40));
    }

    private async Task InstallFilesAsync()
    {
        await Dispatcher.UIThread.InvokeAsync(() => 
        {
            UpdateStatus("Installing files...");
            UpdateProgress(50);
        });

        // Extract zip files
        ZipFile.ExtractToDirectory(Path.Combine(TempDir, "install.zip"), InstallDir, true);

        // Extract uninstall.exe from resources to installation directory
        ExtractResource("VoltAir_Setup.Resources.Files.uninstall.exe", 
            Path.Combine(InstallDir, "uninstall.exe"));

        // Register application in Windows registry
        await RegisterApplicationAsync();

        await Dispatcher.UIThread.InvokeAsync(() => UpdateProgress(75));
    }

    private async Task CreateShortcutsAsync()
    {
        await Dispatcher.UIThread.InvokeAsync(() => 
        {
            UpdateStatus("Creating shortcuts...");
            UpdateProgress(85);
        });

        // Create Start Menu shortcut if checked
        if (startMenuCheck.IsChecked == true)
        {
            string startMenuPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu),
                "Programs",
                "VoltAir.lnk");
                
            await CreateShortcutAsync(startMenuPath);
        }

        // Create Desktop shortcut if checked
        if (desktopShortcutCheck.IsChecked == true)
        {
            string desktopPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory),
                "VoltAir.lnk");
                
            await CreateShortcutAsync(desktopPath);
        }

        // Set up autostart if checked
        if (autoStartCheck.IsChecked == true)
        {
            await SetAutoStartAsync();
        }
    }

    private async Task FinalizeInstallationAsync()
    {
        await Dispatcher.UIThread.InvokeAsync(() => 
        {
            UpdateStatus("Finalizing installation...");
            UpdateProgress(95);
        });

        // Clean up temporary files
        await CleanupAsync();

        await Dispatcher.UIThread.InvokeAsync(() => 
        {
            UpdateStatus("Installation complete!");
            UpdateProgress(100);
        });
    }

    // Helper methods
    private async Task RunProcessAsync(string fileName, string arguments)
    {
        await Task.Run(() =>
        {
            try
            {
                using (var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = fileName,
                        Arguments = arguments,
                        WindowStyle = ProcessWindowStyle.Hidden,
                        CreateNoWindow = true,
                        UseShellExecute = false
                    }
                })
                {
                    process.Start();
                    process.WaitForExit();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Process error: {ex.Message}");
                // Continue even if process fails
            }
        });
    }

    private void ExtractResource(string resourceName, string outputPath)
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    Debug.WriteLine($"Resource not found: {resourceName}");
                    return; // Skip if resource not found
                }

                using (var fileStream = new FileStream(outputPath, FileMode.Create))
                {
                    stream.CopyTo(fileStream);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Resource extraction error: {ex.Message}");
        }
    }

    private async Task CreateOrCleanDirectoryAsync(string path)
    {
        await Task.Run(() => 
        {
            try
            {
                if (Directory.Exists(path))
                {
                    try
                    {
                        Directory.Delete(path, true);
                    }
                    catch
                    {
                        // If we can't delete, try to clean files
                        foreach (var file in Directory.GetFiles(path))
                        {
                            try { File.Delete(file); } catch { }
                        }
                    }
                }
                
                Directory.CreateDirectory(path);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Directory error: {ex.Message}");
            }
        });
    }

    private async Task RegisterApplicationAsync()
    {
        await Task.Run(() =>
        {
            try
            {
                string uninstallKeyPath = @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\VoltAir";
                
                using (var key = Registry.LocalMachine.CreateSubKey(uninstallKeyPath))
                {
                    if (key != null)
                    {
                        key.SetValue("DisplayName", "VoltAir");
                        key.SetValue("UninstallString", $"\"{Path.Combine(InstallDir, "uninstall.exe")}\"");
                        key.SetValue("InstallLocation", InstallDir);
                        key.SetValue("DisplayIcon", $"\"{Path.Combine(InstallDir, "VoltAir.exe")}\"");
                        key.SetValue("Publisher", "VoltAir Inc.");
                        key.SetValue("DisplayVersion", "1.0.0");
                        key.SetValue("URLInfoAbout", "https://voltair.example.com");
                        key.SetValue("NoModify", 1, RegistryValueKind.DWord);
                        key.SetValue("NoRepair", 1, RegistryValueKind.DWord);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Registry error: {ex.Message}");
            }
        });
    }

    private async Task CreateShortcutAsync(string shortcutPath)
    {
        // In a real implementation, you would use IWshRuntimeLibrary
        // For this example, we'll just create a dummy file
        await Task.Run(() =>
        {
            try
            {
                string shortcutDir = Path.GetDirectoryName(shortcutPath);
                if (!Directory.Exists(shortcutDir))
                {
                    Directory.CreateDirectory(shortcutDir);
                }
                
                // In a real implementation:
                // Voici comment créer un vrai raccourci en utilisant PowerShell:
                string psCommand = $@"
                $WshShell = New-Object -comObject WScript.Shell
                $Shortcut = $WshShell.CreateShortcut('{shortcutPath}')
                $Shortcut.TargetPath = '{Path.Combine(InstallDir, "VoltAir.exe")}'
                $Shortcut.WorkingDirectory = '{InstallDir}'
                $Shortcut.IconLocation = '{Path.Combine(InstallDir, "VoltAir.exe")}, 0'
                $Shortcut.Save()
                ";
                
                using (var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "powershell",
                        Arguments = $"-Command \"{psCommand}\"",
                        WindowStyle = ProcessWindowStyle.Hidden,
                        CreateNoWindow = true,
                        UseShellExecute = false
                    }
                })
                {
                    process.Start();
                    process.WaitForExit();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Shortcut creation error: {ex.Message}");
            }
        });
    }

    private async Task SetAutoStartAsync()
    {
        await Task.Run(() =>
        {
            try
            {
                string runKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
                using (var key = Registry.CurrentUser.OpenSubKey(runKeyPath, true))
                {
                    if (key != null)
                    {
                        key.SetValue("VoltAir", $"\"{Path.Combine(InstallDir, "VoltAir.exe")}\"");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Registry error: {ex.Message}");
            }
        });
    }

    private async Task CleanupAsync()
    {
        await Task.Run(() =>
        {
            try
            {
                if (Directory.Exists(TempDir))
                {
                    Directory.Delete(TempDir, true);
                }
            }
            catch (Exception ex) 
            {
                Debug.WriteLine($"Cleanup error: {ex.Message}");
            }
        });
    }

    private void UpdateStatus(string message)
    {
        statusText.Text = message;
    }

    private void UpdateProgress(int value)
    {
        progressBar.Value = value;
    }

    private void ShowError(string message)
    {
        // In a real implementation, show a dialog
        Debug.WriteLine($"ERROR: {message}");
    }
}