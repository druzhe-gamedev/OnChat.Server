using DataModel;
using LinqToDB;
using Microsoft.Extensions.Logging;
using OnChat.Connection;
using OnChat.Protocol.PacketHandler;
using OnChat.Protocol.Packets;
using OnChat.Shared;
using OnChat.Shared.Auth;
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
    protected override async Task<IResponse> Handle(SendMessagePacket packet, IConnection caller)
    {
        AuthenticationState authState = authService.CheckAuthentication(packet, caller);
        if (authState is not Authenticated callerAuthenticated) 
            return new UnauthorizedPacket(packet.CorrelationId, "Unauthorized");
        
        logger.LogInformation($"[{callerAuthenticated.UserId}] Enter {nameof(MessagePacketHandler)}");
        
        if (!await TrySaveMessage(callerAuthenticated.UserId, packet))
        {
            logger.LogInformation($"[{callerAuthenticated.UserId}] No user with such id");
            return new WrongIdPacket(packet.CorrelationId, "No user with such id");
        }
        
        if (!connectionsProvider.Users.TryGetValue(packet.ReceiverId, out Authenticated? authenticated))
            return new SuccessfulPacket(packet.CorrelationId);

        await authenticated.Connection.Write(
            new ReceiveMessagePacket(packet.CorrelationId, callerAuthenticated.UserId, packet.EncryptedMessage)
        );

        return new SuccessfulPacket(packet.CorrelationId);
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