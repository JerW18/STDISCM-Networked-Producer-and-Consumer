using System.Collections.ObjectModel;
using System.IO;
using System.Net.Sockets;
using System.Net;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using P3___Networked_Producer.Models;
using P3___Networked_Producer.Views;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;

namespace P3___Networked_Producer.ViewModels
{
    public partial class UploadViewModel : ObservableObject
    {
        private ProgressPage? progressPage;

        private readonly HashSet<string> videoPaths = [];
        private readonly HashSet<string> allowedExtensions = new(StringComparer.OrdinalIgnoreCase)
            { ".mp4", ".mkv", ".avi", ".mov", ".wmv" };

        public ObservableCollection<VideoFileItem> VideoFiles { get; } = [];

        private static SemaphoreSlim acceptSemaphore;

        [ObservableProperty]
        private int threadCount = 4;

        public UploadViewModel() {}

        public bool CanAcceptDrag(System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var paths = (string[])e.Data.GetData(DataFormats.FileDrop);

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
                if (!File.Exists(path))
                    continue;

                string extension = Path.GetExtension(path);
                if (allowedExtensions.Contains(extension))
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

            if (rejectedVideos.Count > 0)
            {
                MessageBox.Show($"Only video files are accepted!\nRejected files:\n{string.Join("\n", rejectedVideos)}",
                                "Invalid Files", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private bool CanUpload() => videoPaths.Count > 0;

        [RelayCommand]
        private void NavigateToProgress()
        {
            if (Application.Current.MainWindow is MainWindow mainWindow)
            {
                progressPage ??= new ProgressPage();
                _ = mainWindow.MainFrame.Navigate(progressPage);
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

        [RelayCommand(CanExecute = nameof(CanUpload))]
        private async Task UploadVideosAsync()
        {
            int port = 5001;
            string localIP = GetLocalIPAddress();

            TcpListener server = new(IPAddress.Any, port);
            server.Start();

            MessageBox.Show($"Server started on {localIP}:{port}.",
                            "Server Started", MessageBoxButton.OK, MessageBoxImage.Information);

            await Task.Run(() =>
            {
                using TcpClient client = server.AcceptTcpClient();
                using NetworkStream stream = client.GetStream();
                using BinaryReader reader = new(stream);

                int semaphoreCount = reader.ReadInt32();
                if (semaphoreCount > ThreadCount) semaphoreCount = ThreadCount;
                acceptSemaphore = new(semaphoreCount, semaphoreCount);

                int newThreadCount = Math.Min(ThreadCount, videoPaths.Count);
                int videosPerThread = videoPaths.Count / newThreadCount;
                int remainingVideos = videoPaths.Count % newThreadCount;
                var videoPathList = videoPaths.ToList();
                int startIndex = 0;

                List<Thread> threads = [];
                for (int i = 0; i < newThreadCount; i++)
                {
                    int count = videosPerThread + (i < remainingVideos ? 1 : 0);
                    var videoSubset = videoPathList.GetRange(startIndex, count);
                    startIndex += count;

                    Thread thread = new(() => HandleClientConnections(server, videoSubset));
                    thread.Start();
                    threads.Add(thread);
                }

                foreach (var thread in threads)
                {
                    thread.Join();
                }

                server.Stop();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    videoPaths.Clear();
                    VideoFiles.Clear();
                });

                MessageBox.Show("Server stopped listening for clients.",
                                "Upload Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            });
        }

        private static string GetLocalIPAddress()
        {
            foreach (NetworkInterface networkInterface in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (networkInterface.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 && networkInterface.OperationalStatus == OperationalStatus.Up)
                {
                    foreach (UnicastIPAddressInformation ip in networkInterface.GetIPProperties().UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            return ip.Address.ToString();
                        }
                    }
                }
            }
            return "null";
        }

        private static void HandleClientConnections(TcpListener server, List<string> videoSubset)
        {
            bool isRunning = true;

            while (isRunning)
            {
                acceptSemaphore.Wait();
                try
                {
                    TcpClient client = server.AcceptTcpClient();
                    SendVideosToClient(client, videoSubset);
                    isRunning = false; 
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error sending videos to client: {ex.Message}. Retrying...");
                }
                finally
                {
                    acceptSemaphore.Release();
                }
            }
        }

        private static void SendVideosToClient(TcpClient client, List<string> videoSubset)
        {
            try
            {
                using NetworkStream stream = client.GetStream();
                using BinaryWriter writer = new(stream);
                using BinaryReader reader = new(stream);

                // Send the number of video files.
                writer.Write(videoSubset.Count);
                writer.Flush();

                foreach (string path in videoSubset)
                {
                    // Get Filename
                    string fileName = Path.GetFileName(path);
                    byte[] fileNameBytes = System.Text.Encoding.UTF8.GetBytes(fileName);

                    // Write Filename
                    writer.Write(fileNameBytes.Length);
                    writer.Write(fileNameBytes);

                    // Hash Video File
                    byte[] fileData = File.ReadAllBytes(path);
                    byte[] hashBytes = SHA256.HashData(fileData);

                    writer.Write(hashBytes.Length);
                    writer.Write(hashBytes);
                    writer.Flush();

                    // 0 = already exists, 1 = send file
                    int clientResponse = reader.ReadInt32();
                    if (clientResponse == 0)
                    {
                        Trace.WriteLine($"Skipping {fileName}, already exists on client.");
                        continue;
                    }

                    // Send the file if it does not exist on the client
                    writer.Write(fileData.Length);
                    stream.Write(fileData, 0, fileData.Length);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error sending videos to client: {ex.Message}");
            }
        }
    }
}
