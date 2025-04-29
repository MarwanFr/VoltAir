using System.Threading.Tasks;
using Avalonia.Controls;
using System.Threading;
using VoltAir.Views.Components;
using Avalonia.Threading;

public class ToastService
{
    private readonly Panel _containerPanel;
    private ToastNotification _activeToast;
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
    private const int ToastDelay = 1000; // 1 second delay between toasts

    public ToastService(Panel containerPanel)
    {
        _containerPanel = containerPanel;
    }

    public async Task ShowToast(string message, string title = null, ToastType type = ToastType.Error,
        bool autoClose = true, int autoCloseDelay = 5000)
    {
        await _semaphore.WaitAsync();
        try
        {
            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                // Close any existing toast
                if (_activeToast != null && _containerPanel.Children.Contains(_activeToast))
                {
                    await _activeToast.HideToast();
                    _containerPanel.Children.Remove(_activeToast);
                }

                // Create and show new toast
                _activeToast = new ToastNotification();
                _containerPanel.Children.Add(_activeToast);

                await _activeToast.ShowToast(message, title, type, autoClose, autoCloseDelay);

                // Remove from container after animation
                _containerPanel.Children.Remove(_activeToast);
                _activeToast = null;

                // Introduce a delay before releasing the semaphore
                await Task.Delay(ToastDelay);
            });
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task ShowError(string message, string title = "Error", bool autoClose = true)
    {
        await ShowToast(message, title, ToastType.Error, autoClose);
    }

    public async Task ShowSuccess(string message, string title = "Success", bool autoClose = true)
    {
        await ShowToast(message, title, ToastType.Success, autoClose);
    }

    public async Task ShowInfo(string message, string title = "Information", bool autoClose = true)
    {
        await ShowToast(message, title, ToastType.Info, autoClose);
    }

    public async Task ShowWarning(string message, string title = "Warning", bool autoClose = true)
    {
        await ShowToast(message, title, ToastType.Warning, autoClose);
    }
}
