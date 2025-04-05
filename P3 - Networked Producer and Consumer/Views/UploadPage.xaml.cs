using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using P3___Networked_Producer.ViewModels;

namespace P3___Networked_Producer.Views
{
    /// <summary>
    /// Interaction logic for UploadPage.xaml
    /// </summary>
    public partial class UploadPage : Page
    {
        private readonly UploadViewModel viewModel;

        public UploadPage()
        {
            InitializeComponent();
            viewModel = new UploadViewModel();
            DataContext = viewModel;

            Logger.LogAction = LogToUI;
        }

        // Handle Drag Over
        private void Window_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = viewModel.CanAcceptDrag(e) ? DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;
        }

        // Handle File Drop
        private void Window_Drop(object sender, DragEventArgs e)
        {
            viewModel.HandleFileDrop(e);
        }


        private void LogToUI(string message)
        {
            Dispatcher.Invoke(() =>
            {
                ProducerLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
                ProducerLog.ScrollToEnd();
            });
        }
    }
}
