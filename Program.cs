using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using BackEnd_WebSocket.Services;
using BackEnd_WebSocket.Data;
using Microsoft.EntityFrameworkCore;
using BackEnd_WebSocket.Models;



var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer("Server=localhost\\SQLEXPRESS;Database=VentanasDb;Trusted_Connection=True;TrustServerCertificate=True;"));


var app = builder.Build();

app.UseWebSockets();

app.Use(async (context, next) =>
{
    if (context.Request.Path == "/ws")
    {
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

app.Run();


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
                                Console.WriteLine($"Ventana cerrada: {ventana.Title}");
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
                            Console.WriteLine($"✅ Ventana movida por handle: {hWnd} → ({x}, {y})");

                            //using var scope = services.CreateScope();
                            //var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                            //var ventanaDb = await db.Ventanas.FindAsync(hWndValue);
                            //if (ventanaDb == null)
                            //{
                            //    ventanaDb = new VentanaDb
                            //    {
                            //        Handle = hWndValue,
                            //        Title = "", 
                            //        X = x,
                            //        Y = y,
                            //        Width = 0,
                            //        Height = 0,
                            //        UltimaActualizacion = DateTime.Now
                            //    };
                            using var scope = services.CreateScope();
                            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                            // Obtenemos el título real y dimensiones desde el sistema
                            var todasLasVentanas = WindowHelper.GetOpenWindows();
                            var ventanaActual = todasLasVentanas.FirstOrDefault(v => v.Handle.ToInt64() == hWndValue);

                            string titulo = ventanaActual?.Title ?? "";
                            int ancho = ventanaActual?.Width ?? 0;
                            int alto = ventanaActual?.Height ?? 0;

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
                            Console.WriteLine($"✔️ Estado guardado para ventana {hWndValue}");


                        }
                        else
                        {
                            Console.WriteLine("❌ No se pudo parsear el handle.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error procesando mensaje JSON: {ex.Message}");
            }

            Console.WriteLine($"Mensaje recibido: {receivedMessage}");

            if (receivedMessage == "iniciar")
            {
                ProcessLauncher.AbrirNotepads(2);
                Console.WriteLine("Se iniciaron 2 instancias de Notepad.");
            }

            if (receivedMessage == "obtener-ventanas")
            {
                var ventanas = WindowHelper.GetOpenWindows()
                    .Where(v => v.Title != null && (
                        v.Title.Contains("Notepad", StringComparison.OrdinalIgnoreCase) ||
                        v.Title.Contains("Notas de texto", StringComparison.OrdinalIgnoreCase) ||
                        v.Title.Contains("Bloc de notas", StringComparison.OrdinalIgnoreCase)))
                    .Select(v => new
                    {
                        Handle = v.Handle.ToInt64(),
                        v.Title,
                        v.X,
                        v.Y,
                        v.Width,
                        v.Height
                    })
                    .ToList();

                var resolucion = ScreenHelper.GetScreenResolution();

                var payload = new
                {
                    pantalla = new { width = resolucion.Width, height = resolucion.Height },
                    ventanas = ventanas
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


