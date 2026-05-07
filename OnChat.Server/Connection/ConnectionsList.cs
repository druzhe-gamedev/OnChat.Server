using System.Collections.Concurrent;

namespace OnChat.Connection;

public class ConnectionsList
{
    public readonly ConcurrentDictionary<long, ChatConnection> Clients = new ();
}