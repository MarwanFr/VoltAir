using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Styling;

namespace VoltAir.Models
{
    public class AppSettings
    {
        public string Theme { get; set; } = "System";

        private static readonly string ConfigFolder = AppContext.BaseDirectory;
        private static readonly string ConfigFile = Path.Combine(ConfigFolder, "settings.json");

        public static async Task<AppSettings> LoadAsync()
        {
            try
            {
                if (!File.Exists(ConfigFile))
                {
                    return new AppSettings();
                }

                string json = await File.ReadAllTextAsync(ConfigFile);
                var settings = JsonSerializer.Deserialize<AppSettings>(json);

                return settings ?? new AppSettings();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors du chargement des paramètres: {ex.Message}");
                return new AppSettings();
            }
        }

        public async Task SaveAsync()
        {
            try
            {
                if (!Directory.Exists(ConfigFolder))
                {
                    Directory.CreateDirectory(ConfigFolder);
                }

                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(this, options);

                await File.WriteAllTextAsync(ConfigFile, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la sauvegarde des paramètres: {ex.Message}");
            }
        }

        public async Task ResetAsync()
        {
            Theme = "System";
            await SaveAsync();
        }

        public ThemeVariant GetThemeVariant()
        {
            return Theme switch
            {
                "Light" => ThemeVariant.Light,
                "Dark" => ThemeVariant.Dark,
                _ => ThemeVariant.Default
            };
        }
    }
}