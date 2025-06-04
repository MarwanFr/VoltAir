using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Interactivity;

namespace VoltAir.Views.Pages;

public partial class Search : UserControl
{
    public bool IsEverythingAvailable { get; }

    private const int EVERYTHING_OK = 0;
    private const int EVERYTHING_ERROR_MEMORY = 1;
    private const int EVERYTHING_ERROR_IPC = 2;
    private const int EVERYTHING_ERROR_REGISTERCLASSEX = 3;
    private const int EVERYTHING_ERROR_CREATEWINDOW = 4;
    private const int EVERYTHING_ERROR_CREATETHREAD = 5;
    private const int EVERYTHING_ERROR_INVALIDINDEX = 6;
    private const int EVERYTHING_ERROR_INVALIDCALL = 7;

    private const int EVERYTHING_REQUEST_FILE_NAME = 0x00000001;
    private const int EVERYTHING_REQUEST_PATH = 0x00000002;
    private const int EVERYTHING_REQUEST_FULL_PATH_AND_FILE_NAME = 0x00000004;
    private const int EVERYTHING_REQUEST_EXTENSION = 0x00000008;
    private const int EVERYTHING_REQUEST_SIZE = 0x00000010;
    private const int EVERYTHING_REQUEST_DATE_CREATED = 0x00000020;
    private const int EVERYTHING_REQUEST_DATE_MODIFIED = 0x00000040;
    private const int EVERYTHING_REQUEST_DATE_ACCESSED = 0x00000080;
    private const int EVERYTHING_REQUEST_ATTRIBUTES = 0x00000100;
    private const int EVERYTHING_REQUEST_FILE_LIST_FILE_NAME = 0x00000200;
    private const int EVERYTHING_REQUEST_RUN_COUNT = 0x00000400;
    private const int EVERYTHING_REQUEST_DATE_RUN = 0x00000800;
    private const int EVERYTHING_REQUEST_DATE_RECENTLY_CHANGED = 0x00001000;
    private const int EVERYTHING_REQUEST_HIGHLIGHTED_FILE_NAME = 0x00002000;
    private const int EVERYTHING_REQUEST_HIGHLIGHTED_PATH = 0x00004000;
    private const int EVERYTHING_REQUEST_HIGHLIGHTED_FULL_PATH_AND_FILE_NAME = 0x00008000;

    private const int EVERYTHING_SORT_NAME_ASCENDING = 1;
    private const int EVERYTHING_SORT_NAME_DESCENDING = 2;
    private const int EVERYTHING_SORT_PATH_ASCENDING = 3;
    private const int EVERYTHING_SORT_PATH_DESCENDING = 4;
    private const int EVERYTHING_SORT_SIZE_ASCENDING = 5;
    private const int EVERYTHING_SORT_SIZE_DESCENDING = 6;
    private const int EVERYTHING_SORT_EXTENSION_ASCENDING = 7;
    private const int EVERYTHING_SORT_EXTENSION_DESCENDING = 8;
    private const int EVERYTHING_SORT_TYPE_NAME_ASCENDING = 9;
    private const int EVERYTHING_SORT_TYPE_NAME_DESCENDING = 10;
    private const int EVERYTHING_SORT_DATE_CREATED_ASCENDING = 11;
    private const int EVERYTHING_SORT_DATE_CREATED_DESCENDING = 12;
    private const int EVERYTHING_SORT_DATE_MODIFIED_ASCENDING = 13;
    private const int EVERYTHING_SORT_DATE_MODIFIED_DESCENDING = 14;
    private const int EVERYTHING_SORT_ATTRIBUTES_ASCENDING = 15;
    private const int EVERYTHING_SORT_ATTRIBUTES_DESCENDING = 16;
    private const int EVERYTHING_SORT_FILE_LIST_FILENAME_ASCENDING = 17;
    private const int EVERYTHING_SORT_FILE_LIST_FILENAME_DESCENDING = 18;
    private const int EVERYTHING_SORT_RUN_COUNT_ASCENDING = 19;
    private const int EVERYTHING_SORT_RUN_COUNT_DESCENDING = 20;
    private const int EVERYTHING_SORT_DATE_RECENTLY_CHANGED_ASCENDING = 21;
    private const int EVERYTHING_SORT_DATE_RECENTLY_CHANGED_DESCENDING = 22;
    private const int EVERYTHING_SORT_DATE_ACCESSED_ASCENDING = 23;
    private const int EVERYTHING_SORT_DATE_ACCESSED_DESCENDING = 24;
    private const int EVERYTHING_SORT_DATE_RUN_ASCENDING = 25;
    private const int EVERYTHING_SORT_DATE_RUN_DESCENDING = 26;

    private const int EVERYTHING_TARGET_MACHINE_X86 = 1;
    private const int EVERYTHING_TARGET_MACHINE_X64 = 2;
    private const int EVERYTHING_TARGET_MACHINE_ARM = 3;

    [DllImport("Everything64.dll", CharSet = CharSet.Unicode)]
    public static extern uint Everything_SetSearchW(string lpSearchString);

