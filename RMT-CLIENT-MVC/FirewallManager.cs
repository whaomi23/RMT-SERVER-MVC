using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMT_CLIENT_MVC
{
    public static class FirewallManager
    {
        public static (string output, bool isError) CheckFirewallStatus()
        {
            try
            {
                using (var process = new Process())
                {
                    process.StartInfo.FileName = "netsh";
                    process.StartInfo.Arguments = "advfirewall show allprofiles state";
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.CreateNoWindow = true;
                    process.Start();

                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();

                    // Verificar si el firewall está completamente inactivo
                    bool domainEnabled = output.Contains("Domain Profile Settings") && output.Contains("State ON");
                    bool privateEnabled = output.Contains("Private Profile Settings") && output.Contains("State ON");
                    bool publicEnabled = output.Contains("Public Profile Settings") && output.Contains("State ON");

                    if (!domainEnabled && !privateEnabled && !publicEnabled)
                    {
                        // Si el firewall está completamente inactivo, devolver la salida original
                        return (output, false);
                    }

                    // Si el firewall está activo en algún perfil, mostrar el resumen
                    StringBuilder result = new StringBuilder();
                    result.AppendLine("=== ESTADO DEL FIREWALL DE WINDOWS ===");

                    result.AppendLine($"Perfil de Dominio: {(domainEnabled ? "ACTIVO" : "INACTIVO")}");
                    result.AppendLine($"Perfil Privado: {(privateEnabled ? "ACTIVO" : "INACTIVO")}");
                    result.AppendLine($"Perfil Público: {(publicEnabled ? "ACTIVO" : "INACTIVO")}");

                    bool allEnabled = domainEnabled && privateEnabled && publicEnabled;
                    result.AppendLine($"\nEstado General: {(allEnabled ? "FIREWALL COMPLETAMENTE ACTIVO" : "FIREWALL PARCIALMENTE ACTIVO")}");

                    return (result.ToString(), false);
                }
            }
            catch (Exception ex)
            {
                return ($"Error al verificar el estado del firewall: {ex.Message}", true);
            }
        }

        public static (string output, bool isError) DisableFirewall()
        {
            try
            {
                using (var process = new Process())
                {
                    process.StartInfo.FileName = "cmd.exe";
                    process.StartInfo.Arguments = "/c netsh advfirewall set allprofiles state off";
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;
                    process.StartInfo.CreateNoWindow = true;
                    process.Start();

                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (!string.IsNullOrEmpty(error))
                    {
                        return ($"Error al desactivar firewall: {error}", true);
                    }

                    // Verificar la salida del comando
                    if (output.Contains("Ok.") || output.Contains("OK."))
                    {
                        return ("Firewall desactivado correctamente", false);
                    }
                    else
                    {
                        return ($"Error al desactivar firewall. Salida: {output}", true);
                    }
                }
            }
            catch (Exception ex)
            {
                return ($"Error al desactivar el firewall: {ex.Message}", true);
            }
        }

        public static (string output, bool isError) EnableFirewall()
        {
            try
            {
                using (var process = new Process())
                {
                    process.StartInfo.FileName = "cmd.exe";
                    process.StartInfo.Arguments = "/c netsh advfirewall set allprofiles state on";
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;
                    process.StartInfo.CreateNoWindow = true;
                    process.Start();

                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (!string.IsNullOrEmpty(error))
                    {
                        return ($"Error al activar firewall: {error}", true);
                    }

                    // Verificar la salida del comando
                    if (output.Contains("Ok.") || output.Contains("OK."))
                    {
                        return ("Firewall activado correctamente", false);
                    }
                    else
                    {
                        return ($"Error al activar firewall. Salida: {output}", true);
                    }
                }
            }
            catch (Exception ex)
            {
                return ($"Error al activar el firewall: {ex.Message}", true);
            }
        }
    }
}
