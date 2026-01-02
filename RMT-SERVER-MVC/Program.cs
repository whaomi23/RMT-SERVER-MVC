//using System.Net;
//using Microsoft.AspNetCore.Antiforgery;
//using Microsoft.AspNetCore.HttpOverrides;
//using Microsoft.AspNetCore.Mvc;
//using System.Text.Json;

//var builder = WebApplication.CreateBuilder(args);

//// 1. Configuración del servidor Kestrel
//builder.WebHost.ConfigureKestrel(serverOptions =>
//{
//    serverOptions.Listen(IPAddress.Loopback, 8080); // Puerto HTTP para Tor
//});

//// 2. Configuración de servicios esenciales
//builder.Services.AddControllersWithViews(options =>
//{
//    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
//});

//builder.Services.AddAntiforgery(options =>
//{
//    options.Cookie.Name = "__Secure-CsrfToken";
//    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
//    options.HeaderName = "X-CSRF-TOKEN";
//});

//builder.Services.AddSession(options =>
//{
//    options.Cookie.Name = "__Secure-Session";
//    options.Cookie.HttpOnly = true;
//    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
//    options.IdleTimeout = TimeSpan.FromMinutes(20);
//});

//builder.Services.Configure<ForwardedHeadersOptions>(options =>
//{
//    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
//});

//var app = builder.Build();

//// 3. Middleware de seguridad consolidado
//app.Use(async (context, next) =>
//{
//    // Validación estricta del dominio .onion
//    if (!context.Request.Host.Host.EndsWith(".onion", StringComparison.OrdinalIgnoreCase))
//    {
//        context.Response.StatusCode = StatusCodes.Status403Forbidden;
//        await context.Response.WriteAsync("Acceso exclusivo a través de la red Tor");
//        return;
//    }

//    // Headers de seguridad reforzados
//    context.Response.Headers.Append("Content-Security-Policy",
//        "default-src 'self'; " +
//        "script-src 'self' 'unsafe-inline'; " +
//        "style-src 'self' 'unsafe-inline'; " +
//        "img-src 'self' data:; " +
//        "connect-src 'self'; " +
//        "frame-ancestors 'none';");

//    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
//    context.Response.Headers.Append("X-Frame-Options", "DENY");
//    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
//    context.Response.Headers.Append("Referrer-Policy", "no-referrer");
//    context.Response.Headers.Append("Permissions-Policy", "geolocation=(), microphone=(), camera=()");

//    // Ocultar información del servidor
//    context.Response.Headers.Remove("Server");
//    context.Response.Headers.Remove("X-Powered-By");

//    await next();
//});

//app.UseForwardedHeaders();
//app.UseStaticFiles(new StaticFileOptions
//{
//    ServeUnknownFileTypes = false,
//    DefaultContentType = "text/plain"
//});

//app.UseRouting();
//app.UseSession();
//app.UseAuthorization();

//app.MapControllerRoute(
//    name: "default",
//    pattern: "{controller=Clients}/{action=Index}/{id?}");

//app.Run();

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
                serverOptions.Listen(IPAddress.Loopback, 8080, listenOptions =>
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