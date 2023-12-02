using System.Diagnostics;
using System.Net;
using System.Net.WebSockets;
using System.Text;

var builder = WebApplication.CreateSlimBuilder(args);

var app = builder.Build();

app.UseWebSockets();


List<WebSocket> connections = [];
app.MapGet("receiver", async context =>
{
    if(!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = (int)HttpStatusCode.UnsupportedMediaType;
        return;
    }

    using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
    connections.Add(webSocket);

    WebSocketReceiveResult result = default!;
    try
    {
        var buffer = new byte[1024];

        do
        {
            result = await webSocket.ReceiveAsync(new(buffer), CancellationToken.None);
            var receivedMessage = Encoding.UTF8.GetString(buffer[..result.Count]);
            Debug.WriteLine($"Received: {receivedMessage}");

            var replyMessage = Encoding.UTF8.GetBytes($"Replay message: {receivedMessage}");

            foreach(var connection in connections)
            {
                await connection.SendAsync(
                    new(replyMessage, 0, replyMessage.Length),
                    result.MessageType,
                    result.EndOfMessage,
                    CancellationToken.None);
            }
        } while(!result.CloseStatus.HasValue);

        Debug.WriteLine("Closing connection");
    }
    catch(Exception ex)
    {
        Debug.WriteLine(ex.Message);
    }
    finally
    {
        connections.Remove(webSocket);
    }

    await webSocket.CloseAsync(result.CloseStatus!.Value, result.CloseStatusDescription, CancellationToken.None);
    connections.Remove(webSocket);
});

await app.RunAsync();
