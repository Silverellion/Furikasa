using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Furikasa.Services
{
    internal static class ConsoleHelper
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AllocConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleOutputCP(uint wCodePageID);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleCP(uint wCodePageID);

        private static bool _allocated;
        public static void AllocConsoleSafe()
        {
            if (_allocated) return;
            try
            {
                _allocated = AllocConsole();
                if (_allocated)
                {
                    // Ensure UTF-8 for Japanese text and rebind std streams.
                    SetConsoleOutputCP(65001);
                    SetConsoleCP(65001);
                    Console.OutputEncoding = Encoding.UTF8;
                    Console.InputEncoding = Encoding.UTF8;

                    var stdout = Console.OpenStandardOutput();
                    var stderr = Console.OpenStandardError();
                    var stdin = Console.OpenStandardInput();

                    Console.SetOut(new StreamWriter(stdout) { AutoFlush = true });
                    Console.SetError(new StreamWriter(stderr) { AutoFlush = true });
                    Console.SetIn(new StreamReader(stdin));

                    Console.Title = "Furikasa OCR";
                    Console.WriteLine("[Console] Allocated console window (UTF-8). Drop images into the 'Images' folder next to the executable.");
                }
            }
            catch
            {
                // Ignore failures silently.
            }
        }
    }
}
