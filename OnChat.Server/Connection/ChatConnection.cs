using System.Net.Sockets;
using OnChat.Protocol;
using Serilog;

namespace OnChat.Connection;

public class ChatConnection(TcpClient client, Server server) : IAsyncDisposable
{
    private readonly NetworkStream _stream = client.GetStream();
    private readonly ILogger _logger = Log.Logger.ForContext<ChatConnection>();

    public async Task ReadLoop()
    {
        while (true)
        {
            if (!_stream.CanRead)
                continue;

            using ProtocolBuffer buffer = await ProtocolBuffer.CreateFromReader(new BinaryReader(_stream));
            PacketId packetType = (PacketId)buffer.Reader.ReadByte();
            
            server.Protocol.Handlers[packetType].Handle(buffer);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _stream.DisposeAsync();
    }
}