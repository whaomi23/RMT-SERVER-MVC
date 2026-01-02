using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace RMT_CLIENT_MVC
{
    public class OsCommands
    {
        private static OsCommands _instance;
        private static readonly object _lock = new object();
        private static HttpClient httpClient;
        private static string serverURL = "http://192.168.1.104:7277/Clients";

        // Constructor privado para implementar Singleton
        private OsCommands() 
        {
            // Configurar HttpClient para ignorar errores SSL (solo para desarrollo)
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
            };

            httpClient = new HttpClient(handler);
            httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        // Propiedad para acceder a la instancia única
        public static OsCommands Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new OsCommands();
                        }
                    }
                }
                return _instance;
            }
        }

        public async Task<(string output, bool isError)> ExecuteOsCommand(string command, string machineName, string ipAddress, string commandId = null)
        {
            try
            {
                (string output, bool isError) result = default;

                switch (command.ToUpper())
                {
                    case "OFF":
                        result = Shutdown();
                        break;
                    case "CLOSE-SESSION":
                        result = CloseSession();
                        break;
                    case "CLOSE":
                        result = CloseApplication();
                        break;
                    case "RESTART":
                        result = await Restart();
                        break;
                    case "LOCK":
                        result = await Lock();
                        break;
                    case "SLEEP":
                        result = await Sleep();
                        break;
                    case "KILL-CLIENT":
                        result = await KillClient();
                        break;
                    default:
                        result = (output: $"Comando no reconocido: {command}", isError: true);
                        break;
                }

                // Enviar resultado al servidor
                var formData = new MultipartFormDataContent();
                formData.Add(new StringContent(machineName), "machineName");
                formData.Add(new StringContent(ipAddress), "ipAddress");
                formData.Add(new StringContent(commandId ?? command), "commandId");
                formData.Add(new StringContent(result.output), "result");
                formData.Add(new StringContent(result.isError.ToString()), "isError");

                var response = await httpClient.PostAsync($"{serverURL}/SubmitCommandResult", formData);
                response.EnsureSuccessStatusCode();

                return result;
            }
            catch (Exception ex)
            {
                var errorResult = (output: $"Error al ejecutar comando: {ex.Message}", isError: true);
                
                // Enviar error al servidor
                var formData = new MultipartFormDataContent();
                formData.Add(new StringContent(machineName), "machineName");
                formData.Add(new StringContent(ipAddress), "ipAddress");
                formData.Add(new StringContent(commandId ?? command), "commandId");
                formData.Add(new StringContent(errorResult.output), "result");
                formData.Add(new StringContent(errorResult.isError.ToString()), "isError");

                var response = await httpClient.PostAsync($"{serverURL}/SubmitCommandResult", formData);
                response.EnsureSuccessStatusCode();

                return errorResult;
            }
        }

        private (string output, bool isError) Shutdown()
        {
            try
            {
                Process.Start("shutdown", "/s /t 0 /f");
                return (output: "Sistema apagándose...", isError: false);
            }
            catch (Exception ex)
            {
                return (output: $"Error al apagar el sistema: {ex.Message}", isError: true);
            }
        }

        private (string output, bool isError) CloseSession()
        {
            try
            {
                Process.Start("shutdown", "/l");
                return (output: "Sesión cerrada", isError: false);
            }
            catch (Exception ex)
            {
                return (output: $"Error al cerrar la sesión: {ex.Message}", isError: true);
            }
        }

        private (string output, bool isError) CloseApplication()
        {
            try
            {
                Environment.Exit(0);
                return (output: "Aplicación cerrada", isError: false);
            }
            catch (Exception ex)
            {
                return (output: $"Error al cerrar la aplicación: {ex.Message}", isError: true);
            }
        }

        private async Task<(string output, bool isError)> Restart()
        {
            try
            {
                Process.Start("shutdown", "/r /t 0 /f");
                return (output: "Sistema reiniciándose...", isError: false);
            }
            catch (Exception ex)
            {
                return (output: $"Error al reiniciar: {ex.Message}", isError: true);
            }
        }

        private async Task<(string output, bool isError)> Sleep()
        {
            try
            {
                Process.Start("rundll32.exe", "powrprof.dll,SetSuspendState 0,1,0");
                return (output: "Sistema suspendido", isError: false);
            }
            catch (Exception ex)
            {
                return (output: $"Error al suspender: {ex.Message}", isError: true);
            }
        }

        private async Task<(string output, bool isError)> Lock()
        {
            try
            {
                Process.Start("rundll32.exe", "user32.dll,LockWorkStation");
                return (output: "Sesión bloqueada", isError: false);
            }
            catch (Exception ex)
            {
                return (output: $"Error al bloquear: {ex.Message}", isError: true);
            }
        }

        private async Task<(string output, bool isError)> KillClient()
        {
            try
            {
                // Primero enviamos el resultado al servidor
                var result = (output: "Cliente cerrado exitosamente", isError: false);
                
                // Esperamos un momento para asegurar que el resultado se envíe
                await Task.Delay(1000);
                
                // Luego cerramos la aplicación
                Environment.Exit(0);
                
                return result;
            }
            catch (Exception ex)
            {
                return (output: $"Error al cerrar el cliente: {ex.Message}", isError: true);
            }
        }

        private async Task SubmitCommandResult(string machineName, string ipAddress, string command, string result, bool isError)
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

                    var formData = new MultipartFormDataContent();
                    formData.Add(new StringContent(machineName), "machineName");
                    formData.Add(new StringContent(ipAddress), "ipAddress");
                    formData.Add(new StringContent(command), "commandId");
                    formData.Add(new StringContent(chunk), "result");
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
    }
} 