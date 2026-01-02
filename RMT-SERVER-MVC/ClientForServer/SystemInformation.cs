using System.Management;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.IO;
using System;

public class SystemInformation
{
    public string MachineName { get; private set; }
    public string IPAddress { get; private set; }
    public string OSVersion { get; private set; }
    public string OSArchitecture { get; private set; }
    public string Antivirus { get; private set; }
    public bool IsVirtualMachine { get; private set; }
    public string Country { get; private set; }
    public string CPU { get; private set; }
    public string RAM { get; private set; }
    public string[] Drives { get; private set; }

    public SystemInformation()
    {
        MachineName = Environment.MachineName;
        IPAddress = GetIPv4Address();
        OSVersion = GetOSVersion();
        OSArchitecture = GetOSArchitecture();
        Antivirus = GetAntivirusInfo();
        IsVirtualMachine = CheckIfVirtualMachine();
        Country = GetCountry();
        CPU = GetCPUInfo();
        RAM = GetRAMInfo();
        Drives = GetDriveInfo();
    }

    private string GetIPv4Address()
    {
        try
        {
            var hostEntry = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in hostEntry.AddressList)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            return "No IPv4 Address";
        }
        catch
        {
            return "Unknown";
        }
    }

    private string GetOSVersion()
    {
        try
        {
            using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem"))
            {
                foreach (ManagementObject obj in searcher.Get())
                {
                    string version = obj["Caption"].ToString();

                    if (version.Contains("Windows 10")) return "Windows 10";
                    if (version.Contains("Windows 11")) return "Windows 11";
                    if (version.Contains("Windows 8.1")) return "Windows 8.1";
                    if (version.Contains("Windows 8")) return "Windows 8";
                    if (version.Contains("Windows 7")) return "Windows 7";

                    return version;
                }
            }
        }
        catch { }

        return Environment.OSVersion.VersionString;
    }

    private string GetOSArchitecture()
    {
        return Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit";
    }

    private string GetAntivirusInfo()
    {
        try
        {
            string scope = @"\\" + Environment.MachineName + @"\root\SecurityCenter2";
            string query = "SELECT * FROM AntivirusProduct";

            using (var searcher = new ManagementObjectSearcher(scope, query))
            {
                var antivirusList = searcher.Get()
                    .Cast<ManagementObject>()
                    .Select(obj => obj["displayName"].ToString())
                    .ToList();

                if (antivirusList.Any())
                    return string.Join(", ", antivirusList);
            }
        }
        catch { }

        return "Unknown";
    }

    private bool CheckIfVirtualMachine()
    {
        try
        {
            using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_ComputerSystem"))
            {
                foreach (ManagementObject obj in searcher.Get())
                {
                    string manufacturer = obj["Manufacturer"]?.ToString() ?? "";
                    string model = obj["Model"]?.ToString() ?? "";

                    if (manufacturer.Contains("Microsoft Corporation") && model.Contains("Virtual"))
                        return true;
                    if (manufacturer.Contains("VMware")) return true;
                    if (model.Contains("VirtualBox")) return true;
                    if (manufacturer.Contains("Xen")) return true;
                }
            }

            if (Environment.GetEnvironmentVariable("VBOX_HWVIRTEX_IGNORE_SVM_IN_USE") != null)
                return true;
        }
        catch { }

        return false;
    }

    private string GetCountry()
    {
        try
        {
            return RegionInfo.CurrentRegion.EnglishName;
        }
        catch
        {
            return "Unknown";
        }
    }

    private string GetCPUInfo()
    {
        try
        {
            using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Processor"))
            {
                foreach (ManagementObject obj in searcher.Get())
                {
                    return obj["Name"].ToString();
                }
            }
        }
        catch { }

        return "Unknown";
    }

    private string GetRAMInfo()
    {
        try
        {
            using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_ComputerSystem"))
            {
                foreach (ManagementObject obj in searcher.Get())
                {
                    double totalBytes = Convert.ToDouble(obj["TotalPhysicalMemory"]);
                    double totalGB = totalBytes / (1024 * 1024 * 1024);
                    return $"{totalGB:0.##} GB";
                }
            }
        }
        catch { }

        return "Unknown";
    }

    private string[] GetDriveInfo()
    {
        try
        {
            return DriveInfo.GetDrives()
                .Where(d => d.IsReady)
                .Select(d => $"{d.Name} {d.DriveType} {d.TotalFreeSpace / 1024 / 1024 / 1024:0.##}GB free")
                .ToArray();
        }
        catch
        {
            return new[] { "Unknown" };
        }
    }
}