using System.Net.WebSockets;
using System.Text;
using static System.Console;


WriteLine("Starting client...");

CancellationTokenSource cts = new();

using var client = new ClientWebSocket();
await client.ConnectAsync(
    new("ws://localhost:8080/receiver"),
    cts.Token);


WriteLine("Connected to server");


Task.WaitAll(
    Receiver(client, cts),
    Sender(client, cts.Token));

static async Task Receiver(ClientWebSocket client, CancellationTokenSource cts)
{
    var buffer = new byte[1024];
    while(!cts.Token.IsCancellationRequested)
    {
        var result = await client.ReceiveAsync(
            new(buffer),
            cts.Token);

        if(result.MessageType == WebSocketMessageType.Close)
        {
            await client.CloseAsync(
                WebSocketCloseStatus.NormalClosure,
                null,
                cts.Token);

            cts.Cancel();
            break;
        }

        var receivedMessage = Encoding.UTF8.GetString(buffer[..result.Count]);
        WriteLine($"Received: {receivedMessage}");
    }
}

static async Task Sender(ClientWebSocket client, CancellationToken cancellation)
{
    var i = 0;
    while(client.State == WebSocketState.Open)
    {
        var message = $"Hello Server {i++}";

        await client.SendAsync(
            new(Encoding.UTF8.GetBytes(message)),
            WebSocketMessageType.Text,
            true,
            cancellation);

        await Task.Delay(1000, cancellation);
    }
}
