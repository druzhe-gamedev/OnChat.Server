using DataModel;
using LinqToDB;
using Microsoft.Extensions.Logging;
using OnChat.Connection;
using OnChat.Protocol.PacketHandler;
using OnChat.Shared;
using OnChat.Shared.Encryption;
using OnChat.Shared.Messages;
using OnChat.UsersManagement.Authentication;

namespace OnChat.Messaging;

public class MessagePacketHandler(
    AuthValidationService authService,
    OnChatDb db,
    ConnectionsProvider connectionsProvider,
    ILogger<MessagePacketHandler> logger
) : PacketHandler<SendMessagePacket>
{
    protected override async Task Handle(SendMessagePacket packet, IConnection caller)
    {
        AuthenticationState authState = await authService.CheckAuthentication(packet, caller);
        if (authState is not Authenticated callerAuthenticated) 
            return;
        
        logger.LogInformation($"[{callerAuthenticated.UserId}] Enter {nameof(MessagePacketHandler)}");
        
        if (!await TrySaveMessage(callerAuthenticated.UserId, packet))
        {
            await caller.Write(new WrongIdPacket(packet.CorrelationId, "No user with such id"));
            logger.LogInformation($"[{callerAuthenticated.UserId}] No user with such id");
            return;
        }
        
        if (!connectionsProvider.Users.TryGetValue(packet.ReceiverId, out Authenticated? authenticated))
        {
            await caller.Write(new SuccessfulPacket(packet.CorrelationId));
            return;
        }

        await authenticated.Connection.Write(
            new ReceiveMessagePacket(packet.CorrelationId, callerAuthenticated.UserId, packet.EncryptedMessage)
        );

        await caller.Write(new SuccessfulPacket(packet.CorrelationId));
    }

    private async Task<bool> TrySaveMessage(Guid senderId, SendMessagePacket packet)
    {
        User? receiver = await db.Users.FindAsync(packet.ReceiverId);
        User? sender = await db.Users.FindAsync(senderId);

        if (receiver == null || sender == null)
            return false;

        EncryptedMessage message = packet.EncryptedMessage;
        await db.InsertAsync(new Message
                             {
                                 EphemeralPublicKey = message.EphemeralPublicKey,
                                 Nonce = message.Nonce,
                                 Ciphertext = message.Ciphertext,
                                 Tag = message.Tag,
                                 SenderId = senderId,
                                 ReceiverId = receiver.Id
                             });
        return true;
    }
}