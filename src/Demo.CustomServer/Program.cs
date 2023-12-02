using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using static System.Console;

WriteLine("Starting server...");

var ip = new IPEndPoint(IPAddress.Loopback, 8080);
var listener = new Socket(SocketType.Stream, ProtocolType.Tcp);
listener.Bind(ip);
listener.Listen();
const string EOL = "\r\n";

WriteLine("Server started, listening on {0}", ip);

while(true)
{
    var socket = await listener.AcceptAsync();
    var stream = new NetworkStream(socket);
    var buffer = new byte[1024];

    while(true)
    {
        var bytesRead = await stream.ReadAsync(buffer);

        var data = Encoding.UTF8.GetString(buffer[..bytesRead]);
        if(data.StartsWith("GET"))
        {
            // handshake https://developer.mozilla.org/en-US/docs/Web/API/WebSockets_API/Writing_WebSocket_servers#the_websocket_handshake
            WriteLine(data);

            var webSocketKey = _webSocketKeyRegex().Match(data).Groups[1].Value;

            var acceptWebsocket = Convert.ToBase64String(SHA1.HashData(Encoding.UTF8.GetBytes($"{webSocketKey.Trim()}258EAFA5-E914-47DA-95CA-C5AB0DC85B11")));
            var responseMessage = "HTTP/1.1 101 Switching Protocols" + EOL
                                + "Connection: Upgrade" + EOL
                                + "Upgrade: websocket" + EOL
                                + "Sec-WebSocket-Accept: " + acceptWebsocket + EOL + EOL;

            var response = Encoding.UTF8.GetBytes(responseMessage);
            await stream.WriteAsync(response);
            stream.Flush();
        }
        else
        {
            // https://developer.mozilla.org/en-US/docs/Web/API/WebSockets_API/Writing_WebSocket_servers#format
            // Only for small messages, < 126 bytes

            var length = buffer[1] - 0x80;
            var key = buffer[2..6];
            var webSockeData = new byte[length];

            for(var i = 0; i < length; i++)
            {
                var offset = 6 + i;
                webSockeData[i] = (byte)(buffer[offset] ^ key[i % 4]);
            }

            WriteLine(Encoding.UTF8.GetString(webSockeData));
        }
    }
}

internal partial class Program
{
    [GeneratedRegex("Sec-WebSocket-Key: (.*)")]
    private static partial Regex _webSocketKeyRegex();
}
