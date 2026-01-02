# RMT - Remote Management Tool

Sistema de administración remota desarrollado en C# que permite monitorear y controlar máquinas Windows de forma remota a través de una interfaz web.

## 📋 Descripción

RMT (Remote Management Tool) es una herramienta de administración remota que consta de dos componentes principales:

- **Servidor Web (RMT-SERVER-MVC)**: Aplicación ASP.NET Core MVC que proporciona una interfaz web para gestionar y monitorear clientes conectados.
- **Cliente (RMT-CLIENT-MVC)**: Aplicación de consola que se ejecuta en las máquinas remotas y se conecta al servidor para recibir comandos y enviar información.

## 🚀 Características Principales

### Monitoreo y Visualización
- **Captura de pantalla**: Captura y visualización de pantallas de clientes remotos
- **Streaming en tiempo real**: Transmisión de video en tiempo real (VNC) con soporte para múltiples frames
- **Información del sistema**: Recopilación automática de información del sistema (OS, CPU, RAM, antivirus, etc.)
- **Estado de conexión**: Monitoreo del estado de conexión de los clientes

### Control Remoto
- **Ejecución de comandos**: Ejecución remota de comandos del sistema operativo
- **Explorador de archivos**: Navegación y gestión de archivos remotos
- **Transferencia de archivos**: Envío y descarga de archivos entre servidor y cliente
- **Gestión del sistema**: Control de apagado, reinicio, bloqueo de estación de trabajo

### Funcionalidades Avanzadas
- **Gestión de firewall**: Consulta y control del firewall de Windows
- **Gestión de UAC**: Verificación y elevación de privilegios UAC
- **Gestión de WiFi**: Obtención de contraseñas de redes WiFi almacenadas
- **Escaneo de red**: Escaneo de la red local
- **Monitoreo continuo**: Sistema de monitoreo configurable con intervalos y calidad personalizables

## 🛠️ Tecnologías Utilizadas

### Servidor
- **.NET 8.0**
- **ASP.NET Core MVC**
- **Kestrel Web Server**
- **SignalR** (para comunicación en tiempo real)

### Cliente
- **.NET Framework 4.7.2**
- **System.Windows.Forms** (para captura de pantalla)
- **Newtonsoft.Json** (para serialización JSON)
- **DotNetTor** (soporte opcional para Tor)
- **Costura.Fody** (para empaquetado de dependencias)

## 📁 Estructura del Proyecto

```
RMT-SERVER-MVC/
├── RMT-SERVER-MVC/          # Servidor web ASP.NET Core
│   ├── Controllers/         # Controladores MVC
│   ├── Models/              # Modelos de datos
│   ├── Views/               # Vistas Razor
│   ├── wwwroot/             # Archivos estáticos
│   └── Program.cs           # Punto de entrada del servidor
│
├── RMT-CLIENT-MVC/          # Cliente de consola
│   ├── Program.cs           # Punto de entrada del cliente
│   ├── SystemInformation.cs # Recopilación de información del sistema
│   ├── RemoteMonitor.cs     # Sistema de monitoreo remoto
│   ├── SystemManager.cs     # Gestión del sistema
│   ├── FirewallManager.cs   # Gestión del firewall
│   ├── WifiManager.cs      # Gestión de WiFi
│   └── ...
│
└── packages/                # Paquetes NuGet locales
```

## 🔧 Requisitos

### Servidor
- .NET 8.0 SDK o superior
- Windows, Linux o macOS
- Puerto 5062 (HTTP) y 8080 (HTTPS) disponibles

### Cliente
- Windows 7 o superior
- .NET Framework 4.7.2 o superior
- Permisos de administrador (para algunas funcionalidades)

## 📦 Instalación

### Servidor

1. Clonar el repositorio:
```bash
git clone <url-del-repositorio>
cd RMT-SERVER-MVC
```

2. Navegar al directorio del servidor:
```bash
cd RMT-SERVER-MVC
```

3. Restaurar dependencias:
```bash
dotnet restore
```

4. Ejecutar el servidor:
```bash
dotnet run
```

El servidor estará disponible en:
- HTTP: `http://localhost:5062`
- HTTPS: `https://localhost:8080`

### Cliente

1. Compilar el proyecto cliente:
```bash
cd RMT-CLIENT-MVC
```

2. Abrir el proyecto en Visual Studio y compilar, o usar MSBuild:
```bash
msbuild RMT-CLIENT-MVC.csproj /p:Configuration=Release
```

3. Ejecutar el cliente:
```bash
.\bin\Release\RMT-CLIENT-MVC.exe
```

**Nota**: Antes de ejecutar el cliente, asegúrate de actualizar la URL del servidor en `Program.cs`:

```csharp
private static string serverURL = "https://127.0.0.1:8080/Clients";
```

## ⚙️ Configuración

### Configuración del Servidor

El servidor se configura principalmente a través de `Program.cs`. Los puertos se configuran en:

```csharp
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Listen(IPAddress.Loopback, 5062); // HTTP
    serverOptions.Listen(IPAddress.Loopback, 8080, listenOptions =>
    {
        listenOptions.UseHttps(); // HTTPS
    });
});
```

