using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Voltair_Uninstall.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private string _statusText = "Uninstall Voltair? / Désinstaller Voltair ?";
        public string StatusText
        {
            get => _statusText;
            set
            {
                if (_statusText != value)
                {
                    _statusText = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}