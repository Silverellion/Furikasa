using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tesseract;

namespace Furikasa.Services
{
	public sealed class OcrService : IDisposable
	{
		private readonly string _tessDataPath;
		private readonly string _watchPath;
		private FileSystemWatcher? _watcher;
		private TesseractEngine? _engine;
		private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
		private bool _disposed;

		public OcrService()
		{
			_tessDataPath = Path.Combine(AppContext.BaseDirectory, "tessdata");
			_watchPath = Path.Combine(AppContext.BaseDirectory, "Images");
			Directory.CreateDirectory(_tessDataPath);
			Directory.CreateDirectory(_watchPath);
		}

		public void Start()
		{
			ConsoleHelper.AllocConsoleSafe();
			Console.WriteLine("[OCR] Starting service...");
			if (!HasLanguageFile("jpn") || !HasLanguageFile("eng"))
			{
				Console.WriteLine("[OCR] Missing traineddata files (jpn/eng). Place them in tessdata.");
			}

			try
			{
				_engine = new TesseractEngine(_tessDataPath, "jpn+eng", EngineMode.Default);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[OCR] Failed to init engine: {ex.Message}");
				return;
			}

			_watcher = new FileSystemWatcher(_watchPath)
			{
				EnableRaisingEvents = true,
				IncludeSubdirectories = false,
				Filter = "*.*"
			};
			_watcher.Created += OnFileCreated;
			_watcher.Renamed += OnFileCreated;
			Console.WriteLine($"[OCR] Watching folder: {_watchPath}");
		}

		private bool HasLanguageFile(string lang) => File.Exists(Path.Combine(_tessDataPath, lang + ".traineddata"));

		private async void OnFileCreated(object? sender, FileSystemEventArgs e)
		{
			if (_engine == null) return;
			var ext = Path.GetExtension(e.FullPath).ToLowerInvariant();
			if (ext != ".png" && ext != ".jpg" && ext != ".jpeg" && ext != ".bmp") return;

			// Wait briefly for file write to finish
			await Task.Delay(300);
			await _semaphore.WaitAsync();
			try
			{
				Console.WriteLine($"[OCR] Processing: {Path.GetFileName(e.FullPath)}");
				using var img = Pix.LoadFromFile(e.FullPath);
				using var page = _engine.Process(img);
				var text = page.GetText();
				var kanji = new string(text.Where(IsKanji).ToArray());
				Console.WriteLine("[OCR] Full Text:\n" + text.Trim());
				Console.WriteLine("[OCR] Kanji Only: " + kanji);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[OCR] Error: {ex.Message}");
			}
			finally
			{
				_semaphore.Release();
			}
		}

		private static bool IsKanji(char c)
		{
			int code = c;
			// Basic CJK Unified Ideographs & extensions common for Japanese Kanji
			return (code >= 0x4E00 && code <= 0x9FFF) // Unified Ideographs
				   || (code >= 0x3400 && code <= 0x4DBF) // Extension A
				   || (code >= 0xF900 && code <= 0xFAFF); // Compatibility Ideographs
		}

		public void Dispose()
		{
			if (_disposed) return;
			_disposed = true;
			if (_watcher != null)
			{
				_watcher.EnableRaisingEvents = false;
				_watcher.Created -= OnFileCreated;
				_watcher.Renamed -= OnFileCreated;
				_watcher.Dispose();
			}
			_engine?.Dispose();
			_semaphore.Dispose();
		}
	}
}
