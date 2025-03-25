using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls; // For UI elements

namespace NetworkedConsumer
{
    public partial class MainWindow : Window
    {
        private HashSet<string> folderPaths = new HashSet<string>();
        private readonly HashSet<string> allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".mp4", ".mkv", ".avi", ".mov", ".wmv" };

        public MainWindow()
        {
            InitializeComponent();
        }

        // Handle Drag Over - Allow only folders
        private void Window_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] dropped = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (dropped.All(Directory.Exists)) // Accept only folders
                {
                    e.Effects = DragDropEffects.Copy;
                }
                else
                {
                    e.Effects = DragDropEffects.None;
                }
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        // Handle File Drop - Add Folders Only if they contain only videos
        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] paths = (string[])e.Data.GetData(DataFormats.FileDrop);
                List<string> acceptedFolders = new List<string>();
                List<string> rejectedFolders = new List<string>();

                foreach (string path in paths)
                {
                    if (Directory.Exists(path)) // Ensure it's a directory
                    {
                        string[] files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);

                        if (files.Length == 0 || files.All(file => allowedExtensions.Contains(Path.GetExtension(file))))
                        {
                            // Folder contains only videos (or is empty)
                            if (!folderPaths.Contains(path))
                            {
                                folderPaths.Add(path);
                                acceptedFolders.Add(Path.GetFileName(path)); // Store only the folder name
                                TextBlock folderName = new TextBlock
                                {
                                    Text = "📂 " + Path.GetFileName(path),
                                    VerticalAlignment = VerticalAlignment.Center
                                };
                                FolderListBox.Items.Add(folderName);
                            }
                        }
                        else
                        {
                            // Folder contains non-video files
                            rejectedFolders.Add(Path.GetFileName(path));
                        }
                    }
                }

                if (acceptedFolders.Count > 0)
                {
                    MessageBox.Show($"{acceptedFolders.Count} folder(s) added.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                if (rejectedFolders.Count > 0)
                {
                    MessageBox.Show($"Folders containing only videos are accepted!\nRejected folders:\n{string.Join("\n", rejectedFolders)}",
                                    "Invalid Folders", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        // Handle Upload Button Click
        private void UploadButton_Click(object sender, RoutedEventArgs e)
        {
            if (folderPaths.Count == 0)
            {
                MessageBox.Show("No folders selected!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            MessageBox.Show($"Uploading {folderPaths.Count} folders...", "Upload", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
