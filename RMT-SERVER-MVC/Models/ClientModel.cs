namespace RMT_SERVER_MVC.Models
{
    public class ClientModel
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string MachineName { get; set; }
        public string UserName { get; set; }
        public string IPAddress { get; set; }
        public string OSVersion { get; set; }
        public string OSArchitecture { get; set; }
        public string Antivirus { get; set; }
        public bool IsVirtualMachine { get; set; }
        public string Country { get; set; }
        public string CPU { get; set; }
        public string RAM { get; set; }
        public string Drives { get; set; }

        public DateTime LastConnection { get; set; } = DateTime.Now;
        public bool IsOnline { get; set; } = true;

        public Screenshot Screenshot { get; set; }
        public List<ClientCommand> PendingCommands { get; set; }
        public List<CommandResult> CommandResults { get; set; }

        // ... otras propiedades ...
        public List<FileTransferCommand> PendingFileTransfers { get; set; } = new List<FileTransferCommand>();

        public List<FileSystemEntry> FileSystemEntries { get; set; } = new List<FileSystemEntry>();

        public Queue<ScreenshotFrame> ScreenshotFrames { get; set; } = new Queue<ScreenshotFrame>();
        public int MaxFrames { get; set; } = 30; // Mantener los últimos 30 frames
        public bool IsStreaming { get; set; } = false;

        public ClientModel()
        {
            ScreenshotFrames = new Queue<ScreenshotFrame>();
            PendingCommands = new List<ClientCommand>();
            CommandResults = new List<CommandResult>();
            PendingFileTransfers = new List<FileTransferCommand>();
            FileSystemEntries = new List<FileSystemEntry>();
        }
    }

    public class Screenshot
    {
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class ClientCommand
    {
        public string Id { get; set; }
        public string CommandText { get; set; }
        public DateTime SentTime { get; set; }
        public DateTime? CompletedTime { get; set; }
        public string Status { get; set; } // Pending, Sent, Completed
    }

    public class CommandResult
    {
        public string CommandId { get; set; }
        public string Result { get; set; }
        public bool IsError { get; set; }
        public DateTime ReceivedTime { get; set; }
        public Dictionary<int, string> PartialResults { get; set; } // Para almacenar fragmentos
        public string ScreenshotPath { get; internal set; }
        public DateTime Timestamp { get; internal set; }
    }

    // En ClientModel.cs, agregar esta clase
    public class FileTransferCommand
    {
        public string Id { get; set; }
        public string FileName { get; set; }
        public byte[] FileData { get; set; }
        public string DestinationPath { get; set; }
        public DateTime SentTime { get; set; }
        public string Status { get; set; } // Pending, Sent, Completed, Failed
    }

    public class FileSystemEntry
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public string Type { get; set; } // "file" or "directory"
        public long Size { get; set; }
        public DateTime LastModified { get; set; }
    }

    public class ScreenshotViewModel
    {
        public string MachineName { get; set; }
        public List<string> ScreenshotPaths { get; set; }
    }

    public class ScreenshotFrame
    {
        public string FrameId { get; set; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; set; }
        public int FrameNumber { get; set; }

        public string ImageData { get; set; }

    }
}
