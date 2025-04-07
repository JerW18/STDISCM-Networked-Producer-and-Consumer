using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using P3___Networked_Producer.Models;

namespace P3___Networked_Producer.ViewModels
{
    public partial class UploadViewModel : ObservableObject
    {
        private readonly HashSet<string> folderPaths = [];
        private readonly HashSet<string> allowedExtensions = new(StringComparer.OrdinalIgnoreCase)
            { ".mp4", ".mkv", ".avi", ".mov", ".wmv" };

        public ObservableCollection<FolderItem> FoldersToUpload { get; } = [];

        [ObservableProperty]
        private int threadCount = 4;
        public int compressionIndex{ get; set; } = 1;

        [ObservableProperty]
        private string consumerIP = "127.0.0.1";
        partial void OnConsumerIPChanged(string value)
        {
            UploadVideosCommand.NotifyCanExecuteChanged();
        }

        private bool isUploading = false;

        public bool CanAcceptDrag(System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                var paths = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);
                if (paths == null || paths.Length == 0) return false;
                return paths.All(path => Directory.Exists(path));
            }
            return false;
        }

        public void HandleFileDrop(System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                string[] paths = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);
                HandleDrop(paths);
                UploadVideosCommand.NotifyCanExecuteChanged();
            }
        }

        public void HandleDrop(string[] paths)
        {
            var acceptedFoldersForMessage = new List<string>();
            var rejectedFoldersForMessage = new List<string>();
            var duplicateFolders = new List<string>();

            foreach (string path in paths)
            {
                if (Directory.Exists(path))
                {
                    if (IsValidVideoFolder(path))
                    {
                        if (folderPaths.Add(path))
                        {
                            acceptedFoldersForMessage.Add(Path.GetFileName(path));
                            FoldersToUpload.Add(new FolderItem { FolderPath = path });
                        }
                        else
                        {
                            duplicateFolders.Add(Path.GetFileName(path));
                        }
                    }
                    else
                    {
                        rejectedFoldersForMessage.Add(Path.GetFileName(path));
                    }
                }
            }

            if (rejectedFoldersForMessage.Count > 0)
            {
                MessageBox.Show($"Only folders that are empty or contain exclusively supported video files ({string.Join(", ", allowedExtensions)}) are accepted.\n\nRejected folders (contain other file types or folders):\n{string.Join("\n", rejectedFoldersForMessage)}",
                                "Invalid Folder Content", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private bool IsValidVideoFolder(string directoryPath)
        {
            if (Directory.EnumerateDirectories(directoryPath).Any())
            {
                return false;
            }

            try
            {
                var files = Directory.EnumerateFiles(directoryPath, "*.*", SearchOption.AllDirectories);

                bool allAreVideos = files.All(file => allowedExtensions.Contains(Path.GetExtension(file)));
                return allAreVideos;

            }
            catch (UnauthorizedAccessException uaEx)
            {
                return false;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private bool CanUpload()
        {
            if (isUploading || folderPaths.Count == 0)
                return false;

            // IP format check
            return System.Net.IPAddress.TryParse(ConsumerIP, out _);
        }

        [RelayCommand(CanExecute = nameof(CanUpload))]
        private async Task UploadVideosAsync()
        {
            if (!System.Net.IPAddress.TryParse(ConsumerIP, out _))
            {
                System.Windows.MessageBox.Show("Please enter a valid IP address.", "Invalid IP", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            if (!CheckOnline(ConsumerIP, 5001))
            {
                MessageBox.Show("Failed to connect to server.",
                                "Connection Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            isUploading = true;
            UploadVideosCommand.NotifyCanExecuteChanged();

            var folderList = folderPaths.ToList();
            int total = folderList.Count;
            int perThread = total / ThreadCount;
            int remainder = total % ThreadCount;

            int startIndex = 0;
            List<Thread> threads = [];

            for (int i = 0; i < ThreadCount; i++)
            {
                int count = perThread + (i < remainder ? 1 : 0);
                var subset = folderList.GetRange(startIndex, count);
                startIndex += count;

                var videoList = getVideoList(subset);

                Thread thread = new(() => UploadVideo(videoList));
                thread.Start();
                threads.Add(thread);
            }

            await Task.Run(() => threads.ForEach(t => t.Join()));

            isUploading = false;
            UploadVideosCommand.NotifyCanExecuteChanged();

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                folderPaths.Clear();
                FoldersToUpload.Clear();
            });

            Logger.Log("[Producer] Upload complete.");
        }

        private List<string> getVideoList(List<string> folderPaths)
        {
            var videoList = new List<string>();
            try
            {
                foreach (string folderPath in folderPaths)
                {
                    var files = Directory.EnumerateFiles(folderPath, "*.*", SearchOption.AllDirectories)
                        .Where(file => allowedExtensions.Contains(Path.GetExtension(file)));
                    videoList.AddRange(files);
                }
            }
            catch (UnauthorizedAccessException uaEx)
            {
                Logger.Log($"[Producer] Access denied: {uaEx.Message}");
            }
            catch (Exception ex)
            {
                Logger.Log($"[Producer] Error: {ex.Message}");
            }
            return videoList;
        }

        private bool CheckOnline(string ip, int port)
        {
            try
            {
                using var client = new TcpClient();
                var result = client.BeginConnect(ip, port, null, null);
                bool success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(1));
                return success && client.Connected;
            }
            catch
            {
                return false;
            }
        }

        private void UploadVideo(List<string> videoSubset)
        {
            List<string> retryList = new();

            try
            {
                using TcpClient client = new();
                Logger.Log($"[Producer] Connecting to consumer at: {ConsumerIP}:5001");
                client.Connect(ConsumerIP, 5001);
                using NetworkStream stream = client.GetStream();
                using BinaryWriter writer = new(stream);
                using BinaryReader reader = new(stream);

                writer.Write(videoSubset.Count);

                foreach (var path in videoSubset)
                {
                    string compressedPath;
                    try
                    {
                        compressedPath = CompressVideo(path);
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"[Producer] Compression failed for {Path.GetFileName(path)}: {ex.Message}");
                        continue;
                    }

                    string fileName = Path.GetFileName(compressedPath);
                    byte[] fileNameBytes = Encoding.UTF8.GetBytes(fileName);
                    writer.Write(fileNameBytes.Length);
                    writer.Write(fileNameBytes);

                    byte[] fileData = File.ReadAllBytes(compressedPath);
                    byte[] hashBytes = SHA256.HashData(fileData);

                    writer.Write(hashBytes.Length);
                    writer.Write(hashBytes);

                    int serverResponse = reader.ReadInt32();
                    if (serverResponse == 0)
                    {
                        Logger.Log($"[Producer] Skipped (duplicate): {fileName}");
                        continue;
                    }
                    else if (serverResponse == 2)
                    {
                        Logger.Log($"[Producer] Rejected (queue full): {fileName}");
                        retryList.Add(path); 
                        continue;
                    }

                    writer.Write(fileData.Length);
                    stream.Write(fileData, 0, fileData.Length);

                    Logger.Log($"[Producer] Uploaded: {fileName}");
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[Producer] Upload error (batch): {ex.Message}");
            }

            if (retryList.Count > 0)
            {
                Logger.Log($"[Producer] Retrying {retryList.Count} rejected files after delay...");
                Thread.Sleep(3000);
                UploadVideo(retryList);
            }
        }

        private string CompressVideo(string inputPath)
        {
            //string directory = Path.GetDirectoryName(inputPath)!;

            string compressedFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Compressed");
            Directory.CreateDirectory(compressedFolder);
            string filenameNoExt = Path.GetFileNameWithoutExtension(inputPath);
            string compressedPath = Path.Combine(compressedFolder, $"{filenameNoExt}_{GetCRFName()}_compression.mp4");

            string ffmpegPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg.exe");
            int crf = GetCRF();

            if (crf == -1)
            {
                Logger.Log("[Producer] Skipped compression (no compression selected).");
                return inputPath; 
            }

            Process ffmpeg = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    Arguments = $"-y -i \"{inputPath}\" -vcodec libx264 -crf {crf} \"{compressedPath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            ffmpeg.Start();
            string error = ffmpeg.StandardError.ReadToEnd();
            ffmpeg.WaitForExit();

            if (ffmpeg.ExitCode != 0)
                Logger.Log($"[Producer] ffmpeg error:  {error}");

            if (!File.Exists(compressedPath))
            {
                throw new Exception("Compression failed.");
            }

            return compressedPath;
        }

        public int GetCRF()
        {
            return compressionIndex switch
            {
                0 => 10, 
                1 => 23, 
                2 => 49,
                _ => -1
            };
        }

        public string GetCRFName()
        {
            return compressionIndex switch
            {
                0 => "low",
                1 => "medium",
                2 => "high",
                _ => "null"
            };
        }

        [RelayCommand]
        private void DeleteFolder(FolderItem? folder)
        {
            if (folder != null && !isUploading)
            {
                folderPaths.Remove(folder.FolderPath);
                FoldersToUpload.Remove(folder);
                UploadVideosCommand.NotifyCanExecuteChanged();
            }
        }
    }
}
