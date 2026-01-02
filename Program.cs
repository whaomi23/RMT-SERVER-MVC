using System.Net;
using System.Text.Json;

namespace RMT_SERVER_MVC
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Forzamos a usar localhost
            string localIP = "127.0.0.1";

            // Actualizar launchSettings.json con localhost
            UpdateLaunchSettings(localIP);

            var builder = WebApplication.CreateBuilder(args);

            // Configura Kestrel para escuchar solo en localhost
            builder.WebHost.ConfigureKestrel(serverOptions =>
            {
                serverOptions.Listen(IPAddress.Loopback, 5062); // HTTP (localhost)
                serverOptions.Listen(IPAddress.Loopback, 7277, listenOptions =>
                {
                    listenOptions.UseHttps(); // HTTPS (localhost)
                });
            });

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Clients}/{action=Index}/{id?}");

            app.Run();
        }

        private static void UpdateLaunchSettings(string localIP)
        {
            try
            {
                string launchSettingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Properties", "launchSettings.json");
                if (File.Exists(launchSettingsPath))
                {
                    string jsonContent = File.ReadAllText(launchSettingsPath);
                    var launchSettings = JsonSerializer.Deserialize<LaunchSettings>(jsonContent);

                    if (launchSettings != null && launchSettings.profiles != null)
                    {
                        // Actualizar perfiles para usar localhost
                        if (launchSettings.profiles.ContainsKey("http"))
                        {
                            launchSettings.profiles["http"].applicationUrl = "http://localhost:5062";
                        }
                        if (launchSettings.profiles.ContainsKey("https"))
                        {
                            launchSettings.profiles["https"].applicationUrl = "https://localhost:7277;http://localhost:5062";
                        }

                        // Guardar cambios
                        var options = new JsonSerializerOptions { WriteIndented = true };
                        string updatedJson = JsonSerializer.Serialize(launchSettings, options);
                        File.WriteAllText(launchSettingsPath, updatedJson);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al actualizar launchSettings.json: {ex.Message}");
            }
        }
    }

    // Clases para deserializar launchSettings.json
    public class LaunchSettings
    {
        public Dictionary<string, Profile> profiles { get; set; }
    }

    public class Profile
    {
        public string commandName { get; set; }
        public bool dotnetRunMessages { get; set; }
        public bool launchBrowser { get; set; }
        public string applicationUrl { get; set; }
        public Dictionary<string, string> environmentVariables { get; set; }
    }
}