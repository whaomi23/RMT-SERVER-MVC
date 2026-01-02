using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RMT_CLIENT_MVC
{
    public class RemoteMonitor
    {
        private static bool _isMonitoring = false;
        private static System.Threading.Timer _captureTimer;
        private static System.Threading.Timer _statusTimer;
        private static HttpClient _httpClient;
        private static string _serverUrl;
        private static int _captureInterval = 1000; // 1 segundo por defecto
        private static int _quality = 70; // Calidad JPEG (70%)

        public static void Initialize(string serverUrl)
        {
            _serverUrl = serverUrl;

            // Configurar HttpClient para ignorar errores SSL (solo para desarrollo)
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
            };
            _httpClient = new HttpClient(handler);
            _httpClient.Timeout = TimeSpan.FromSeconds(30);

            // Configurar timers
            _statusTimer = new System.Threading.Timer(CheckMonitorStatus, null, 0, 5000); // Verificar estado cada 5 segundos
            _captureTimer = new System.Threading.Timer(CaptureAndSendFrame, null, Timeout.Infinite, Timeout.Infinite);
        }

        public static void StartMonitoring()
        {
            if (!_isMonitoring)
            {
                _isMonitoring = true;
                _captureTimer.Change(0, _captureInterval);
                Console.WriteLine("\nMonitoreo iniciado");
            }
        }

        public static void StopMonitoring()
        {
            if (_isMonitoring)
            {
                _isMonitoring = false;
                _captureTimer.Change(Timeout.Infinite, Timeout.Infinite);
                Console.WriteLine("\nMonitoreo detenido");
            }
        }

        public static void SetCaptureInterval(int interval)
        {
            if (interval > 0)
            {
                _captureInterval = interval;
                if (_isMonitoring)
                {
                    _captureTimer.Change(0, _captureInterval);
                }
            }
        }

        public static void SetImageQuality(int quality)
        {
            if (quality >= 1 && quality <= 100)
            {
                _quality = quality;
            }
        }

        private static async void CheckMonitorStatus(object state)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_serverUrl}/api/monitor-status");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    bool serverWantsMonitoring = content.Contains("\"isActive\":true");

                    if (serverWantsMonitoring && !_isMonitoring)
                    {
                        StartMonitoring();
                    }
                    else if (!serverWantsMonitoring && _isMonitoring)
                    {
                        StopMonitoring();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nError verificando estado: {ex.Message}");
            }
        }

        private static async void CaptureAndSendFrame(object state)
        {
            if (!_isMonitoring) return;

            try
            {
                using (var bmp = CaptureMonitor())
                using (var ms = new MemoryStream())
                {
                    // Comprimir imagen con calidad configurada
                    var encoderParams = new EncoderParameters(1);
                    encoderParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, _quality);

                    var jpegEncoder = GetEncoder(ImageFormat.Jpeg);
                    bmp.Save(ms, jpegEncoder, encoderParams);

                    var content = new MultipartFormDataContent();
                    content.Add(new ByteArrayContent(ms.ToArray()), "frame", "screenshot.jpg");

                    await _httpClient.PostAsync($"{_serverUrl}/api/upload-frame", content);
                    Console.WriteLine($"Captura enviada a {DateTime.Now:T}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nError en captura: {ex.Message}");
                StopMonitoring();
            }
        }

        private static Bitmap CaptureMonitor()
        {
            var bounds = Screen.PrimaryScreen.Bounds;
            var bmp = new Bitmap(bounds.Width, bounds.Height);

            using (var g = Graphics.FromImage(bmp))
            {
                g.CopyFromScreen(bounds.X, bounds.Y, 0, 0, bounds.Size, CopyPixelOperation.SourceCopy);
            }

            return bmp;
        }

        private static ImageCodecInfo GetEncoder(ImageFormat format)
        {
            var codecs = ImageCodecInfo.GetImageEncoders();
            foreach (var codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }
    }
}
