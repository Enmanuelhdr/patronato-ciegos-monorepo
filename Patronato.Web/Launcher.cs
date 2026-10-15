using System;
using System.Diagnostics;
using System.IO;

namespace Patronato.Web
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "Patronato.Web - Servidor de Desarrollo (React + Vite)";
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("======================================================================");
            Console.WriteLine("   PATRONATO NACIONAL DE CIEGOS - PORTAL WEB (REACT + VITE)");
            Console.WriteLine("======================================================================");
            Console.ResetColor();
            Console.WriteLine();

            // Ubicar la carpeta raíz de Patronato.Web
            var dirActual = AppDomain.CurrentDomain.BaseDirectory;
            var webDir = dirActual;
            while (!File.Exists(Path.Combine(webDir, "package.json")))
            {
                var parent = Directory.GetParent(webDir);
                if (parent == null) break;
                webDir = parent.FullName;
            }

            if (!File.Exists(Path.Combine(webDir, "package.json")))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[Error] No se encontró package.json en {webDir}");
                Console.ResetColor();
                Console.ReadLine();
                return;
            }

            Console.WriteLine($"Directorio Web: {webDir}");
            Console.WriteLine("Iniciando Vite en http://localhost:5173 ...");
            Console.WriteLine();

            // Asegurar PATH con Node.js
            var nodePath = @"C:\Program Files\nodejs";
            var pathActual = Environment.GetEnvironmentVariable("PATH") ?? "";
            if (!pathActual.Contains(nodePath, StringComparison.OrdinalIgnoreCase) && Directory.Exists(nodePath))
            {
                Environment.SetEnvironmentVariable("PATH", $"{nodePath};{pathActual}");
            }

            // Abrir navegador
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "http://localhost:5173",
                    UseShellExecute = true
                });
            }
            catch { }

            // Ejecutar npm run dev
            var npmExe = File.Exists(Path.Combine(nodePath, "npm.cmd")) 
                ? Path.Combine(nodePath, "npm.cmd") 
                : "npm.cmd";

            var psi = new ProcessStartInfo
            {
                FileName = npmExe,
                Arguments = "run dev",
                WorkingDirectory = webDir,
                UseShellExecute = false
            };

            try
            {
                var proc = Process.Start(psi);
                proc?.WaitForExit();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error ejecutando npm: {ex.Message}");
                Console.ResetColor();
                Console.WriteLine("Presiona Enter para salir...");
                Console.ReadLine();
            }
        }
    }
}
