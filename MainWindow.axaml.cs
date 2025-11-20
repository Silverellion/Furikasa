using Avalonia.Controls;
using Furikasa.Services;
using System;
using Avalonia.Input;

namespace Furikasa
{
    public partial class MainWindow : Window
    {
        private readonly OcrService _ocrService = new OcrService();
        public MainWindow()
        {
            InitializeComponent();
            this.KeyDown += OnKeyDown;
            this.Closing += OnClosing;
        }

        private void OnOpened(object? sender, EventArgs e)
        {
            // No automatic OCR or console allocation on startup.
        }

        private void OnClosing(object? sender, WindowClosingEventArgs e)
        {
            _ocrService.Dispose();
        }

        private async void OnKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.F11)
            {
                await _ocrService.CaptureWindowAndOcrAsync(this);
            }
        }
    }
}