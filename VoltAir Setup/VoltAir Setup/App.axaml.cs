using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using VoltAir_Setup.ViewModels;
using VoltAir_Setup.Views;
using VoltAir_Setup.Services;
using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace VoltAir_Setup;

public class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit.
            DisableAvaloniaDataAnnotationValidation();

            // Initialize telemetry
            InitializeTelemetry();

            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // Remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }

    private void InitializeTelemetry()
    {
        try
        {
            // Essayer d'utiliser TelemetryCredentials
            string supabaseUrl = null;
            string supabaseKey = null;

            try
            {
                supabaseUrl = TelemetryCredentials.SupabaseUrl;
                supabaseKey = TelemetryCredentials.SupabaseKey;
            }
            catch
            {
                // Si TelemetryCredentials est manquant ou invalide, on ignore silencieusement
                return;
            }

            if (!string.IsNullOrEmpty(supabaseUrl) && !string.IsNullOrEmpty(supabaseKey))
            {
                // Créer l'instance du service de télémétrie
                var telemetry = new Telemetry(supabaseUrl, supabaseKey);

                // Récupérer la version de l'application
                string version = typeof(App).Assembly.GetName().Version?.ToString() ?? "1.0.0";

                // Envoyer les données d'installation de manière asynchrone
                // La classe Telemetry vérifiera si l'envoi est nécessaire
                Task.Run(async () =>
                {
                    try
                    {
                        await telemetry.SendInstallationDataAsync(version);
                    }
                    catch
                    {
                        // Ignorer silencieusement toute erreur liée à la télémétrie
                    }
                });
            }
        }
        catch
        {
            // Ignorer silencieusement toute erreur liée à la télémétrie
        }
    }
    
    private Dictionary<string, string> LoadEnvironmentVariables()
    {
        var vars = new Dictionary<string, string>();

        try
        {
            // Look for the .env file in the application directory
            string envFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".env");

            if (File.Exists(envFilePath))
            {
                foreach (var line in File.ReadAllLines(envFilePath))
                {
                    // Ignore comments and empty lines
                    if (!string.IsNullOrWhiteSpace(line) && !line.StartsWith("#"))
                    {
                        // Split the line into key and value
                        var parts = line.Split('=', 2);
                        if (parts.Length == 2)
                        {
                            vars[parts[0].Trim()] = parts[1].Trim();
                        }
                    }
                }
            }
            else
            {
                // Handle the case where the .env file does not exist
            }
        }
        catch (Exception ex)
        {
            // Handle exceptions that occur while loading environment variables
        }

        return vars;
    }
}