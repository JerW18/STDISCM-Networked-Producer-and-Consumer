using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace P3___Networked_Consumer
{
    public partial class FullScreenWindow : Window
    {
        public FullScreenWindow(string videoPath)
        {
            InitializeComponent();

            mediaElement.Source = new Uri(videoPath);
            mediaElement.Play();

            // Allow the window to be dragged by clicking anywhere on the grid background
            this.MouseLeftButtonDown += (s, e) =>
            {
                if (e.ClickCount == 1)
                    this.DragMove();
            };
        }

        // Close window when the exit button is clicked
        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // Also allow closing the window by pressing the Escape key
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                this.Close();
        }
    }
}
