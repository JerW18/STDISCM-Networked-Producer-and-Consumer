using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace VideoUploader
{
    public partial class MainWindow : Window
    {
        private string saveFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "UploadedVideos");
        private List<string> uploadedVideos = new List<string>();
        private DispatcherTimer playbackTimer;

        public MainWindow()
        {
            InitializeComponent();

            // Ensure save directory exists
            if (!Directory.Exists(saveFolder))
                Directory.CreateDirectory(saveFolder);

            // Timer to stop video playback after 10 seconds
            playbackTimer = new DispatcherTimer();
            playbackTimer.Interval = TimeSpan.FromSeconds(10);
            playbackTimer.Tick += (sender, e) => StopPlayback();
        }

        private void UploadButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Multiselect = true,
                Filter = "Video Files|*.mp4;*.avi;*.mov;*.wmv;*.mkv"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                foreach (string filePath in openFileDialog.FileNames)
                {
                    string destinationPath = Path.Combine(saveFolder, Path.GetFileName(filePath));

                    try
                    {
                        File.Copy(filePath, destinationPath, true);
                        uploadedVideos.Add(destinationPath);
                        AddVideoToGallery(destinationPath);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error saving file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void AddVideoToGallery(string videoPath)
        {
            Grid videoContainer = new Grid
            {
                Width = 200,
                Height = 120,
                Margin = new Thickness(10)
            };

            Image thumbnail = new Image
            {
                Source = new BitmapImage(new Uri("https://www.iconpacks.net/icons/1/free-video-icon-833-thumb.png")),
                Stretch = Stretch.Uniform,
                Opacity = 1
            };

            MediaElement videoPreview = new MediaElement
            {
                Source = new Uri(videoPath),
                Width = 200,
                Height = 120,
                LoadedBehavior = MediaState.Manual,
                Visibility = Visibility.Collapsed // Hidden until hovered
            };

            videoContainer.Children.Add(thumbnail);
            videoContainer.Children.Add(videoPreview);

            // Event: Mouse Hover to Preview Video
            videoContainer.MouseEnter += (s, e) =>
            {
                thumbnail.Visibility = Visibility.Collapsed;
                videoPreview.Visibility = Visibility.Visible;
                videoPreview.Play();
                playbackTimer.Start();
            };

            // Event: Mouse Leave to Hide Preview
            videoContainer.MouseLeave += (s, e) =>
            {
                videoPreview.Visibility = Visibility.Collapsed;
                thumbnail.Visibility = Visibility.Visible;
                videoPreview.Stop();
                playbackTimer.Stop();
            };

            VideoGallery.Children.Add(videoContainer);
        }

        private void StopPlayback()
        {
            foreach (var child in VideoGallery.Children)
            {
                if (child is Grid grid && grid.Children[1] is MediaElement video)
                {
                    video.Stop();
                }
            }
            playbackTimer.Stop();
        }
    }
}
