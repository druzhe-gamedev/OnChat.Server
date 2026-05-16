using DataModel;
using LinqToDB;
using OnChat.Common.IQueryableUtils;
using OnChat.Common.Validation;
using OnChat.Protocol.PacketHandler;
using OnChat.Protocol.Packets;
using OnChat.Shared.Auth;
using OnChat.Shared.Encryption;
using OnChat.Shared.Messages;
using OnChat.Shared.Validation;
using OnChat.UsersManagement.Authentication;

namespace OnChat.Messaging;

public class LoadMessagesHandler(Server server, IValidator<LoadMessagesPacket> validator, AuthValidationService authService, OnChatDb db)
    : ValidationHandlerBase<LoadMessagesPacket>(server, validator)
{
    protected override async Task<IResponse> PacketHandle(LoadMessagesPacket packet, IConnection caller)
    {
        AuthenticationState authState = authService.CheckAuthentication(packet, caller);
        if (authState is not Authenticated authenticated) 
            return new UnauthorizedPacket(packet.CorrelationId, "Unauthorized");

        if (await db.Users.FindAsync(packet.ChatParticipant) == null)
            return new WrongIdPacket(packet.CorrelationId, "No user with such id");

        // todo paging
        EncryptedMessage[] messages = db.Messages
                                        .LoadWith(message => message.RecipientEntries)
                                        .Where(message =>
                                            message.RecipientEntries.Any(recipient => recipient.RecipientId == authenticated.UserId) &&
                                            message.RecipientEntries.Any(recipient => recipient.RecipientId == packet.ChatParticipant)
                                        ).Select(message =>
                                            new EncryptedMessage(
                                                message.SenderId,
                                                message.RecipientEntries.Select(recipient =>
                                                    new Shared.Encryption.RecipientEntry(
                                                        recipient.RecipientId,
                                                        new RecipientWrappedKey(
                                                            recipient.Nonce,
                                                            recipient.Ciphertext,
                                                            recipient.Tag
                                                        )
                                                    )
                                                ).ToArray(),
                                                message.EphemeralPublicKey,
                                                message.Nonce,
                                                message.Ciphertext,
                                                message.Tag,
                                                message.Timestamp
                                            )
                                        ).Page(packet.Page, packet.Quantity).ToArray();

        return new MessagesPacket(packet.CorrelationId, messages);
    }
}