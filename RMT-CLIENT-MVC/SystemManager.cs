using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMT_CLIENT_MVC
{
    //public static class SystemManager
    //{
    //    public static (string output, bool isError) Shutdown()
    //    {
    //        try
    //        {
    //            using (var process = new Process())
    //            {
    //                process.StartInfo.FileName = "cmd.exe";
    //                process.StartInfo.Arguments = "/c shutdown /s /t 0 /f";
    //                process.StartInfo.UseShellExecute = false;
    //                process.StartInfo.RedirectStandardOutput = true;
    //                process.StartInfo.RedirectStandardError = true;
    //                process.StartInfo.CreateNoWindow = true;
    //                process.Start();

    //                string output = process.StandardOutput.ReadToEnd();
    //                string error = process.StandardError.ReadToEnd();
    //                process.WaitForExit();

    //                if (!string.IsNullOrEmpty(error))
    //                {
    //                    return ($"Error al apagar el sistema: {error}", true);
    //                }

    //                return ("Sistema apagándose...", false);
    //            }
    //        }
    //        catch (Exception ex)
    //        {
    //            return ($"Error al apagar el sistema: {ex.Message}", true);
    //        }
    //    }

    //    public static (string output, bool isError) Restart()
    //    {
    //        try
    //        {
    //            using (var process = new Process())
    //            {
    //                process.StartInfo.FileName = "cmd.exe";
    //                process.StartInfo.Arguments = "/c shutdown /r /t 0 /f";
    //                process.StartInfo.UseShellExecute = false;
    //                process.StartInfo.RedirectStandardOutput = true;
    //                process.StartInfo.RedirectStandardError = true;
    //                process.StartInfo.CreateNoWindow = true;
    //                process.Start();

    //                string output = process.StandardOutput.ReadToEnd();
    //                string error = process.StandardError.ReadToEnd();
    //                process.WaitForExit();

    //                if (!string.IsNullOrEmpty(error))
    //                {
    //                    return ($"Error al reiniciar el sistema: {error}", true);
    //                }

    //                return ("Sistema reiniciándose...", false);
    //            }
    //        }
    //        catch (Exception ex)
    //        {
    //            return ($"Error al reiniciar el sistema: {ex.Message}", true);
    //        }
    //    }

    //    public static (string output, bool isError) CloseSession()
    //    {
    //        try
    //        {
    //            using (var process = new Process())
    //            {
    //                process.StartInfo.FileName = "cmd.exe";
    //                process.StartInfo.Arguments = "/c shutdown /l";
    //                process.StartInfo.UseShellExecute = false;
    //                process.StartInfo.RedirectStandardOutput = true;
    //                process.StartInfo.RedirectStandardError = true;
    //                process.StartInfo.CreateNoWindow = true;
    //                process.Start();

    //                string output = process.StandardOutput.ReadToEnd();
    //                string error = process.StandardError.ReadToEnd();
    //                process.WaitForExit();

    //                if (!string.IsNullOrEmpty(error))
    //                {
    //                    return ($"Error al cerrar sesión: {error}", true);
    //                }

    //                return ("Sesión cerrada", false);
    //            }
    //        }
    //        catch (Exception ex)
    //        {
    //            return ($"Error al cerrar sesión: {ex.Message}", true);
    //        }
    //    }

    //    public static (string output, bool isError) CloseApplication()
    //    {
    //        try
    //        {
    //            Environment.Exit(0);
    //            return ("Aplicación cerrada", false);
    //        }
    //        catch (Exception ex)
    //        {
    //            return ($"Error al cerrar la aplicación: {ex.Message}", true);
    //        }
    //    }
    //}

    public static class SystemManager
    {
        private static string MayanCmd => MayanTranslator.Decode("tzol-kin-baktun-pop-tun-katun-haab"); // cmd.exe

        private static string ShutdownArgs => MayanTranslator.Decode("kab-shu-sal-chan-hun-nah"); // /c shutdown /s /t 0 /f
        private static string RestartArgs => MayanTranslator.Decode("kab-shu-niil-chan-hun-nah"); // /c shutdown /r /t 0 /f
        private static string CloseSessionArgs => MayanTranslator.Decode("kab-shu-luum"); // /c shutdown /l

        private static string LockArgs => MayanTranslator.Decode("kab-áalkab-libroʼob-pop-tun-katun-haab-máakÓoxp'éelyéetelka'ap'éel-pop-libroʼob-jantej-privarlemeyaj"); // rundll32.exe user32.dll,LockWorkStation

        public static (string output, bool isError) LockWorkstation() =>
            Azteca(LockArgs, "Error al bloquear la estación", "Estación bloqueada");

        public static (string output, bool isError) Shutdown() =>
            Azteca(ShutdownArgs, "Error al apagar el sistema", "Sistema apagándose...");

        public static (string output, bool isError) Restart() =>
            Azteca(RestartArgs, "Error al reiniciar el sistema", "Sistema reiniciándose...");

        public static (string output, bool isError) CloseSession() =>
            Azteca(CloseSessionArgs, "Error al cerrar sesión", "Sesión cerrada");

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

        private static (string output, bool isError) Azteca(string arguments, string errorMsg, string successMsg)
        {
            try
            {
                using (var process = new Process())
                {
                    process.StartInfo.FileName = MayanCmd;
                    process.StartInfo.Arguments = arguments;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;
                    process.StartInfo.CreateNoWindow = true;
                    process.Start();

                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (!string.IsNullOrEmpty(error))
                        return ($"{errorMsg}: {error}", true);

                    return (successMsg, false);
                }
            }
            catch (Exception ex)
            {
                return ($"{errorMsg}: {ex.Message}", true);
            }
        }
    }
}
