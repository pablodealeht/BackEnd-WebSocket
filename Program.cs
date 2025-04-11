// Program.cs actualizado para leer el JWT desde la query string en WebSocket

using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using BackEnd_WebSocket.Data;
using BackEnd_WebSocket.Models;
using BackEnd_WebSocket.Services;

var builder = WebApplication.CreateBuilder(args);

// Config JWT desde appsettings.json
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSettings["Key"];

// 1. Configuración de la base de datos
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer("Server=localhost\\SQLEXPRESS;Database=VentanasDb;Trusted_Connection=True;TrustServerCertificate=True;"));

// 2. Configuración de Identity + JWT
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!))
        };

        // *** Esta parte es fundamental para WebSocket ***
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;

                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/ws"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();  
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:4200") //  URL del frontend Angular
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


app.UseWebSockets();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

// Middleware para manejar WebSocket y chequear el JWT
app.Use(async (context, next) =>
{
    if (context.Request.Path == "/ws")
    {
        var authResult = await context.AuthenticateAsync(JwtBearerDefaults.AuthenticationScheme);
        if (!authResult.Succeeded || !authResult.Principal.Identity.IsAuthenticated)
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Unauthorized");
            return;
        }

        if (context.WebSockets.IsWebSocketRequest)
        {
            var webSocket = await context.WebSockets.AcceptWebSocketAsync();
            await Echo(webSocket, app.Services);
        }
        else
        {
            context.Response.StatusCode = 400;
        }
    }
    else
    {
        await next();
    }
});
app.MapControllers();

app.Run();

// Función utilitaria para obtener una ventana desde el sistema por handle
static (string title, int x, int y, int width, int height) GetVentanaPorHandle(long hWndValue)
{
    var todas = WindowHelper.GetOpenWindows();
    var ventana = todas.FirstOrDefault(v => v.Handle.ToInt64() == hWndValue);
    return (
        ventana?.Title ?? "",
        ventana?.X ?? 0,
        ventana?.Y ?? 0,
        ventana?.Width ?? 0,
        ventana?.Height ?? 0
    );
}

