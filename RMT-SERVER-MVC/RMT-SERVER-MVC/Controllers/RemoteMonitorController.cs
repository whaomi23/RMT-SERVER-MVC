using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace RMT_SERVER_MVC.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RemoteMonitorController : ControllerBase
    {
        private static bool _isActive = false;
        private static int _currentInterval = 1000;
        private static int _currentQuality = 70;

        [HttpGet("monitor-status")]
        public IActionResult GetMonitorStatus()
        {
            return Ok(new
            {
                isActive = _isActive,
                interval = _currentInterval,
                quality = _currentQuality
            });
        }

        [HttpPost("start-monitoring")]
        public IActionResult StartMonitoring()
        {
            _isActive = true;
            return Ok(new { message = "Monitoreo iniciado" });
        }

        [HttpPost("stop-monitoring")]
        public IActionResult StopMonitoring()
        {
            _isActive = false;
            return Ok(new { message = "Monitoreo detenido" });
        }

        [HttpPost("set-interval")]
        public IActionResult SetInterval([FromBody] int interval)
        {
            if (interval < 100) return BadRequest("El intervalo mínimo es 100ms");
            _currentInterval = interval;
            return Ok(new { message = $"Intervalo configurado a {interval}ms" });
        }

        [HttpPost("set-quality")]
        public IActionResult SetQuality([FromBody] int quality)
        {
            if (quality < 1 || quality > 100) return BadRequest("La calidad debe estar entre 1 y 100");
            _currentQuality = quality;
            return Ok(new { message = $"Calidad configurada a {quality}%" });
        }

        [HttpPost("upload-frame")]
        public async Task<IActionResult> UploadFrame()
        {
            try
            {
                var file = Request.Form.Files[0];
                if (file == null) return BadRequest("No se recibió ningún frame");

                // Aquí puedes procesar el frame como necesites
                // Por ejemplo, guardarlo en disco o en memoria
                var fileName = $"frame_{DateTime.Now:yyyyMMdd_HHmmss}.jpg";
                var filePath = Path.Combine("wwwroot", "frames", fileName);

                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                return Ok(new { message = "Frame recibido correctamente", fileName });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
} 