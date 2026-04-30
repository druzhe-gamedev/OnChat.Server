using OnChat.Protocol;
using OnChat.Shared;

namespace OnChat.PacketHandler;

public interface IPacketHandler
{
    PacketType PacketType { get; }
    void Handle(ProtocolBuffer buffer);
}