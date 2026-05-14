using System.Collections.Concurrent;
using OnChat.Protocol.PacketHandler;

namespace OnChat.Connection;

public class ConnectionsProvider
{
    public readonly ConcurrentDictionary<long, ChatConnection> Clients = [];
    // todo log off
    public readonly ConcurrentDictionary<Guid, Authenticated> Users = [];
}