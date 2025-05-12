using Avalonia.Controls;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;

namespace VoltAir.Views.Components
{
    public partial class MicrosoftActivationScripts : UserControl
    {
        // Toast service for notifications
        private ToastService _toastService;

        public MicrosoftActivationScripts()
        {
            InitializeComponent();
            _toastService = new ToastService(this.FindControl<Panel>("ToastContainer"));
        }

        private async void OnActivateWindowsClick(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            string tempPath = Path.Combine(Path.GetTempPath(), "VoltAir", "MAS");
            string filePath = Path.Combine(tempPath, "MAS_AIO.cmd");
            string url = "https://github.com/massgravel/Microsoft-Activation-Scripts/raw/master/MAS/All-In-One-Version-KL/MAS_AIO.cmd";

            try
            {
                // Ensure the directory exists
                if (!Directory.Exists(tempPath))
                {
                    Directory.CreateDirectory(tempPath);
                }

                // Download the file
                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage response = await client.GetAsync(url);
                    response.EnsureSuccessStatusCode();

                    byte[] fileBytes = await response.Content.ReadAsByteArrayAsync();
                    await File.WriteAllBytesAsync(filePath, fileBytes);
                }

                // Step 2: Launch the script with elevated privileges and send the option "1"
                var psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c echo 1 | \"{filePath}\"",
                    Verb = "runas", // Request admin rights
                    UseShellExecute = true,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                Process.Start(psi);
            }
            catch (Exception ex)
            {
                await _toastService.ShowError($"Échec de l’activation : {ex.Message}", "Erreur");
            }
        }
    }
}