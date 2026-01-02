using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace RMT_CLIENT_MVC
{
    public static class UacManager
    {
        public static (string output, bool isError) CheckUacSettings()
        {
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\System"))
                {
                    if (key == null)
                    {
                        return ("No se pudo acceder a la clave del registro UAC", true);
                    }

                    int enableLUA = (int)(key.GetValue("EnableLUA") ?? 0);
                    int promptOnSecureDesktop = (int)(key.GetValue("PromptOnSecureDesktop") ?? 0);

                    string result = "Estado UAC:\n";
                    result += $"EnableLUA: {(enableLUA == 1 ? "Habilitado" : "Deshabilitado")}\n";
                    result += $"PromptOnSecureDesktop: {(promptOnSecureDesktop == 1 ? "Habilitado" : "Deshabilitado")}\n";
                    result += "\nNota: Si ambos valores están en 1, el UAC está completamente habilitado.";

                    return (result, false);
                }
            }
            catch (Exception ex)
            {
                return ($"Error al verificar configuración UAC: {ex.Message}", true);
            }
        }
    }
}