// Función principal que maneja los WebSocket
async Task Echo(WebSocket webSocket, IServiceProvider services)
{
    var buffer = new byte[1024 * 4];
    while (webSocket.State == WebSocketState.Open)
    {
        var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

        if (result.MessageType == WebSocketMessageType.Text)
        {
            var receivedMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);

            try
            {
                var mensaje = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(receivedMessage);

                if (mensaje != null && mensaje.ContainsKey("tipo"))
                {
                    string? tipo = mensaje["tipo"].GetString();

                    if (tipo == "cerrar" && mensaje.ContainsKey("title"))
                    {
                        var title = mensaje["title"].GetString();
                        if (!string.IsNullOrWhiteSpace(title))
                        {
                            var ventanas = WindowHelper.GetOpenWindows();
                            var ventana = ventanas.FirstOrDefault(w => w.Title != null &&
                                w.Title.Contains(title, StringComparison.OrdinalIgnoreCase));
                            if (ventana != null)
                            {
                                WindowHelper.CerrarVentana(ventana.Handle);
                                Console.WriteLine($" Ventana cerrada: {ventana.Title}");
                            }
                        }
                    }
                    else if (tipo == "mover" && mensaje.ContainsKey("handle") && mensaje.ContainsKey("x") && mensaje.ContainsKey("y"))
                    {
                        if (mensaje["handle"].TryGetInt64(out long hWndValue))
                        {
                            int x = mensaje["x"].GetInt32();
                            int y = mensaje["y"].GetInt32();

                            IntPtr hWnd = new IntPtr(hWndValue);
                            WindowHelper.MoverVentana(hWnd, x, y);
                            Console.WriteLine($" Ventana movida: {hWnd} → ({x}, {y})");

                            using var scope = services.CreateScope();
                            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                            var (titulo, _, _, ancho, alto) = GetVentanaPorHandle(hWndValue);

                            var ventanaDb = await db.Ventanas.FindAsync(hWndValue);
                            if (ventanaDb == null)
                            {
                                ventanaDb = new VentanaDb
                                {
                                    Handle = hWndValue,
                                    Title = titulo,
                                    X = x,
                                    Y = y,
                                    Width = ancho,
                                    Height = alto,
                                    UltimaActualizacion = DateTime.Now
                                };
                                db.Ventanas.Add(ventanaDb);
                            }
                            else
                            {
                                ventanaDb.X = x;
                                ventanaDb.Y = y;
                                ventanaDb.UltimaActualizacion = DateTime.Now;
                            }

                            await db.SaveChangesAsync();
                        }
                        else
                        {
                            Console.WriteLine(" No se pudo parsear el handle para mover.");
                        }
                    }
                    else if (tipo == "resize" && mensaje.ContainsKey("handle") && mensaje.ContainsKey("width") && mensaje.ContainsKey("height"))
                    {
                        if (mensaje["handle"].TryGetInt64(out long hWndValue))
                        {
                            int width = mensaje["width"].GetInt32();
                            int height = mensaje["height"].GetInt32();

                            IntPtr hWnd = new IntPtr(hWndValue);
                            WindowHelper.RedimensionarVentana(hWnd, width, height);
                            Console.WriteLine($" Ventana redimensionada: {hWnd} → ({width} x {height})");

                            using var scope = services.CreateScope();
                            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                            var (_, x, y, _, _) = GetVentanaPorHandle(hWndValue);

                            var ventanaDb = await db.Ventanas.FindAsync(hWndValue);
                            if (ventanaDb == null)
                            {
                                ventanaDb = new VentanaDb
                                {
                                    Handle = hWndValue,
                                    Title = "",
                                    Width = width,
                                    Height = height,
                                    X = x,
                                    Y = y,
                                    UltimaActualizacion = DateTime.Now
                                };
                                db.Ventanas.Add(ventanaDb);
                            }
                            else
                            {
                                ventanaDb.Width = width;
                                ventanaDb.Height = height;
                                ventanaDb.UltimaActualizacion = DateTime.Now;
                            }

                            await db.SaveChangesAsync();
                        }
                        else
                        {
                            Console.WriteLine(" No se pudo parsear el handle para resize.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($" Error procesando JSON: {ex.Message}");
            }

            Console.WriteLine($" Mensaje recibido: {receivedMessage}");

            // Lanzar notepads
            if (receivedMessage == "iniciar")
            {
                ProcessLauncher.AbrirNotepads(2);
                Console.WriteLine("Se iniciaron 2 instancias de Notepad.");
            }

            // Enviar lista de ventanas activas
            if (receivedMessage == "obtener-ventanas")
            {
                //var ventanas = WindowHelper.GetOpenWindows()
                //    .Where(v => v.Title != null && (
                //        v.Title.Contains("Notepad", StringComparison.OrdinalIgnoreCase) ||
                //        v.Title.Contains("Notas de texto", StringComparison.OrdinalIgnoreCase) ||
                //        v.Title.Contains("Bloc de notas", StringComparison.OrdinalIgnoreCase)))
                //    .Select(v => new
                //    {
                //        Handle = v.Handle.ToInt64(),
                //        v.Title,
                //        v.X,
                //        v.Y,
                //        v.Width,
                //        v.Height
                //    })
                //    .ToList();

                //var resolucion = ScreenHelper.GetScreenResolution();

                //var payload = new
                //{
                //    pantalla = new { width = resolucion.Width, height = resolucion.Height },
                //    ventanas = ventanas
                //};

                //var json = JsonSerializer.Serialize(payload);
                //await webSocket.SendAsync(
                //    new ArraySegment<byte>(Encoding.UTF8.GetBytes(json)),
                //    WebSocketMessageType.Text,
                //    true,
                //    CancellationToken.None);
                using var scope = services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var ventanasSistema = WindowHelper.GetOpenWindows()
                    .Where(v => v.Title != null && (
                        v.Title.Contains("Notepad", StringComparison.OrdinalIgnoreCase) ||
                        v.Title.Contains("Notas de texto", StringComparison.OrdinalIgnoreCase) ||
                        v.Title.Contains("Bloc de notas", StringComparison.OrdinalIgnoreCase)))
                    .ToList();

                var handles = ventanasSistema.Select(v => v.Handle.ToInt64()).ToList();
                var estadosGuardados = db.Ventanas
                    .Where(v => handles.Contains(v.Handle))
                    .ToDictionary(v => v.Handle);

                // ✅ FORZAMOS mover y redimensionar con el estado guardado
                foreach (var v in ventanasSistema)
                {
                    var handle = v.Handle.ToInt64();
                    if (estadosGuardados.TryGetValue(handle, out var guardada))
                    {
                        Console.WriteLine($"📌 Reposicionando ventana {v.Title} ({handle}) a ({guardada.X},{guardada.Y}) tamaño ({guardada.Width}x{guardada.Height})");
                        WindowHelper.MoverVentana(v.Handle, guardada.X, guardada.Y);
                        WindowHelper.RedimensionarVentana(v.Handle, guardada.Width, guardada.Height);
                    }
                }

                var ventanasFinales = ventanasSistema.Select(v =>
                {
                    var handle = v.Handle.ToInt64();
                    if (estadosGuardados.TryGetValue(handle, out var guardada))
                    {
                        return new
                        {
                            Handle = handle,
                            v.Title,
                            X = guardada.X,
                            Y = guardada.Y,
                            Width = guardada.Width,
                            Height = guardada.Height
                        };
                    }
                    else
                    {
                        return new
                        {
                            Handle = handle,
                            v.Title,
                            v.X,
                            v.Y,
                            v.Width,
                            v.Height
                        };
                    }
                }).ToList();

                var resolucion = ScreenHelper.GetScreenResolution();

                var payload = new
                {
                    pantalla = new { width = resolucion.Width, height = resolucion.Height },
                    ventanas = ventanasFinales
                };

                var json = JsonSerializer.Serialize(payload);
                await webSocket.SendAsync(
                    new ArraySegment<byte>(Encoding.UTF8.GetBytes(json)),
                    WebSocketMessageType.Text,
                    true,
                    CancellationToken.None);

            }
            else
            {
                var responseMessage = JsonSerializer.Serialize(new { status = "ok" });
                var responseBytes = Encoding.UTF8.GetBytes(responseMessage);
                await webSocket.SendAsync(
                    new ArraySegment<byte>(responseBytes),
                    WebSocketMessageType.Text,
                    true,
                    CancellationToken.None);
            }
        }
        else if (result.MessageType == WebSocketMessageType.Close)
        {
            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Cierre solicitado", CancellationToken.None);
        }
    }
}
