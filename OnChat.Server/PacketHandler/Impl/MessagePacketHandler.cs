using OnChat.Protocol;
using OnChat.Shared;
using Serilog;

namespace OnChat.PacketHandler.Impl;

public class MessagePacketHandler : IPacketHandler
{
    public PacketType PacketType => PacketType.MessagePacket;
    private readonly ILogger _logger = Log.Logger.ForContext<MessagePacketHandler>();
    
    public void Handle(ProtocolBuffer buffer)
    {
        MessagePacket packet = new ();
        packet.Deserialize(buffer.Reader);
        
        _logger.Information($"{packet.UserId} {packet.Message}");
    }
}