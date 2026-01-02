using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;

namespace RMT_CLIENT_MVC
{
    public static class ScanMachineManager
    {
        public static (string output, bool isError) ScanNetwork()
        {
            try
            {
                var result = new List<string>();
                result.Add("=== ESCANEO DE RED LOCAL ===");
                result.Add("");

                // Obtener la dirección IP local
                string localIP = GetLocalIPAddress();
                if (string.IsNullOrEmpty(localIP))
                {
                    return ("No se pudo obtener la dirección IP local", true);
                }

                // Obtener la red local (primeros 3 octetos)
                string networkPrefix = localIP.Substring(0, localIP.LastIndexOf('.') + 1);
                result.Add($"Red local: {networkPrefix}0/24");
                result.Add("");

                // Escanear la red
                var activeHosts = new List<string>();
                for (int i = 1; i <= 254; i++)
                {
                    string ip = $"{networkPrefix}{i}";
                    if (IsHostActive(ip))
                    {
                        string hostname = GetHostName(ip);
                        string macAddress = GetMacAddress(ip);
                        activeHosts.Add($"IP: {ip} | Hostname: {hostname} | MAC: {macAddress}");
                    }
                }

                if (activeHosts.Count == 0)
                {
                    result.Add("No se encontraron hosts activos en la red");
                }
                else
                {
                    result.Add($"Hosts activos encontrados: {activeHosts.Count}");
                    result.Add("");
                    result.AddRange(activeHosts);
                }

                return (string.Join("\n", result), false);
            }
            catch (Exception ex)
            {
                return ($"Error al escanear la red: {ex.Message}", true);
            }
        }

        private static string GetLocalIPAddress()
        {
            try
            {
                using (var socket = new System.Net.Sockets.Socket(System.Net.Sockets.AddressFamily.InterNetwork, System.Net.Sockets.SocketType.Dgram, 0))
                {
                    socket.Connect("8.8.8.8", 65530);
                    IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                    return endPoint.Address.ToString();
                }
            }
            catch
            {
                return null;
            }
        }

        private static bool IsHostActive(string ip)
        {
            try
            {
                using (var ping = new Ping())
                {
                    var reply = ping.Send(ip, 100); // Timeout de 100ms
                    return reply.Status == IPStatus.Success;
                }
            }
            catch
            {
                return false;
            }
        }

        private static string GetHostName(string ip)
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "nbtstat",
                        Arguments = $"-A {ip}",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                var match = Regex.Match(output, @"Nombre único\s*<00>\s*:\s*(.+)|Unique\s*<00>\s*:\s*(.+)");
                if (match.Success)
                {
                    return !string.IsNullOrEmpty(match.Groups[1].Value) ?
                           match.Groups[1].Value.Trim() :
                           match.Groups[2].Value.Trim();
                }

                return "Desconocido";
            }
            catch
            {
                return "Error";
            }
        }

        private static string GetMacAddress(string ip)
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "arp",
                        Arguments = $"-a {ip}",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                var match = Regex.Match(output, @"([0-9A-Fa-f]{2}[:-]){5}([0-9A-Fa-f]{2})");
                return match.Success ? match.Value : "No disponible";
            }
            catch
            {
                return "Error";
            }
        }
    }
} 