using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace VoltAir_Setup.ViewModels;

public class InstallationViewModel : INotifyPropertyChanged
{
    private bool _isInstalling;
    public bool IsInstalling
    {
        get => _isInstalling;
        set => SetField(ref _isInstalling, value);
    }

    private bool _isComplete;
    public bool IsComplete
    {
        get => _isComplete;
        set => SetField(ref _isComplete, value);
    }

    private int _progress;
    public int Progress
    {
        get => _progress;
        set => SetField(ref _progress, value);
    }

    private string _statusText = "Ready to install...";
    public string StatusText
    {
        get => _statusText;
        set => SetField(ref _statusText, value);
    }

    private bool _createDesktopShortcut = true;
    public bool CreateDesktopShortcut
    {
        get => _createDesktopShortcut;
        set => SetField(ref _createDesktopShortcut, value);
    }

    private bool _createStartMenuShortcut = true;
    public bool CreateStartMenuShortcut
    {
        get => _createStartMenuShortcut;
        set => SetField(ref _createStartMenuShortcut, value);
    }

    private bool _startAtBoot = false;
    public bool StartAtBoot
    {
        get => _startAtBoot;
        set => SetField(ref _startAtBoot, value);
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
    {
        if (Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}