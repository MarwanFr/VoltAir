using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using VoltAir.Views.Components;

namespace VoltAir.Views.Pages.BoostWindows
{
    public partial class CleanFolders : Window
    {
        private readonly string _tempPath;
        private readonly string _prefetchPath;
        private readonly string _winTempPath;
        private readonly string _softwareDistributionPath;
        private readonly string _downloadsPath;

        private long _tempSize = 0;
        private long _prefetchSize = 0;
        private long _winTempSize = 0;
        private long _softwareDistributionSize = 0;
        private long _downloadsSize = 0;
        private long _recycleBinSize = 0;

        private bool _allSelected = false;

        // Toast service for notifications
        private ToastService _toastService;

        // Progress tracking
        private long _totalBytesToDelete = 0;
        private long _totalBytesDeleted = 0;
        private int _filesCount = 0;
        private int _filesDeleted = 0;
        private Dictionary<string, long> _folderSizes = new Dictionary<string, long>();

        public CleanFolders()
        {
            InitializeComponent();
            _toastService = new ToastService(ToastContainer);

            _tempPath = Path.GetTempPath();
            _prefetchPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Prefetch");
            _winTempPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp");
            _softwareDistributionPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "SoftwareDistribution", "Download");
            _downloadsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");

            TempSizeText.Text = "Calculating...";
            PrefetchSizeText.Text = "Calculating...";
            WinTempSizeText.Text = "Calculating...";
            SoftwareDistributionSizeText.Text = "Calculating...";
            DownloadsSizeText.Text = "Calculating...";
            RecycleBinSizeText.Text = "Calculating...";
            TotalSizeText.Text = "0 B";

            CalculateFolderSizes();
        }

        private async void CalculateFolderSizes()
        {
            try
            {
                await Task.Run(async () =>
                {
                    try
                    {
                        _tempSize = CalculateDirectorySize(_tempPath);
                        _folderSizes[_tempPath] = _tempSize;
                        await UpdateSizeLabel(TempSizeText, _tempSize);

                        _prefetchSize = CalculateDirectorySize(_prefetchPath);
                        _folderSizes[_prefetchPath] = _prefetchSize;
                        await UpdateSizeLabel(PrefetchSizeText, _prefetchSize);

                        _winTempSize = CalculateDirectorySize(_winTempPath);
                        _folderSizes[_winTempPath] = _winTempSize;
                        await UpdateSizeLabel(WinTempSizeText, _winTempSize);

                        _softwareDistributionSize = CalculateDirectorySize(_softwareDistributionPath);
                        _folderSizes[_softwareDistributionPath] = _softwareDistributionSize;
                        await UpdateSizeLabel(SoftwareDistributionSizeText, _softwareDistributionSize);

                        _downloadsSize = CalculateDirectorySize(_downloadsPath);
                        _folderSizes[_downloadsPath] = _downloadsSize;
                        await UpdateSizeLabel(DownloadsSizeText, _downloadsSize);

                        _recycleBinSize = GetRecycleBinSize();
                        _folderSizes["recycle_bin"] = _recycleBinSize;
                        await UpdateSizeLabel(RecycleBinSizeText, _recycleBinSize);

                        await UpdateTotalSize();

                        await _toastService.ShowInfo("Folder size calculation completed", "Size Calculation");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error calculating folder sizes: {ex.Message}");
                        await _toastService.ShowError($"Error calculating folder sizes: {ex.Message}", "Calculation Error");
                    }
                });
            }
            catch (Exception ex)
            {
                await _toastService.ShowError($"Error starting calculation: {ex.Message}", "Error");
            }
        }

