using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Tesseract;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.Versioning;

namespace Furikasa.Services
{
	[SupportedOSPlatform("windows")]
	public sealed class OcrService : IDisposable
	{
		private readonly string _tessDataPath;
		private TesseractEngine? _engine;
		private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
		private bool _disposed;

		public OcrService()
		{
			_tessDataPath = Path.Combine(AppContext.BaseDirectory, "tessdata");
			Directory.CreateDirectory(_tessDataPath);
		}

		private bool HasLanguageFile(string lang) => File.Exists(Path.Combine(_tessDataPath, lang + ".traineddata"));

		private TesseractEngine? EnsureEngine()
		{
			if (_engine != null) return _engine;
			if (!HasLanguageFile("jpn")) return null;
			try
			{
				_engine = new TesseractEngine(_tessDataPath, "jpn+eng", EngineMode.Default);
				return _engine;
			}
			catch (Exception)
			{
				return null;
			}
		}

		public async Task CaptureWindowAndOcrAsync(Window window)
		{
			if (_disposed) return;
			if (!OperatingSystem.IsWindows())
				return; // Only Windows capture implemented

			ConsoleHelper.AllocConsoleSafe();
			var engine = EnsureEngine();
			if (engine == null)
			{
				Console.WriteLine("Tesseract not initialized. Ensure tessdata/jpn.traineddata exists.");
				return;
			}

			await _semaphore.WaitAsync();
			try
			{
				if (!TryGetWindowRect(out var rect))
					return;

				var bmp = CaptureScreenRect(rect);
				if (bmp == null) return;

				using var ms = new MemoryStream();
				bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
				bmp.Dispose();
				ms.Position = 0;
				using var pix = Pix.LoadFromMemory(ms.ToArray());
				using var page = engine.Process(pix);
				var text = page.GetText()?.Trim();
				if (!string.IsNullOrWhiteSpace(text))
				{
					Console.WriteLine(text);
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"OCR error: {ex.Message}");
			}
			finally
			{
				_semaphore.Release();
			}
		}

		private static bool TryGetWindowRect(out RECT rect)
		{
			IntPtr hwnd = GetForegroundWindow();
			if (hwnd == IntPtr.Zero)
			{
				rect = default;
				return false;
			}
			return GetWindowRect(hwnd, out rect);
		}

		private static Bitmap? CaptureScreenRect(RECT rect)
		{
			int width = rect.Right - rect.Left;
			int height = rect.Bottom - rect.Top;
			if (width <= 0 || height <= 0) return null;

			var bmp = new Bitmap(width, height, PixelFormat.Format24bppRgb);
			using var g = Graphics.FromImage(bmp);
			g.CopyFromScreen(rect.Left, rect.Top, 0, 0, new System.Drawing.Size(width, height), CopyPixelOperation.SourceCopy);
			return bmp;
		}

		public void Dispose()
		{
			if (_disposed) return;
			_disposed = true;
			_engine?.Dispose();
			_semaphore.Dispose();
		}

		#region Win32
		[StructLayout(LayoutKind.Sequential)]
		private struct RECT { public int Left; public int Top; public int Right; public int Bottom; }

		[DllImport("user32.dll")]
		private static extern IntPtr GetForegroundWindow();

		[DllImport("user32.dll")]
		private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
		#endregion
	}
}
