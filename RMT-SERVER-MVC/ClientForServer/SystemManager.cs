using System;
using System.Diagnostics;

namespace RMT_CLIENT_MVC
{
    public static class SystemManager
    {
        public static (string output, bool isError) Shutdown()
        {
            try
            {
                using (var process = new Process())
                {
                    process.StartInfo.FileName = "cmd.exe";
                    process.StartInfo.Arguments = "/c shutdown /s /t 0 /f";
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
                        return ($"Error al apagar el sistema: {error}", true);
                    }

                    return ("Sistema apagándose...", false);
                }
            }
            catch (Exception ex)
            {
                return ($"Error al apagar el sistema: {ex.Message}", true);
            }
        }

        public static (string output, bool isError) Restart()
        {
            try
            {
                using (var process = new Process())
                {
                    process.StartInfo.FileName = "cmd.exe";
                    process.StartInfo.Arguments = "/c shutdown /r /t 0 /f";
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
                        return ($"Error al reiniciar el sistema: {error}", true);
                    }

                    return ("Sistema reiniciándose...", false);
                }
            }
            catch (Exception ex)
            {
                return ($"Error al reiniciar el sistema: {ex.Message}", true);
            }
        }

        public static (string output, bool isError) CloseSession()
        {
            try
            {
                using (var process = new Process())
                {
                    process.StartInfo.FileName = "cmd.exe";
                    process.StartInfo.Arguments = "/c shutdown /l";
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
                        return ($"Error al cerrar sesión: {error}", true);
                    }

                    return ("Sesión cerrada", false);
                }
            }
            catch (Exception ex)
            {
                return ($"Error al cerrar sesión: {ex.Message}", true);
            }
        }

        public static (string output, bool isError) CloseApplication()
        {
            try
            {
                Environment.Exit(0);
                return ("Aplicación cerrada", false);
            }
            catch (Exception ex)
            {
                return ($"Error al cerrar la aplicación: {ex.Message}", true);
            }
        }
    }
} 