        private async Task UpdateSizeLabel(TextBlock textBlock, long size)
        {
            string sizeText = FormatSize(size);
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                textBlock.Text = sizeText;
            });
        }

        private async Task UpdateTotalSize()
        {
            long totalSize = 0;

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (CheckTemp.IsChecked == true) totalSize += _tempSize;
                if (CheckPrefetch.IsChecked == true) totalSize += _prefetchSize;
                if (CheckWindowsTemp.IsChecked == true) totalSize += _winTempSize;
                if (CheckSoftwareDistribution.IsChecked == true) totalSize += _softwareDistributionSize;
                if (CheckDownloads.IsChecked == true) totalSize += _downloadsSize;
                if (CheckRecycleBin.IsChecked == true) totalSize += _recycleBinSize;

                TotalSizeText.Text = FormatSize(totalSize);
            });
        }

        private long CalculateDirectorySize(string folderPath)
        {
            long folderSize = 0;

            try
            {
                if (!Directory.Exists(folderPath))
                    return 0;

                foreach (var file in Directory.GetFiles(folderPath))
                {
                    try
                    {
                        long fileSize = new FileInfo(file).Length;
                        folderSize += fileSize;
                        Debug.WriteLine($"File: {file}, Size: {fileSize} bytes");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error calculating size for file {file}: {ex.Message}");
                    }
                }

                foreach (var dir in Directory.GetDirectories(folderPath))
                {
                    try
                    {
                        long dirSize = CalculateDirectorySize(dir);
                        folderSize += dirSize;
                        Debug.WriteLine($"Directory: {dir}, Size: {dirSize} bytes");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error calculating size for directory {dir}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error calculating size for folder {folderPath}: {ex.Message}");
            }

            return folderSize;
        }


        private int CountFilesInDirectory(string path)
        {
            int count = 0;

            try
            {
                if (!Directory.Exists(path))
                    return 0;

                count += Directory.GetFiles(path).Length;

                foreach (var dir in Directory.GetDirectories(path))
                {
                    count += CountFilesInDirectory(dir);
                }
            }
            catch { }

            return count;
        }

        private string FormatSize(long bytes)
        {
            if (bytes <= 0) return "0 B";
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            double len = bytes;

            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }

            return $"{len:0.##} {sizes[order]}";
        }

        private void CheckBox_Changed(object? sender, RoutedEventArgs e) => _ = UpdateTotalSize();

        private void SelectAllCheckbox_Changed(object? sender, RoutedEventArgs e)
        {
            bool isChecked = CheckAll.IsChecked == true;

            CheckTemp.IsChecked = isChecked;
            CheckPrefetch.IsChecked = isChecked;
            CheckWindowsTemp.IsChecked = isChecked;
            CheckSoftwareDistribution.IsChecked = isChecked;
            CheckDownloads.IsChecked = isChecked;
            CheckRecycleBin.IsChecked = isChecked;

            _ = UpdateTotalSize();

            // Toast notification for selecting all options
            if (isChecked)
            {
                _ = _toastService.ShowInfo("All folders selected", "Selection");
            }
            else
            {
                _ = _toastService.ShowInfo("All folders deselected", "Selection");
            }
        }

        private async void OnAskDeleteFilesClick(object? sender, RoutedEventArgs e)
        {
            if (new[] { CheckTemp, CheckPrefetch, CheckWindowsTemp, CheckSoftwareDistribution, CheckDownloads, CheckRecycleBin }
                .All(cb => cb.IsChecked != true))
            {
                // Toast notification for selection error
                await _toastService.ShowWarning("Please select at least one folder to clean", "Selection Required");
                return;
            }

            string confirmMessage = $"Are you sure you want to delete these files (Size: {TotalSizeText.Text})? This action cannot be undone.";
            CleanConfirmPopup.Show(confirmMessage);
            CleanConfirmPopup.Confirmed += OnConfirmDeleteFiles;
            CleanConfirmPopup.Canceled += OnCancelDeleteFiles;
        }

        private void OnConfirmDeleteFiles(object? sender, EventArgs e)
        {
            CleanConfirmPopup.Confirmed -= OnConfirmDeleteFiles;
            PerformDeletion();
        }

        private async void OnCancelDeleteFiles(object? sender, EventArgs e)
        {
            CleanConfirmPopup.Canceled -= OnCancelDeleteFiles;

            // Toast notification for cancellation
            await _toastService.ShowInfo("Clean operation canceled", "Canceled");
        }

        private async void PerformDeletion()
        {
            try
            {
                // Reset progress tracking
                _totalBytesToDelete = 0;
                _totalBytesDeleted = 0;
                _filesCount = 0;
                _filesDeleted = 0;

                bool checkTemp = CheckTemp.IsChecked == true;
                bool checkPrefetch = CheckPrefetch.IsChecked == true;
                bool checkWinTemp = CheckWindowsTemp.IsChecked == true;
                bool checkSD = CheckSoftwareDistribution.IsChecked == true;
                bool checkDownloads = CheckDownloads.IsChecked == true;
                bool checkRecycleBin = CheckRecycleBin.IsChecked == true;

                // Compute total files count and size to delete
                await Task.Run(async () => {
                    if (checkTemp) {
                        _totalBytesToDelete += _folderSizes[_tempPath];
                        _filesCount += CountFilesInDirectory(_tempPath);
                    }
                    if (checkPrefetch) {
                        _totalBytesToDelete += _folderSizes[_prefetchPath];
                        _filesCount += CountFilesInDirectory(_prefetchPath);
                    }
                    if (checkWinTemp) {
                        _totalBytesToDelete += _folderSizes[_winTempPath];
                        _filesCount += CountFilesInDirectory(_winTempPath);
                    }
                    if (checkSD) {
                        _totalBytesToDelete += _folderSizes[_softwareDistributionPath];
                        _filesCount += CountFilesInDirectory(_softwareDistributionPath);
                    }
                    if (checkDownloads) {
                        _totalBytesToDelete += _folderSizes[_downloadsPath];
                        _filesCount += CountFilesInDirectory(_downloadsPath);
                    }
                    if (checkRecycleBin) {
                        _totalBytesToDelete += _folderSizes["recycle_bin"];
                        // No folder structure for recycle bin
                    }

                    await Dispatcher.UIThread.InvokeAsync(() => {
                        ProgressDialog.ShowProgress("Cleaning in progress...", $"Deleting {_filesCount} files ({FormatSize(_totalBytesToDelete)})", _totalBytesToDelete, _filesCount);
                    });
                });

                // Build a summary message
                string summary = "Cleaned: ";
                if (checkTemp) summary += "Temp, ";
                if (checkPrefetch) summary += "Prefetch, ";
                if (checkWinTemp) summary += "Windows Temp, ";
                if (checkSD) summary += "Software Distribution, ";
                if (checkDownloads) summary += "Downloads, ";
                if (checkRecycleBin) summary += "Recycle Bin, ";

                // Remove the final comma
                if (summary.EndsWith(", "))
                    summary = summary.Substring(0, summary.Length - 2);

                await Task.Run(async () =>
                {
                    try
                    {
                        if (checkTemp) await DeleteFilesInDirectoryAsync(_tempPath);
                        if (ProgressDialog.IsOperationCancelled()) return;

                        if (checkPrefetch) await DeleteFilesInDirectoryAsync(_prefetchPath);
                        if (ProgressDialog.IsOperationCancelled()) return;

                        if (checkWinTemp) await DeleteFilesInDirectoryAsync(_winTempPath);
                        if (ProgressDialog.IsOperationCancelled()) return;

                        if (checkSD) await DeleteFilesInDirectoryAsync(_softwareDistributionPath);
                        if (ProgressDialog.IsOperationCancelled()) return;

                        if (checkDownloads) await DeleteFilesInDirectoryAsync(_downloadsPath);
                        if (ProgressDialog.IsOperationCancelled()) return;

                        // Not deleting Recycle Bin via directory
                        if (checkRecycleBin) {
                            await Task.Run(() => {
                                EmptyRecycleBin();
                                // Update progress after recycle bin is emptied
                                ProgressDialog.UpdateProgress(_folderSizes["recycle_bin"], 0);
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log the exception if needed, but do not propagate it
                        Debug.WriteLine($"Error during deletion: {ex.Message}");
                    }
                });

                // Hide progress UI
                ProgressDialog.HideProgress();

                // Recalculate sizes
                CalculateFolderSizes();

                // Toast notification for success
                await _toastService.ShowSuccess($"Cleaning completed successfully!\n{summary}", "Cleaning Complete");
            }
            catch (Exception ex)
            {
                // Hide progress UI
                ProgressDialog.HideProgress();

                // Toast notification for error
                await _toastService.ShowError($"Error while cleaning: {ex.Message}", "Cleaning Error");
            }
        }

        private async Task DeleteFilesInDirectoryAsync(string path)
{
    if (!Directory.Exists(path)) return;

    // First try to delete files
    foreach (var file in Directory.GetFiles(path))
    {
        if (ProgressDialog.IsOperationCancelled()) return;

        await Task.Run(() =>
        {
            try
            {
                var fileInfo = new FileInfo(file);
                
                // Clear read-only attribute if set
                if (fileInfo.Attributes.HasFlag(FileAttributes.ReadOnly))
                {
                    fileInfo.Attributes &= ~FileAttributes.ReadOnly;
                }

                long fileSize = fileInfo.Length;
                File.Delete(file);
                ProgressDialog.UpdateProgress(fileSize, 1);
            }
            catch (UnauthorizedAccessException)
            {
                // Try again with admin privileges if needed
                try
                {
                    File.SetAttributes(file, FileAttributes.Normal);
                    File.Delete(file);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error deleting file {file}: {ex.Message}");
                }
            }
            catch (IOException ioEx) when (ioEx.Message.Contains("used by another process"))
            {
                // Skip files that are in use
                Debug.WriteLine($"File in use, skipping: {file}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error deleting file {file}: {ex.Message}");
            }
        });
    }

    // Then try to delete directories
    foreach (var dir in Directory.GetDirectories(path))
    {
        if (ProgressDialog.IsOperationCancelled()) return;

        await DeleteFilesInDirectoryAsync(dir);

        await Task.Run(() =>
        {
            try
            {
                var dirInfo = new DirectoryInfo(dir);
                
                // Clear read-only attribute if set
                if (dirInfo.Attributes.HasFlag(FileAttributes.ReadOnly))
                {
                    dirInfo.Attributes &= ~FileAttributes.ReadOnly;
                }

                if (!Directory.EnumerateFileSystemEntries(dir).Any())
                {
                    Directory.Delete(dir);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error deleting directory {dir}: {ex.Message}");
            }
        });
    }
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

        // Recycle Bin support
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct SHQUERYRBINFO
        {
            public int cbSize;
            public long i64Size;
            public long i64NumItems;
        }

        [DllImport("Shell32.dll", CharSet = CharSet.Unicode)]
        private static extern int SHQueryRecycleBin(string? pszRootPath, ref SHQUERYRBINFO pSHQueryRBInfo);

        [DllImport("Shell32.dll")]
        private static extern int SHEmptyRecycleBin(IntPtr hwnd, string? pszRootPath, int dwFlags);

        private long GetRecycleBinSize()
        {
            SHQUERYRBINFO queryInfo = new SHQUERYRBINFO();
            queryInfo.cbSize = Marshal.SizeOf(typeof(SHQUERYRBINFO));

            int result = SHQueryRecycleBin(null, ref queryInfo);
            return result == 0 ? queryInfo.i64Size : 0;
        }

        private void EmptyRecycleBin()
        {
            const int SHERB_NOCONFIRMATION = 0x00000001;
            const int SHERB_NOPROGRESSUI = 0x00000002;
            const int SHERB_NOSOUND = 0x00000004;

            try
            {
                SHEmptyRecycleBin(IntPtr.Zero, null, SHERB_NOCONFIRMATION | SHERB_NOPROGRESSUI | SHERB_NOSOUND);
            }
            catch (Exception ex)
            {
                // Notification in case of error while emptying the Recycle Bin
                _ = _toastService.ShowError($"Error emptying Recycle Bin: {ex.Message}", "Recycle Bin Error");
            }
        }

        private void CancelDeletionButton_Click(object sender, RoutedEventArgs e)
        {
            ProgressDialog.CancelDeletion();
        }
    }
}