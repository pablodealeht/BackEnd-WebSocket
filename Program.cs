using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using BackEnd_WebSocket.Services;


var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseWebSockets();

app.Use(async (context, next) =>
{
    if (context.Request.Path == "/ws")
    {
        if (context.WebSockets.IsWebSocketRequest)
        {
            var webSocket = await context.WebSockets.AcceptWebSocketAsync();
            await Echo(webSocket);
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

static async Task Echo(WebSocket webSocket)
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
                    else if (tipo == "mover" && mensaje.ContainsKey("title") && mensaje.ContainsKey("x") && mensaje.ContainsKey("y"))
                    {
                        var title = mensaje["title"].GetString();
                        if (!string.IsNullOrWhiteSpace(title))
                        {
                            var x = mensaje["x"].GetInt32();
                            var y = mensaje["y"].GetInt32();

                            var ventanas = WindowHelper.GetOpenWindows();
                            var ventana = ventanas.FirstOrDefault(w => w.Title != null &&
                                w.Title.Contains(title, StringComparison.OrdinalIgnoreCase));
                            if (ventana != null)
                            {
                                WindowHelper.MoverVentana(ventana.Handle, x, y);
                                Console.WriteLine($"Ventana movida: {ventana.Title} → ({x}, {y})");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error procesando mensaje JSON: {ex.Message}");
            }



            Console.WriteLine($"Mensaje recibido: {receivedMessage}");

            if (receivedMessage == "obtener-ventanas")
            {
                var ventanas = WindowHelper.GetOpenWindows()
                  .Where(v => v.Title.Contains("Notepad"))
                  .Select(v => new
                  {
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
            //else
            //{
            //    var responseMessage = $"Echo: {receivedMessage}";
            //    var responseBytes = Encoding.UTF8.GetBytes(responseMessage);
            //    await webSocket.SendAsync(
            //        new ArraySegment<byte>(responseBytes),
            //        WebSocketMessageType.Text,
            //        true,
            //        CancellationToken.None);
            //}
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

