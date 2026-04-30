using System.Net.Sockets;
using OnChat.Shared;
using Serilog;

namespace OnChat.Protocol.Connection;

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
            PacketType packetType = (PacketType)buffer.Reader.ReadByte();
            _logger.Information($"packetType: {packetType}");
            /*long bytesAvailable = buffer.Stream.Length - buffer.Stream.Position;
            _logger.Information($"bytes: {bytesAvailable}");*/
            
            /*while (bytesAvailable > 0)
            {*/
                server.Protocol.Handlers[packetType].Handle(buffer);
            /*    bytesAvailable = buffer.Stream.Length - buffer.Stream.Position;
            }*/
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _stream.DisposeAsync();
    }
}