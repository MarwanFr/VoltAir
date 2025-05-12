using Microsoft.Win32;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace VoltAir.Views.Components.AppManager;

public class ProgramManager
{
    public List<Application> GetInstalledApplications()
    {
        var applications = new List<Application>();

        var registryPaths = new[]
        {
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
            @"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
        };

        foreach (var registryPath in registryPaths)
        {
            using (var key = Registry.LocalMachine.OpenSubKey(registryPath))
            {
                if (key == null) continue;

                foreach (var subkeyName in key.GetSubKeyNames())
                {
                    using (var subkey = key.OpenSubKey(subkeyName))
                    {
                        var displayName = subkey.GetValue("DisplayName") as string;
                        if (string.IsNullOrEmpty(displayName)) continue;

                        var installLocation = subkey.GetValue("InstallLocation") as string;
                        var uninstallString = subkey.GetValue("UninstallString") as string;
                        var publisher = subkey.GetValue("Publisher") as string;
                        var installDateRaw = subkey.GetValue("InstallDate") as string;

                        string formattedInstallDate = null;
                        if (!string.IsNullOrEmpty(installDateRaw) && installDateRaw.Length == 8)
                        {
                            formattedInstallDate = $"{installDateRaw.Substring(0, 4)}-{installDateRaw.Substring(4, 2)}-{installDateRaw.Substring(6, 2)}";
                        }

                        var estimatedSize = subkey.GetValue("EstimatedSize") as int?;

                        var app = new Application
                        {
                            Name = displayName,
                            UninstallString = uninstallString,
                            InstallLocation = installLocation,
                            Publisher = publisher,
                            InstallDate = formattedInstallDate,
                            SizeInBytes = estimatedSize.HasValue ? estimatedSize.Value * 1024 : 0
                        };

                        applications.Add(app);
                    }
                }
            }
        }

        Parallel.ForEach(applications.Where(a => a.SizeInBytes == 0), app =>
        {
            if (!string.IsNullOrEmpty(app.InstallLocation))
            {
                app.SizeInBytes = CalculateFolderSize(app.InstallLocation);
            }
        });

        return applications;
    }

    private long CalculateFolderSize(string folderPath)
    {
        try
        {
            if (!Directory.Exists(folderPath)) return 0;
            var dirInfo = new DirectoryInfo(folderPath);
            return dirInfo.EnumerateFiles("*", SearchOption.AllDirectories).Sum(fi => fi.Length);
        }
        catch
        {
            return 0;
        }
    }
}

public class Application
{
    public string Name { get; set; }
    public string UninstallString { get; set; }
    public string InstallLocation { get; set; }
    public long SizeInBytes { get; set; }
    public string Publisher { get; set; }
    public string InstallDate { get; set; }

    public string FormattedSize =>
        SizeInBytes == 0 ? "Calculating..." : FormatSize(SizeInBytes);

    private string FormatSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        int order = 0;
        double len = bytes;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}