    [DllImport("Everything64.dll")]
    public static extern void Everything_SetMatchPath(bool bEnable);

    [DllImport("Everything64.dll")]
    public static extern void Everything_SetMatchCase(bool bEnable);

    [DllImport("Everything64.dll")]
    public static extern void Everything_SetMatchWholeWord(bool bEnable);

    [DllImport("Everything64.dll")]
    public static extern void Everything_SetRegex(bool bEnable);

    [DllImport("Everything64.dll")]
    public static extern void Everything_SetMax(uint dwMax);

    [DllImport("Everything64.dll")]
    public static extern void Everything_SetOffset(uint dwOffset);

    [DllImport("Everything64.dll")]
    public static extern bool Everything_GetMatchPath();

    [DllImport("Everything64.dll")]
    public static extern bool Everything_GetMatchCase();

    [DllImport("Everything64.dll")]
    public static extern bool Everything_GetMatchWholeWord();

    [DllImport("Everything64.dll")]
    public static extern bool Everything_GetRegex();

    [DllImport("Everything64.dll")]
    public static extern uint Everything_GetMax();

    [DllImport("Everything64.dll")]
    public static extern uint Everything_GetOffset();

    [DllImport("Everything64.dll")]
    public static extern IntPtr Everything_GetSearchW();

    [DllImport("Everything64.dll")]
    public static extern uint Everything_GetLastError();

    [DllImport("Everything64.dll")]
    public static extern bool Everything_QueryW(bool bWait);

    [DllImport("Everything64.dll")]
    public static extern void Everything_SortResultsByPath();

    [DllImport("Everything64.dll")]
    public static extern uint Everything_GetNumFileResults();

    [DllImport("Everything64.dll")]
    public static extern uint Everything_GetNumFolderResults();

    [DllImport("Everything64.dll")]
    public static extern uint Everything_GetNumResults();

    [DllImport("Everything64.dll")]
    public static extern uint Everything_GetTotFileResults();

    [DllImport("Everything64.dll")]
    public static extern uint Everything_GetTotFolderResults();

    [DllImport("Everything64.dll")]
    public static extern uint Everything_GetTotResults();

    [DllImport("Everything64.dll")]
    public static extern bool Everything_IsVolumeResult(uint nIndex);

    [DllImport("Everything64.dll")]
    public static extern bool Everything_IsFolderResult(uint nIndex);

    [DllImport("Everything64.dll")]
    public static extern bool Everything_IsFileResult(uint nIndex);

    [DllImport("Everything64.dll", CharSet = CharSet.Unicode)]
    public static extern void Everything_GetResultFullPathName(uint nIndex, StringBuilder lpString, uint nMaxCount);

    [DllImport("Everything64.dll")]
    public static extern void Everything_Reset();

    [DllImport("Everything64.dll", CharSet = CharSet.Unicode)]
    public static extern IntPtr Everything_GetResultFileName(uint nIndex);

    // Everything 1.4
    [DllImport("Everything64.dll")]
    public static extern void Everything_SetSort(uint dwSortType);

    [DllImport("Everything64.dll")]
    public static extern uint Everything_GetSort();

    [DllImport("Everything64.dll")]
    public static extern uint Everything_GetResultListSort();

    [DllImport("Everything64.dll")]
    public static extern void Everything_SetRequestFlags(uint dwRequestFlags);

    [DllImport("Everything64.dll")]
    public static extern uint Everything_GetRequestFlags();

    [DllImport("Everything64.dll")]
    public static extern uint Everything_GetResultListRequestFlags();

    [DllImport("Everything64.dll", CharSet = CharSet.Unicode)]
    public static extern IntPtr Everything_GetResultExtension(uint nIndex);

    [DllImport("Everything64.dll")]
    public static extern bool Everything_GetResultSize(uint nIndex, out long lpFileSize);

    [DllImport("Everything64.dll")]
    public static extern bool Everything_GetResultDateCreated(uint nIndex, out long lpFileTime);

    [DllImport("Everything64.dll")]
    public static extern bool Everything_GetResultDateModified(uint nIndex, out long lpFileTime);

    [DllImport("Everything64.dll")]
    public static extern bool Everything_GetResultDateAccessed(uint nIndex, out long lpFileTime);

    [DllImport("Everything64.dll")]
    public static extern uint Everything_GetResultAttributes(uint nIndex);

    [DllImport("Everything64.dll", CharSet = CharSet.Unicode)]
    public static extern IntPtr Everything_GetResultFileListFileName(uint nIndex);

    [DllImport("Everything64.dll")]
    public static extern uint Everything_GetResultRunCount(uint nIndex);

    [DllImport("Everything64.dll")]
    public static extern bool Everything_GetResultDateRun(uint nIndex, out long lpFileTime);

    [DllImport("Everything64.dll")]
    public static extern bool Everything_GetResultDateRecentlyChanged(uint nIndex, out long lpFileTime);

    [DllImport("Everything64.dll", CharSet = CharSet.Unicode)]
    public static extern IntPtr Everything_GetResultHighlightedFileName(uint nIndex);

