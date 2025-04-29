using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Interactivity;
using System;
using System.Diagnostics;

namespace VoltAir.Views.Components
{
    public partial class ProgressDialog : UserControl
    {
        private long _totalBytesToDelete = 0;
        private long _totalBytesDeleted = 0;
        private int _filesCount = 0;
        private int _filesDeleted = 0;
        private Stopwatch _progressStopwatch = new Stopwatch();
        private bool _operationCancelled = false;

        public ProgressDialog()
        {
            InitializeComponent();
            
            // Initially hide the dialog
            this.IsVisible = false;
        }
        
        public void ShowProgress(string title, string description, long totalBytesToDelete, int filesCount)
        {
            if (ProgressTitle == null || ProgressDescription == null || 
                DeletionProgressBar == null || ProgressPercentText == null || 
                EstimatedTimeText == null)
            {
                Debug.WriteLine("Error: UI elements not initialized properly");
                return;
            }

            ProgressTitle.Text = title;
            ProgressDescription.Text = description;
            _totalBytesToDelete = totalBytesToDelete;
            _filesCount = filesCount;
            _totalBytesDeleted = 0;
            _filesDeleted = 0;
            _operationCancelled = false;

            this.IsVisible = true;
            DeletionProgressBar.Value = 0;
            ProgressPercentText.Text = "0%";
            EstimatedTimeText.Text = "Estimating time...";

            _progressStopwatch.Restart();
        }

        public void HideProgress()
        {
            this.IsVisible = false;
            _progressStopwatch.Stop();
        }

        public void UpdateProgress(long bytesDeleted, int filesDeleted)
        {
            // Guard clauses to prevent null reference exceptions
            if (_totalBytesToDelete <= 0 || DeletionProgressBar == null || 
                ProgressPercentText == null || EstimatedTimeText == null || 
                ProgressDescription == null) 
                return;

            _totalBytesDeleted += bytesDeleted;
            _filesDeleted += filesDeleted;

            double progressPercentage = (double)_totalBytesDeleted / _totalBytesToDelete * 100;

            DeletionProgressBar.Value = progressPercentage;
            ProgressPercentText.Text = $"{progressPercentage:F1}%";

            if (_progressStopwatch.Elapsed.TotalSeconds > 0 && progressPercentage > 0)
            {
                double bytesPerSecond = _totalBytesDeleted / _progressStopwatch.Elapsed.TotalSeconds;
                long bytesRemaining = _totalBytesToDelete - _totalBytesDeleted;

                if (bytesPerSecond > 0)
                {
                    double secondsRemaining = bytesRemaining / bytesPerSecond;
                    TimeSpan timeRemaining = TimeSpan.FromSeconds(secondsRemaining);

                    string timeText;
                    if (timeRemaining.TotalHours >= 1)
                        timeText = $"{timeRemaining.Hours}h {timeRemaining.Minutes}m remaining";
                    else if (timeRemaining.TotalMinutes >= 1)
                        timeText = $"{timeRemaining.Minutes}m {timeRemaining.Seconds}s remaining";
                    else
                        timeText = $"{timeRemaining.Seconds}s remaining";

                    EstimatedTimeText.Text = timeText;
                    ProgressDescription.Text = $"Deleted {_filesDeleted} of {_filesCount} files ({FormatSize(_totalBytesDeleted)} of {FormatSize(_totalBytesToDelete)})";
                }
            }
        }

        private string FormatSize(long bytes)
        {
            if (bytes <= 0) return "0 B";
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            double len = bytes;

            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }

            return $"{len:0.##} {sizes[order]}";
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            CancelDeletion();
        }

        public bool IsOperationCancelled()
        {
            return _operationCancelled;
        }
        
        public void CancelDeletion()
        {
            _operationCancelled = true;
            HideProgress();
        }
    }
}