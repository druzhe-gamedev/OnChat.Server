using OnChat.Protocol.PacketHandler;
using OnChat.Protocol.Packets;
using OnChat.Shared;
using OnChat.Shared.Validation;

namespace OnChat.Common.Packet;

public abstract class ValidationHandlerBase<T>(Server server, IValidator<T> validator) : PacketHandler<T> where T : BasePacket
{
    protected override async Task Handle(T packet, IConnection caller)
    {
        if (validator.Validate(packet) is ValidationResultError<PacketId> error)
        {
            if (!server.Protocol.Packets.TryGetValue(error.ErrorCode, out Type? packetType) ||
                packetType.BaseType != typeof(FailureResponse))
                return;

            FailureResponse failureResponse = (FailureResponse)Activator.CreateInstance(
                packetType,
                packet.CorrelationId,
                error.Description
            )!;

            
            await caller.Write(failureResponse);
            return;
        }

        await PacketHandle(packet, caller);
    }
    
    protected virtual Task PacketHandle(T packet, IConnection caller)
    {
        return Task.CompletedTask;
    }
}