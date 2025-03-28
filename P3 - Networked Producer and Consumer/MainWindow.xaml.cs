using System.IO;
using System.Windows;
using System.Windows.Controls;
using P3___Networked_Producer.Views;

namespace P3___Networked_Producer
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            MainFrame.Navigate(new UploadPage());
        }
    }
}
