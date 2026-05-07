using System.Net.Sockets;
using System.Reflection;
using OnChat.Protocol;
using OnChat.Protocol.PacketHandler;
using OnChat.Protocol.Packets;
using Serilog;

namespace OnChat.Connection;

public class ChatConnection(TcpClient client, Server server) : IConnection, IAsyncDisposable
{
    private readonly NetworkStream _stream = client.GetStream();
    private readonly ILogger _logger = Log.Logger.ForContext<ChatConnection>();

    public async Task Write(IPacket packet)
    {
        PacketIdAttribute? attribute = packet.GetType().GetCustomAttribute<PacketIdAttribute>();
        if (attribute == null)
        {
            _logger.Error("No packet id attribute");
            return;
        }

        PacketId packetId = attribute.PacketId;

        MemoryStream ms = new();
        BinaryWriter writer = new(ms);
        
        using ProtocolBuffer buffer = new (new MemoryStream());
        buffer.Writer.Write((byte)packetId);
        server.Protocol
              .GetCodec(packet.GetType())
              .Encode(buffer.Writer, packet);

        await buffer.WrapPacket(writer);

        ms.Seek(0, SeekOrigin.Begin);
        await ms.CopyToAsync(_stream);
    }
    
    public async Task Read()
    {
        while (true)
        {
            if (!_stream.CanRead) continue;
            using ProtocolBuffer buffer = await ProtocolBuffer.CreateFromReader(new BinaryReader(_stream));
            PacketId packetId = (PacketId)buffer.Reader.ReadByte();
            
            if(!server.Protocol.Packets.TryGetValue(packetId, out Type? packetType))
            {
                _logger.Error($"Packet id {packetId} is not registered");
                return;
            }
            
            object packet = server.Protocol.GetCodec(packetType).Decode(buffer.Reader);

            if (packet is IPacket sendable)
                await server.Protocol.Handlers[packet.GetType()].Handle(sendable, this);
            else
                _logger.Error("Packet is malformed");
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _stream.DisposeAsync();
    }
}