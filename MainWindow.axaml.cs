using Avalonia.Controls;
using Furikasa.Services;
using System;

namespace Furikasa
{
    public partial class MainWindow : Window
    {
        private readonly OcrService _ocrService = new OcrService();
        public MainWindow()
        {
            InitializeComponent();
            this.Opened += OnOpened;
            this.Closing += OnClosing;
        }

        private void OnOpened(object? sender, EventArgs e)
        {
            _ocrService.Start();
        }

        private void OnClosing(object? sender, WindowClosingEventArgs e)
        {
            _ocrService.Dispose();
        }
    }
}