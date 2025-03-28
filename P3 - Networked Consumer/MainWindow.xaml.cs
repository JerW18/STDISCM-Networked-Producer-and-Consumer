using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace P3___Networked_Consumer
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
            string fileName = System.IO.Path.GetFileName(videoPath);

            // Create a container for the video and filename
            StackPanel videoStack = new StackPanel
            {
                Width = 200,
                Margin = new Thickness(10),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            Grid videoContainer = new Grid
            {
                Width = 200,
                Height = 120
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

            TextBlock fileNameText = new TextBlock
            {
                Text = fileName,
                FontSize = 12,
                Foreground = Brushes.Black, // Ensure visibility
                TextWrapping = TextWrapping.Wrap, // Prevent text cutoff
                TextAlignment = TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 5, 0, 0)
            };

            videoContainer.Children.Add(thumbnail);
            videoContainer.Children.Add(videoPreview);

            // Event: Mouse Hover to Preview Video
            videoContainer.MouseEnter += (s, e) =>
            {
                thumbnail.Visibility = Visibility.Collapsed;
                videoPreview.Visibility = Visibility.Visible;
                videoPreview.Position = TimeSpan.Zero;
                videoPreview.Play();

                playbackTimer.Stop();
                playbackTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(10)
                };
                playbackTimer.Tick += (sender, args) =>
                {
                    videoPreview.Stop();
                    videoPreview.Visibility = Visibility.Collapsed;
                    thumbnail.Visibility = Visibility.Visible;
                    playbackTimer.Stop();
                };
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

            // Add video container and filename to stack
            videoStack.Children.Add(videoContainer);
            videoStack.Children.Add(fileNameText);

            // Add the stack to the gallery
            VideoGallery.Children.Add(videoStack);
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
