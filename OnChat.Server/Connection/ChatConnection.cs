using System.Net.Sockets;
using System.Reflection;
using OnChat.Protocol;
using OnChat.Protocol.PacketHandler;
using OnChat.Protocol.Packets;
using Serilog;

namespace OnChat.Connection;

public class ChatConnection(long id, TcpClient client, Server server, ConnectionsProvider connectionsProvider) : IConnection, IAsyncDisposable
{
    private readonly NetworkStream _stream = client.GetStream();
    private readonly ILogger _logger = Log.Logger.ForContext<ChatConnection>();
    public AuthenticationState AuthenticationState { get; private set; } = new NotAuthenticated();

    public void Authenticate(Guid userId, string username)
    {
        if (AuthenticationState is not NotAuthenticated)
            return;
        
        AuthenticationState = new Authenticated(userId, username, this);
    }
    
    public async Task Write(IPacket packet)
    {
        try
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
            await ms.CopyToAsync(_stream);}
        catch (Exception e)
        {
            if (AuthenticationState is Authenticated authenticated)
                connectionsProvider.Users.TryRemove(authenticated.UserId, out _);

            connectionsProvider.Clients.TryRemove(id, out _);
            _logger.Error(e.Message);
        }
    }
    
    public async Task Read()
    {
        try
        {
            while (true)
            {
                if (!_stream.CanRead) continue;
                using ProtocolBuffer buffer = await ProtocolBuffer.CreateFromReader(new BinaryReader(_stream));
                PacketId packetId = (PacketId)buffer.Reader.ReadByte();

                if (!server.Protocol.Packets.TryGetValue(packetId, out Type? packetType))
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
        catch (Exception e)
        {
            if (AuthenticationState is Authenticated authenticated)
                connectionsProvider.Users.TryRemove(authenticated.UserId, out _);
            
            connectionsProvider.Clients.TryRemove(id, out _);
            _logger.Error(e.Message);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _stream.DisposeAsync();
    }
}