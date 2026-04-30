using System.Collections.Frozen;
using System.Reflection;
using OnChat.PacketHandler;
using OnChat.Shared;
using OnChat.Types;
using Serilog;

namespace OnChat.Protocol;

public class ChatProtocol
{
    private readonly ILogger _logger = Log.Logger.ForContext<ChatProtocol>();
    public readonly FrozenDictionary<PacketType, IPacketHandler> Handlers;
    
    public ChatProtocol()
    {
        Handlers = Assembly.GetExecutingAssembly().DefinedTypes
                               .Where(type =>
                                   type.IsConcrete &&
                                   type.ImplementedInterfaces.Any(i => i.IsAssignableTo(typeof(IPacketHandler)))
                               ).Select(type => (IPacketHandler)Activator.CreateInstance(type)!)
                               .ToDictionary(type => type.PacketType)
                               .ToFrozenDictionary();

        foreach (KeyValuePair<PacketType, IPacketHandler> handler in Handlers)
        {
            _logger.Information($"Register packet handler [{handler.Key}] = {handler.Value.GetType().Name}");
        }
    }
}