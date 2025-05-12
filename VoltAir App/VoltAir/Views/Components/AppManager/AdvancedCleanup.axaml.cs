using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System.Diagnostics;
using System.Linq;
using Avalonia.Layout;
using Avalonia.Media;
using VoltAir.Views.Components;

namespace VoltAir.Views.Components.AppManager
{
    public partial class AdvancedCleanup : Window
    {
        private StackPanel applicationsPanel;
        private ToastService _toastService;

        public AdvancedCleanup()
        {
            InitializeComponent();
            LoadApplications();
            _toastService = new ToastService(this.FindControl<Panel>("ToastContainer"));
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            ConfirmPopup = this.FindControl<ConfirmDialog>("ConfirmPopup");
            applicationsPanel = this.FindControl<StackPanel>("ApplicationsPanel");
        }

        private void LoadApplications()
        {
            var programManager = new ProgramManager();
            var applications = programManager.GetInstalledApplications();

            foreach (var app in applications)
            {
                var cardBorder = new Border
                {
                    Classes = { "Cards" },
                    Padding = new Thickness(15),
                    Margin = new Thickness(0, 0, 0, 10)
                };

                var mainGrid = new Grid
                {
                    ColumnDefinitions = new ColumnDefinitions("*,Auto"),
                    RowDefinitions = new RowDefinitions("Auto,Auto,Auto")
                };

                // App Name
                var appNameLabel = new TextBlock
                {
                    Text = app.Name,
                    FontWeight = FontWeight.Bold,
                    FontSize = 14,
                    Margin = new Thickness(0, 0, 0, 5)
                };
                Grid.SetRow(appNameLabel, 0);
                Grid.SetColumn(appNameLabel, 0);

                // App Size
                var appSizeLabel = new TextBlock
                {
                    Text = app.FormattedSize,
                    FontSize = 12,
                    Foreground = new SolidColorBrush(0xFF888888),
                    Margin = new Thickness(0, 0, 0, 5)
                };
                Grid.SetRow(appSizeLabel, 1);
                Grid.SetColumn(appSizeLabel, 0);

                // Publisher
                if (!string.IsNullOrEmpty(app.Publisher))
                {
                    var publisherLabel = new TextBlock
                    {
                        Text = $"Publisher: {app.Publisher}",
                        FontSize = 12,
                        FontStyle = FontStyle.Italic,
                        Foreground = new SolidColorBrush(0xFF888888)
                    };
                    Grid.SetRow(publisherLabel, 2);
                    Grid.SetColumn(publisherLabel, 0);
                    mainGrid.Children.Add(publisherLabel);
                }

                // Uninstall Button
                var uninstallButton = new Button
                {
                    Content = "Uninstall",
                    Classes = { "Secondary" },
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(10, 0, 0, 0),
                    MinWidth = 100
                };
                Grid.SetRowSpan(uninstallButton, 3);
                Grid.SetColumn(uninstallButton, 1);

                uninstallButton.Click += async (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(app.UninstallString))
                    {
                        ConfirmPopup.Show(
                            $"Are you sure you want to uninstall {app.Name}?",
                            "This action cannot be undone."
                        );

                        ConfirmPopup.Confirmed += async (s, args) =>
                        {
                            try
                            {
                                var psi = new ProcessStartInfo
                                {
                                    FileName = app.UninstallString,
                                    UseShellExecute = true,
                                    Verb = "runas"
                                };
                                Process.Start(psi);
                                await _toastService.ShowSuccess($"{app.Name} uninstalled successfully", "Uninstall Success");
                            }
                            catch (System.Exception ex)
                            {
                                await _toastService.ShowError($"Error uninstalling {app.Name}: {ex.Message}", "Uninstall Error");
                            }
                        };

                        ConfirmPopup.Canceled += (s, args) =>
                        {
                            _toastService.ShowInfo($"Uninstallation of {app.Name} canceled", "Uninstall Canceled");
                        };
                    }
                    else
                    {
                        await _toastService.ShowError($"Uninstall string for {app.Name} is not available", "Uninstall Error");
                    }
                };

                mainGrid.Children.Add(appNameLabel);
                mainGrid.Children.Add(appSizeLabel);
                mainGrid.Children.Add(uninstallButton);

                cardBorder.Child = mainGrid;
                applicationsPanel.Children.Add(cardBorder);
            }
        }
    }
}