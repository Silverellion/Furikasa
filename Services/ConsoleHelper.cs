using System;
using System.Runtime.InteropServices;

namespace Furikasa.Services
{
    internal static class ConsoleHelper
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AllocConsole();

        private static bool _allocated;
        public static void AllocConsoleSafe()
        {
            if (_allocated) return;
            try
            {
                _allocated = AllocConsole();
                if (_allocated)
                {
                    Console.WriteLine("[Console] Allocated console window.");
                }
            }
            catch
            {
                // Ignore failures silently.
            }
        }
    }
}