    [DllImport("Everything64.dll", CharSet = CharSet.Unicode)]
    public static extern IntPtr Everything_GetResultHighlightedPath(uint nIndex);

    [DllImport("Everything64.dll", CharSet = CharSet.Unicode)]
    public static extern IntPtr Everything_GetResultHighlightedFullPathAndFileName(uint nIndex);

    [DllImport("Everything64.dll")]
    public static extern uint Everything_GetRunCountFromFileName(string lpFileName);

    [DllImport("Everything64.dll")]
    public static extern bool Everything_SetRunCountFromFileName(string lpFileName, uint dwRunCount);

    [DllImport("Everything64.dll")]
    public static extern uint Everything_IncRunCountFromFileName(string lpFileName);

    public class FileResult
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public long Size { get; set; }
        public DateTime DateModified { get; set; }
        public string IconData { get; set; }
        public bool IsDirectory { get; set; }
        public string FullPath => System.IO.Path.Combine(Path, Name);
    }

    public ObservableCollection<FileResult> FileResults { get; } = new();
    public ObservableCollection<FileInfo> PhotoFiles { get; } = new();

    public Search()
    {
        InitializeComponent();
        DataContext = this;

        IsEverythingAvailable = IsEverythingInstalled();

        if (IsEverythingAvailable)
        {
            ConfigEverythingBorder.IsVisible = false;
            SearchSection.IsVisible = true;
        }
        else
        {
            ConfigEverythingBorder.IsVisible = true;
            SearchSection.IsVisible = false;
        }
    }

    private bool IsEverythingInstalled()
    {
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

        var everythingPath1 = Path.Combine(programFiles, "Everything", "Everything.exe");
        var everythingPath2 = Path.Combine(programFilesX86, "Everything", "Everything.exe");

        if (File.Exists(everythingPath1) || File.Exists(everythingPath2)) return true;

        return false;
    }

    private async void ConfigEverything_OnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            ConfigEverythingArrowRightButton.IsVisible = false;
            ConfigEverythingLoadButton.IsVisible = true;

            var downloadUrl = "https://www.voidtools.com/Everything-1.4.1.1027.x64-Setup.exe";

            // Repertory for the installer
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var voltairFolder = Path.Combine(localAppData, "Temp", "VoltAir");
            var downloadPath = Path.Combine(voltairFolder, "Everything-Setup.exe");

            // Create the directory if it does not exist
            Directory.CreateDirectory(voltairFolder);

            // Download the installer
            using (var client = new WebClient())
            {
                await client.DownloadFileTaskAsync(new Uri(downloadUrl), downloadPath);
            }

            // Start the installation
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = downloadPath,
                    UseShellExecute = true
                }
            };

            process.Start();

            TitleConfigEverything.Text = "The Everything installation file has been launched.";
            TextConfigEverything1.IsVisible = false;
            ButtonConfigEverything.IsVisible = false;
            TextConfigEverything2.IsVisible = true;
            ButtonStartEverything.IsVisible = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during the installation: {ex.Message}");
        }
    }

    private void StartEverything_OnClick(object? sender, RoutedEventArgs e)
    {
        ConfigEverythingBorder.IsVisible = false;
        SearchSection.IsVisible = true;
    }

    private void Search_OnClick(object? sender, RoutedEventArgs e)
    {
        // Clear results
        FileResults.Clear();

        // Config Everything settings
        Everything_SetSearchW(SearchBar.Text);
        Everything_SetRequestFlags(EVERYTHING_REQUEST_FILE_NAME | EVERYTHING_REQUEST_PATH |
                                   EVERYTHING_REQUEST_DATE_MODIFIED | EVERYTHING_REQUEST_SIZE);
        Everything_SetSort(EVERYTHING_SORT_DATE_MODIFIED_DESCENDING);

        // Start the search
        Everything_QueryW(true);
        
        // Browse results
        var numResults = Everything_GetNumResults();
        for (uint i = 0; i < numResults; i++)
            try
            {
                var fileName = Marshal.PtrToStringUni(Everything_GetResultFileName(i));
                var pathBuilder = new StringBuilder(260);
                Everything_GetResultFullPathName(i, pathBuilder, 260);
                var fullPath = pathBuilder.ToString();
                var directoryPath = Path.GetDirectoryName(fullPath);

                Everything_GetResultDateModified(i, out var dateModified);
                Everything_GetResultSize(i, out var size);

                var fileResult = new FileResult
                {
                    Name = fileName,
                    Path = directoryPath,
                    Size = size,
                    DateModified = DateTime.FromFileTime(dateModified),
                    IsDirectory = Everything_IsFolderResult(i)
                };

                FileResults.Add(fileResult);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error for result {i}: {ex.Message}");
            }
    }

    private void OpenFile_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is FileResult fileResult)
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = fileResult.FullPath,
                        UseShellExecute = true
                    }
                };
                process.Start();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error on the open: {ex.Message}");
            }
    }
}



// Converter for file size formatting
public class FileSizeConverter : IValueConverter
{
    public static readonly FileSizeConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            var order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }

            return $"{len:0.##} {sizes[order]}";
        }

        return "0 B";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return null;
    }
}