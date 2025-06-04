using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using System.Linq;
using VoltAir.Models;
using VoltAir.ViewModels;
using VoltAir.Views;
using System.Threading.Tasks;
using System;
using System.IO;

namespace VoltAir
{
    public class App : Application
    {
        public static AppSettings Settings { get; private set; } = new();
        
        private const string VERSION_APP = "1.0.0";
        
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }
        
        public override void OnFrameworkInitializationCompleted()
        {
            // Load settings synchronously
            Settings = Task.Run(() => AppSettings.LoadAsync()).Result;
            // Apply the selected theme
            RequestedThemeVariant = Settings.GetThemeVariant();
            
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                DisableAvaloniaDataAnnotationValidation();
                desktop.MainWindow = new MainWindow
                {
                    DataContext = new MainWindowViewModel(),
                };
                
                desktop.MainWindow.Opened += (sender, args) => 
                {
                    Task.Run(CheckForUpdatesAsync);
                };
            }
            
            base.OnFrameworkInitializationCompleted();
        }
        
        private void DisableAvaloniaDataAnnotationValidation()
        {
            var dataValidationPluginsToRemove =
                BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();
            foreach (var plugin in dataValidationPluginsToRemove)
            {
                BindingPlugins.DataValidators.Remove(plugin);
            }
        }
        
        private async Task CheckForUpdatesAsync()
        {
            try
            {
                // Wait UI
                await Task.Delay(1500);

                Console.WriteLine("Starting update check...");
                Console.WriteLine($"Current application version: {VERSION_APP}");

                var versionChecker = new VersionChecker(VERSION_APP);
                await versionChecker.CheckForUpdatesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during update check: {ex.Message}");
            }
        }
    }
}