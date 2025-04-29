using System;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace VoltAir.Views.Pages.SettingsView;

public partial class P3 : UserControl
{
    public P3()
    {
        InitializeComponent();
    }

    private void Website_OnClick(object? sender, RoutedEventArgs e)
    {
        var url = "https://voltair.pages.dev";
        try
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            };
            Process.Start(psi);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }
}