### Configuración del Cliente

Edita `Program.cs` en el proyecto cliente para cambiar:
- URL del servidor
- Intervalo de polling (por defecto 5 segundos)
- Configuración de Tor (si se utiliza)

## 🎮 Uso

### Iniciar el Servidor

1. Ejecutar el servidor desde la línea de comandos o Visual Studio
2. Abrir un navegador y navegar a `https://localhost:8080/Clients`
3. La interfaz web mostrará los clientes conectados

### Conectar un Cliente

1. Ejecutar el ejecutable del cliente en la máquina remota
2. El cliente se registrará automáticamente en el servidor
3. Aparecerá en la lista de clientes conectados en la interfaz web

### Comandos Disponibles

#### Comandos del Sistema
- `OFF` - Apagar el sistema
- `RESTART` - Reiniciar el sistema
- `CLOSE-SESSION` - Cerrar sesión del usuario
- `LOCK` - Bloquear la estación de trabajo
- `KILL-CLIENT` - Cerrar el cliente

#### Comandos de Archivos
- `LIST_DIR "ruta"` - Listar directorio (formato JSON)
- `LS_DIR "ruta"` - Listar directorio (formato legible)
- `DOWNLOAD_FILE "ruta"` - Descargar archivo desde el cliente

#### Comandos de Pantalla
- `SCREEN-CAP` - Tomar captura de pantalla
- `VNC-START-SCREEN` - Iniciar streaming de pantalla
- `VNC-STOP-SCREEN` - Detener streaming de pantalla

#### Comandos de Monitoreo
- `MONITOR-START` - Iniciar monitoreo continuo
- `MONITOR-STOP` - Detener monitoreo continuo
- `MONITOR-INTERVAL <ms>` - Configurar intervalo de captura
- `MONITOR-QUALITY <1-100>` - Configurar calidad de imagen

#### Comandos de Red y Seguridad
- `WIFI-GET-PASSWORD` - Obtener contraseñas WiFi almacenadas
- `SCAN-NETWORK` - Escanear la red local
- `FIREWALL-STATUS` - Verificar estado del firewall
- `FIREWALL-ON` - Activar firewall
- `FIREWALL-OFF` - Desactivar firewall
- `UAC-CHECK` - Verificar configuración UAC
- `UAC-ELEVATE` - Elevar privilegios UAC

## 🔒 Seguridad

**⚠️ ADVERTENCIA**: Este software está diseñado para uso en entornos controlados y con autorización explícita. El uso no autorizado de este software puede violar leyes locales e internacionales.

### Consideraciones de Seguridad

- El servidor actualmente acepta conexiones SSL sin validación de certificados (solo para desarrollo)
- En producción, se recomienda:
  - Configurar certificados SSL válidos
  - Implementar autenticación y autorización
  - Usar HTTPS estrictamente
  - Configurar firewall adecuadamente
  - Implementar logging y auditoría

### Soporte para Tor (Opcional)

El cliente incluye soporte para conectarse a través de la red Tor usando `DotNetTor`. Para habilitarlo:

1. Instalar y ejecutar Tor Browser o Tor Service
2. Descomentar las líneas relacionadas con Tor en `Program.cs`
3. Configurar el handler de SocksPort

## 📝 Notas de Desarrollo

- El proyecto utiliza **Costura.Fody** para empaquetar todas las dependencias en un solo ejecutable
- Los resultados de comandos grandes se dividen en fragmentos para evitar problemas de tamaño
- Las capturas de pantalla se almacenan en `wwwroot/Ankle Boots/Clientes/{MachineName}/screenshots/`
- El streaming de pantalla utiliza una cola de frames con límite configurable

## 🐛 Solución de Problemas

### El cliente no se conecta al servidor
- Verificar que el servidor esté ejecutándose
- Verificar la URL del servidor en `Program.cs` del cliente
- Verificar que los puertos no estén bloqueados por el firewall
- Verificar la configuración SSL si se usa HTTPS

### Las capturas de pantalla no se muestran
- Verificar permisos de escritura en el directorio `wwwroot`
- Verificar que el cliente tenga permisos para capturar pantalla
- Revisar los logs del servidor para errores

### Los comandos no se ejecutan
- Verificar que el cliente esté en línea
- Revisar los logs del cliente para errores
- Verificar permisos de administrador si es necesario

## 📄 Licencia

Este proyecto es proporcionado "tal cual" sin garantías. Úsalo bajo tu propia responsabilidad.

## 👥 Contribuciones

Las contribuciones son bienvenidas. Por favor:
1. Fork el proyecto
2. Crea una rama para tu característica
3. Commit tus cambios
4. Push a la rama
5. Abre un Pull Request

## 📧 Contacto

Para preguntas o soporte, por favor abre un issue en el repositorio.

---

**Desarrollado con ❤️ usando C# y ASP.NET Core** 

POC: https://youtube.com/shorts/uX74JGtfTo4?si=usfjljjy3jNKRO8M 

