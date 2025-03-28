using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using P3___Networked_Producer.Models;
using P3___Networked_Producer.Views;

namespace P3___Networked_Producer.ViewModels
{
    public partial class UploadViewModel : ObservableObject
    {
        private readonly HashSet<string> videoPaths = new HashSet<string>();
        private readonly HashSet<string> allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { ".mp4", ".mkv", ".avi", ".mov", ".wmv" };

        public ObservableCollection<VideoFileItem> VideoFiles { get; } = new ObservableCollection<VideoFileItem>();

        public UploadViewModel() {}

        public bool CanAcceptDrag(System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var paths = (string[])e.Data.GetData(DataFormats.FileDrop);
                // Accept only if every path is a file with an allowed video extension.
                return paths.All(path => File.Exists(path) && allowedExtensions.Contains(Path.GetExtension(path)));
            }
            return false;
        }
        public void HandleFileDrop(System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] paths = (string[])e.Data.GetData(DataFormats.FileDrop);
                HandleDrop(paths);
                UploadVideosCommand.NotifyCanExecuteChanged();
            }
        }

        public void HandleDrop(string[] paths)
        {
            var acceptedVideos = new List<string>();
            var rejectedVideos = new List<string>();

            foreach (string path in paths)
            {
                if (File.Exists(path))
                {
                    if (allowedExtensions.Contains(Path.GetExtension(path)))
                    {
                        if (videoPaths.Add(path))
                        {
                            acceptedVideos.Add(Path.GetFileName(path));
                            VideoFiles.Add(new VideoFileItem { FilePath = path });
                        }
                    }
                    else
                    {
                        rejectedVideos.Add(Path.GetFileName(path));
                    }
                }
                else
                {
                    rejectedVideos.Add(path);
                }
            }

            if (acceptedVideos.Count > 0)
            {
                MessageBox.Show($"{acceptedVideos.Count} video(s) added.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            if (rejectedVideos.Count > 0)
            {
                MessageBox.Show($"Only video files are accepted!\nRejected files:\n{string.Join("\n", rejectedVideos)}",
                                "Invalid Files", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private bool CanUpload() => videoPaths.Count > 0;

        [RelayCommand(CanExecute = nameof(CanUpload))]
        private void UploadVideos()
        {
            MessageBox.Show($"Uploading {videoPaths.Count} video(s)...", "Upload", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        [RelayCommand]
        private void NavigateToProgress()
        {
            if (Application.Current.MainWindow is MainWindow mainWindow)
            {
                _ = mainWindow.MainFrame.Navigate(new ProgressPage());
            }
            else
            {
                MessageBox.Show("Main window is not available.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void DeleteVideo(VideoFileItem video)
        {
            if (video != null)
            {
                videoPaths.Remove(video.FilePath);
                VideoFiles.Remove(video);
                UploadVideosCommand.NotifyCanExecuteChanged();
            }
        }
    }
}
