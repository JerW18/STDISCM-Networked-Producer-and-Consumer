﻿using System.Windows;
using P3___Networked_Producer.Views;

namespace P3___Networked_Producer
{
    public partial class MainWindow : Window
    {
        private readonly UploadPage? uploadPage;

        public MainWindow()
        {
            InitializeComponent();

            uploadPage ??= new UploadPage();
            MainFrame.Navigate(uploadPage);
        }
    }
}
