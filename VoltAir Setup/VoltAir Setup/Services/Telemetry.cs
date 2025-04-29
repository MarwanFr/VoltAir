using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Globalization;
using System.Management;
using System.IO;

namespace VoltAir_Setup.Services
{
    public class Telemetry
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private readonly string _supabaseUrl;
        private readonly string _supabaseKey;
        
        // Chemin pour stocker la date du dernier envoi de télémétrie
        private static readonly string _telemetryFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "VoltAir_Setup",
            "telemetry_last_sent.txt");
            
        // Chemin pour stocker l'identifiant d'installation unique
        private static readonly string _installationIdPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "VoltAir_Setup",
            "installation_id.txt");
            
        // Intervalle minimum entre deux envois de télémétrie (7 jours par défaut)
        private static readonly TimeSpan _minimumInterval = TimeSpan.FromDays(7);
        
        // Nombre maximal de tentatives en cas d'échec
        private const int _maxRetryAttempts = 3;
        
        // Délai entre les tentatives (en secondes)
        private const int _retryDelaySeconds = 5;

        public Telemetry(string supabaseUrl, string supabaseKey)
        {
            _supabaseUrl = supabaseUrl;
            _supabaseKey = supabaseKey;
        }

        /// <summary>
        /// Sends installation data to Supabase if enough time has passed since the last send
        /// </summary>
        public async Task SendInstallationDataAsync(string version)
        {
            try
            {
                if (string.IsNullOrEmpty(_supabaseUrl) || string.IsNullOrEmpty(_supabaseKey))
                {
                    return;
                }
                
                // Vérifier si nous devons envoyer des données maintenant
                if (!ShouldSendTelemetry())
                {
                    Console.WriteLine("Skipping telemetry send - minimum interval not elapsed");
                    return;
                }

                string installationId = GetOrCreateInstallationId();
                
                var installData = new
                {
                    id = installationId,
                    ip = GetIpAddress(),
                    version = version,
                    installation_date = DateTime.UtcNow.ToString("o"),
                    region = GetRegion(),
                    windows_edition = GetWindowsEdition()
                };

                await SendDataWithRetryAsync(installData);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception occurred in telemetry: {ex.Message}");
                // Ne pas propager l'exception pour éviter d'affecter l'application
            }
        }
        
        /// <summary>
        /// Essaye d'envoyer les données avec plusieurs tentatives en cas d'échec
        /// </summary>
        private async Task SendDataWithRetryAsync(object data)
        {
            for (int attempt = 0; attempt < _maxRetryAttempts; attempt++)
            {
                try
                {
                    string json = JsonSerializer.Serialize(data);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    _httpClient.DefaultRequestHeaders.Clear();
                    _httpClient.DefaultRequestHeaders.Add("apikey", _supabaseKey);
                    _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _supabaseKey);

                    // Send data to Supabase
                    var response = await _httpClient.PostAsync($"{_supabaseUrl}/rest/v1/installations", content);

                    if (response.IsSuccessStatusCode)
                    {
                        Console.WriteLine("Installation data sent successfully.");
                        // Enregistrer le moment de l'envoi réussi
                        SaveLastTelemetrySendTime();
                        return;
                    }
                    else
                    {
                        string errorResponse = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"Failed to send installation data. Status: {response.StatusCode}, Response: {errorResponse}");
                        
                        // Si ce n'est pas la dernière tentative, attendre avant de réessayer
                        if (attempt < _maxRetryAttempts - 1)
                        {
                            await Task.Delay(TimeSpan.FromSeconds(_retryDelaySeconds));
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception during telemetry attempt {attempt + 1}: {ex.Message}");
                    
                    // Si ce n'est pas la dernière tentative, attendre avant de réessayer
                    if (attempt < _maxRetryAttempts - 1)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(_retryDelaySeconds));
                    }
                }
            }
        }
        
        /// <summary>
        /// Détermine si nous devons envoyer des données de télémétrie maintenant
        /// en fonction de la dernière fois qu'elles ont été envoyées
        /// </summary>
        private bool ShouldSendTelemetry()
        {
            try
            {
                string directory = Path.GetDirectoryName(_telemetryFilePath);
                
                // Vérifier si le répertoire existe, sinon le créer
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                    return true; // Première exécution, on doit envoyer
                }

                // Si le fichier n'existe pas, c'est la première exécution
                if (!File.Exists(_telemetryFilePath))
                {
                    return true;
                }

                // Lire la dernière date d'envoi
                string lastSentText = File.ReadAllText(_telemetryFilePath);
                if (DateTime.TryParse(lastSentText, out DateTime lastSentTime))
                {
                    // Vérifier si suffisamment de temps s'est écoulé depuis le dernier envoi
                    return DateTime.Now - lastSentTime > _minimumInterval;
                }
                
                return true; // En cas d'erreur de parsing, on envoie pour être sûr
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking telemetry timing: {ex.Message}");
                // En cas d'erreur, on suppose qu'il faut envoyer
                return true;
            }
        }
        
        /// <summary>
        /// Enregistre le moment actuel comme étant la dernière fois 
        /// où les données de télémétrie ont été envoyées
        /// </summary>
        private void SaveLastTelemetrySendTime()
        {
            try
            {
                string directory = Path.GetDirectoryName(_telemetryFilePath);
                
                // Créer le répertoire si nécessaire
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Enregistrer la date et l'heure actuelles
                File.WriteAllText(_telemetryFilePath, DateTime.Now.ToString("o"));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving telemetry timing: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Obtient ou crée un identifiant unique pour cette installation
        /// </summary>
        private string GetOrCreateInstallationId()
        {
            try
            {
                string directory = Path.GetDirectoryName(_installationIdPath);
                
                // Créer le répertoire si nécessaire
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Si le fichier existe, lire l'ID
                if (File.Exists(_installationIdPath))
                {
                    return File.ReadAllText(_installationIdPath).Trim();
                }

                // Sinon, créer un nouvel ID
                string newId = Guid.NewGuid().ToString();
                File.WriteAllText(_installationIdPath, newId);
                return newId;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error managing installation ID: {ex.Message}");
                // En cas d'erreur, générer un ID temporaire
                return Guid.NewGuid().ToString();
            }
        }

        /// <summary>
        /// Retrieves the IP address of the machine
        /// </summary>
        private static string GetIpAddress()
        {
            try
            {
                // Get the first non-loopback IP address
                foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (networkInterface.OperationalStatus == OperationalStatus.Up)
                    {
                        foreach (var ip in networkInterface.GetIPProperties().UnicastAddresses)
                        {
                            if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork &&
                                !ip.Address.ToString().Equals("127.0.0.1"))
                            {
                                return ip.Address.ToString();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception occurred while getting IP address: {ex.Message}");
            }

            return "None";
        }

        /// <summary>
        /// Retrieves the region (country) of the machine
        /// </summary>
        private static string GetRegion()
        {
            try
            {
                RegionInfo regionInfo = new RegionInfo(CultureInfo.CurrentCulture.LCID);
                return regionInfo.DisplayName;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception occurred while getting region: {ex.Message}");
                return "Unknown";
            }
        }

        /// <summary>
        /// Retrieves the Windows edition
        /// </summary>
        private static string GetWindowsEdition()
        {
            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT Caption FROM Win32_OperatingSystem"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        return obj["Caption"]?.ToString() ?? "Unknown";
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception occurred while getting Windows edition: {ex.Message}");
            }

            return "Unknown";
        }
    }
}