﻿using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using static P3___Networked_Consumer.ConsumerQueue;

namespace P3___Networked_Consumer
{
    public partial class MainWindow : Window
    {
        private string saveFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "UploadedVideos");
        private DispatcherTimer playbackTimer;
        private bool isConsuming = true;

        public MainWindow()
        {
            InitializeComponent();
            IpAddressText.Text = $"Server IP: {GetLocalIPAddress()}";

            Logger.LogMsg = LogToUI;

            if (!Directory.Exists(saveFolder))
                Directory.CreateDirectory(saveFolder);

            playbackTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(10)
            };
            playbackTimer.Tick += (s, e) => StopPlayback();

            DownloadConsumer();
        }

        private string GetLocalIPAddress()
        {
            foreach (var ni in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up)
                {
                    foreach (var ip in ni.GetIPProperties().UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                            return ip.Address.ToString();
                    }
                }
            }

            return "Unavailable";
        }

        private void AddVideoToGallery(string videoPath)
        {
            string fileName = System.IO.Path.GetFileName(videoPath);

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
                Source = new BitmapImage(new Uri("/Assets/video-thumbnail.png", UriKind.Relative)),
                Stretch = Stretch.Uniform
            };

            MediaElement videoPreview = new MediaElement
            {
                Source = new Uri(videoPath),
                Width = 200,
                Height = 120,
                LoadedBehavior = MediaState.Manual,
                Visibility = Visibility.Collapsed
            };

            videoContainer.Children.Add(thumbnail);
            videoContainer.Children.Add(videoPreview);

            videoContainer.MouseEnter += (s, e) =>
            {
                thumbnail.Visibility = Visibility.Collapsed;
                videoPreview.Visibility = Visibility.Visible;
                videoPreview.Position = TimeSpan.Zero;
                videoPreview.Play();

                playbackTimer.Stop();
                playbackTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(10) };
                playbackTimer.Tick += (sender, args) =>
                {
                    videoPreview.Stop();
                    videoPreview.Visibility = Visibility.Collapsed;
                    thumbnail.Visibility = Visibility.Visible;
                    playbackTimer.Stop();
                };
                playbackTimer.Start();
            };

            videoContainer.MouseLeave += (s, e) =>
            {
                videoPreview.Visibility = Visibility.Collapsed;
                thumbnail.Visibility = Visibility.Visible;
                videoPreview.Stop();
                playbackTimer.Stop();
            };

            videoContainer.MouseLeftButtonUp += (s, e) =>
            {
                var player = new MediaElement
                {
                    Source = new Uri(videoPath),
                    LoadedBehavior = MediaState.Manual,
                    UnloadedBehavior = MediaState.Manual,
                    Stretch = Stretch.Uniform,
                    Volume = 0.5
                };

                var playButton = new Button { Content = "▶", Width = 50, Margin = new Thickness(5, 0, 0, 0) };
                var pauseButton = new Button { Content = "❚❚", Width = 50, Margin = new Thickness(5, 0, 0, 0) };
                var stopButton = new Button { Content = "■", Width = 50, Margin = new Thickness(5, 0, 0, 0) };

                var volumeLabel = new TextBlock { Text = "Vol:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10, 0, 0, 0), Foreground = Brushes.White };
                var volumeSlider = new Slider
                {
                    Width = 70,
                    Minimum = 0,
                    Maximum = 1,
                    Value = player.Volume,
                    VerticalAlignment = VerticalAlignment.Center
                };

                var timeLabel = new TextBlock { Text = "00:00 / 00:00", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10, 0, 5, 0), Foreground = Brushes.White };
                var timelineSlider = new Slider
                {
                    MinWidth = 150,
                    VerticalAlignment = VerticalAlignment.Center,
                    IsEnabled = false 
                };

                var controlsPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Center,
                    Background = new SolidColorBrush(Color.FromArgb(180, 0, 0, 0)),
                    Height = 40
                };
                controlsPanel.Children.Add(playButton);
                controlsPanel.Children.Add(pauseButton);
                controlsPanel.Children.Add(stopButton);
                controlsPanel.Children.Add(volumeLabel);
                controlsPanel.Children.Add(volumeSlider);
                controlsPanel.Children.Add(timeLabel);
                controlsPanel.Children.Add(timelineSlider);

                var mainGrid = new Grid();
                mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); 
                                                                                            

                Grid.SetRow(player, 0);
                mainGrid.Children.Add(player);

                Grid.SetRow(controlsPanel, 1);
                mainGrid.Children.Add(controlsPanel);

                Window playerWindow = new Window
                {
                    Width = 800,
                    Height = 450,
                    Title = fileName,
                    WindowStyle = WindowStyle.SingleBorderWindow, 
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    Content = mainGrid 
                };

                DispatcherTimer timer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(200)
                };
                bool isDragging = false; 

                player.MediaOpened += (sender, args) =>
                {
                    if (player.NaturalDuration.HasTimeSpan)
                    {
                        var totalDuration = player.NaturalDuration.TimeSpan;
                        timelineSlider.Maximum = totalDuration.TotalSeconds;
                        timeLabel.Text = $"00:00 / {totalDuration:mm\\:ss}"; 
                        timelineSlider.IsEnabled = true; 
                        timer.Start(); 
                    }
                    player.Play();
                };

                player.MediaEnded += (sender, args) =>
                {
                    player.Stop();
                    timelineSlider.Value = 0; 
                    timer.Stop(); 
                };

                player.MediaFailed += (sender, args) =>
                {
                    MessageBox.Show($"Media failed to load or play: {args.ErrorException.Message}", "Media Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    timer.Stop();
                    timelineSlider.IsEnabled = false;
                };

                timer.Tick += (sender, args) =>
                {
                    if (!isDragging && player.NaturalDuration.HasTimeSpan) 
                    {
                        timelineSlider.Value = player.Position.TotalSeconds;
                        timeLabel.Text = $"{player.Position:mm\\:ss} / {player.NaturalDuration.TimeSpan:mm\\:ss}";
                    }
                    else if (player.NaturalDuration.HasTimeSpan)
                    {
                        timeLabel.Text = $"{TimeSpan.FromSeconds(timelineSlider.Value):mm\\:ss} / {player.NaturalDuration.TimeSpan:mm\\:ss}";
                    }
                };

                playButton.Click += (sender, args) =>
                {
                    player.Play();
                    if (player.NaturalDuration.HasTimeSpan) timer.Start(); 
                };
                pauseButton.Click += (sender, args) =>
                {
                    player.Pause();
                    timer.Stop(); 
                };
                stopButton.Click += (sender, args) =>
                {
                    player.Stop(); 
                    timelineSlider.Value = 0; 
                    timer.Stop(); 
                    if (player.NaturalDuration.HasTimeSpan) 
                        timeLabel.Text = $"00:00 / {player.NaturalDuration.TimeSpan:mm\\:ss}";
                    else
                        timeLabel.Text = "00:00 / 00:00"; 
                };

                volumeSlider.ValueChanged += (sender, args) =>
                {
                    player.Volume = volumeSlider.Value;
                };

                timelineSlider.AddHandler(Thumb.DragStartedEvent, new DragStartedEventHandler((sender, args) =>
                {
                    isDragging = true;
                    if(player.CanPause) player.Pause();
                    timer.Stop();
                }));

                timelineSlider.AddHandler(Thumb.DragCompletedEvent, new DragCompletedEventHandler((sender, args) =>
                {
                    isDragging = false;
                    player.Position = TimeSpan.FromSeconds(timelineSlider.Value);
                    player.Play();
                    timer.Start();
                }));

                playerWindow.Closed += (sender, args) =>
                {
                    timer?.Stop();
                    player.Stop();
                    player.Close();
                };

                playerWindow.ShowDialog(); 

                timer?.Stop();
            };

            TextBlock fileNameText = new TextBlock
            {
                Text = fileName,
                FontSize = 12,
                Foreground = Brushes.Black,
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 5, 0, 0)
            };
            
            videoStack.Children.Add(videoContainer);
            videoStack.Children.Add(fileNameText);
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

        private void ConfigStart()
        {
            if (!int.TryParse(qInput.Text, out int queueSize))
                queueSize = 10;

            if (!int.TryParse(cInput.Text, out int consumerThreads))
                consumerThreads = 2;

            ConsumerQueue.InitializeQueue(queueSize);
            StartServer();
            StartConsumerThreads(consumerThreads);

            Logger.Log("Server started.");
        }

        private void ClickStart(object sender, RoutedEventArgs e)
        {
            ConfigStart();
            startBtn.IsEnabled = false;
            startBtn.Content = "Running";

            qInput.IsEnabled = false;
            cInput.IsEnabled = false;
        }
        private void StartServer()
        {
            Thread serverThread = new Thread(() =>
            {
                try
                {
                    TcpListener listener = new(IPAddress.Any, 5001);
                    listener.Start();

                    Logger.Log("[Server] Listening on port 5001");

                    while (isConsuming)
                    {
                        TcpClient client = listener.AcceptTcpClient();
                        Logger.Log("[Server] Producer connected");

                        Thread handlerThread = new Thread(() => HandleProducer(client));
                        handlerThread.IsBackground = true;
                        handlerThread.Start();
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log($"[Server] Error: {ex.Message}");
                }
            });

            serverThread.IsBackground=true;
            serverThread.Start();
        }

        private void StartConsumerThreads(int count)
        {
            for (int i = 0; i < count; i++)
            {
                Thread consumerThread = new(() => DownloadConsumer());
                consumerThread.IsBackground = true;
                consumerThread.Start();
            }
        }

        private void DownloadConsumer()
        {
            Thread consumerThread = new Thread(() =>
            {
                while (isConsuming)
                {
                    if (P3___Networked_Consumer.ConsumerQueue.TryDequeue(out var item))
                    {
                        string savePath = Path.Combine(saveFolder, item.FileName);
                        bool saved = false;

                        try
                        {
                            File.WriteAllBytes(savePath, item.FileData);
                            saved = true;


                            //Thread.Sleep(10000); //test queue
                            Dispatcher.Invoke(() =>
                            {
                                AddVideoToGallery(savePath);
                            });
                            Logger.Log($"[Consumer] downloaded + displayed: {item.FileName}");
                        }
                        catch (Exception ex)
                        {
                            Debug.Write($"[Consumer] Failed to save {item.FileName}: {ex.Message}");
                        }
                        
                        if(!saved)
                        {
                            P3___Networked_Consumer.ConsumerQueue.TryRequeue(item);
                            Thread.Sleep(1000);
                        }
                    }
                    else
                    {
                        Thread.Sleep(1000);
                    }
                }
            });

            consumerThread.IsBackground = true;
            consumerThread.Start();
        }

        private void HandleProducer(TcpClient client)
        {
            try
            {
                using NetworkStream stream = client.GetStream();
                using BinaryReader reader = new(stream);
                using BinaryWriter writer = new(stream);

                int fileCount = reader.ReadInt32();

                for (int i = 0; i < fileCount; i++)
                {
                    int fileNameLen = reader.ReadInt32();
                    string fileName = Encoding.UTF8.GetString(reader.ReadBytes(fileNameLen));

                    int hashLen = reader.ReadInt32();
                    byte[] hash = reader.ReadBytes(hashLen);

                    // Only check first
                    if (ConsumerQueue.IsDuplicate(hash))
                    {
                        Logger.Log($"[Server] Rejected (duplicate): {fileName}");
                        writer.Write(0); // Duplicate
                        continue;
                    }

                    if (!ConsumerQueue.GetSlot())
                    {
                        Logger.Log($"[Server] Rejected (queue full): {fileName}");
                        writer.Write(2); 
                        continue;
                    }

                    writer.Write(1);

                    int fileSize = reader.ReadInt32();
                    byte[] fileData = reader.ReadBytes(fileSize);

                    ConsumerQueue.FinishEnqueue(fileName, fileData, hash); 
                    Logger.Log($"[Server] Enqueued: {fileName}");
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[Server] Upload error: {ex.Message}");
            }
            finally
            {
                client.Close();
            }
        }

        public void LogToUI(string message)
        {
            Dispatcher.Invoke(() =>
            {
                ServerLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
                ServerLog.ScrollToEnd();
            });
        }
    }


}
