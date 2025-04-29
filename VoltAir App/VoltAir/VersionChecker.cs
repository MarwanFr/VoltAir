using System;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Diagnostics;

namespace VoltAir
{
    public class VersionChecker
    {
        private const string VersionUrl = "https://voltair.pages.dev/api/version.json";
        private readonly string _currentVersion;

        public VersionChecker(string currentVersion)
        {
            _currentVersion = currentVersion?.Trim() ?? "";
        }

        public async Task CheckForUpdatesAsync()
        {
            try
            {
                using var httpClient = new HttpClient
                {
                    Timeout = TimeSpan.FromSeconds(10)
                };

                Console.WriteLine($"Downloading version file from {VersionUrl}...");
                var jsonResponse = await httpClient.GetStringAsync(VersionUrl);
                Console.WriteLine($"JSON response received: {jsonResponse}");

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    AllowTrailingCommas = true,
                    ReadCommentHandling = JsonCommentHandling.Skip
                };

                var versionInfo = JsonSerializer.Deserialize<VersionInfo>(jsonResponse, options);

                if (versionInfo == null)
                {
                    Console.WriteLine("Error: Version info is null after deserialization.");
                    return;
                }

                Console.WriteLine($"Remote version: {versionInfo.LastVersion}");
                Console.WriteLine($"Download URL: {versionInfo.DownloadUrl}");

                string localVersion = NormalizeVersion(_currentVersion);
                string remoteVersion = NormalizeVersion(versionInfo.LastVersion);

                Console.WriteLine($"Normalized local version: '{localVersion}'");
                Console.WriteLine($"Normalized remote version: '{remoteVersion}'");

                if (string.IsNullOrEmpty(remoteVersion))
                {
                    Console.WriteLine("Error: Remote version is empty!");
                    return;
                }

                if (localVersion != remoteVersion)
                {
                    Console.WriteLine("New version available. Opening download page...");
                    if (!string.IsNullOrEmpty(versionInfo.DownloadUrl))
                    {
                        OpenUrlInBrowser(versionInfo.DownloadUrl);
                    }
                    else
                    {
                        Console.WriteLine("Error: Download URL is missing.");
                    }
                }
                else
                {
                    Console.WriteLine("Application is up to date.");
                }
            }
            catch (HttpRequestException httpEx)
            {
                Console.WriteLine($"Network error while checking updates: {httpEx.Message}");
                Console.WriteLine("It looks like there is no internet connection or the server is unreachable.");
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine("Request timed out while checking for updates.");
            }
            catch (JsonException jsonEx)
            {
                Console.WriteLine($"JSON deserialization error: {jsonEx.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error while checking for updates: {ex.GetType().Name}: {ex.Message}");
            }
        }

        private string NormalizeVersion(string version)
        {
            if (string.IsNullOrEmpty(version))
                return "";

            return version.Replace(" ", "").ToUpperInvariant();
        }

        private void OpenUrlInBrowser(string url)
        {
            try
            {
                Console.WriteLine($"Opening browser with URL: {url}");

                var psi = new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error opening browser: {ex.Message}");
            }
        }
    }

    public class VersionInfo
    {
        [JsonPropertyName("last_version")]
        public string LastVersion { get; set; }

        [JsonPropertyName("download_url")]
        public string DownloadUrl { get; set; }

        [JsonPropertyName("release_notes")]
        public string ReleaseNotes { get; set; }
    }
}