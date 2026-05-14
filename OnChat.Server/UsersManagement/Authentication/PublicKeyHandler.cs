using DataModel;
using LinqToDB;
using OnChat.Protocol.PacketHandler;
using OnChat.Shared;
using OnChat.Shared.Encryption;
using OnChat.Shared.Messages;

namespace OnChat.UsersManagement.Authentication;

public class PublicKeyHandler(OnChatDb db, AuthValidationService authService) : PacketHandler<PublicKeyPacket>
{
    protected override async Task Handle(PublicKeyPacket packet, IConnection caller)
    {
        AuthenticationState authState = await authService.CheckAuthentication(packet, caller);
        if (authState is not Authenticated authenticated) 
            return;

        IQueryable<User> user = db.Users.Where(user => user.Id == authenticated.UserId);
        if (!user.Any())
        {
            await caller.Write(new WrongIdPacket(packet.CorrelationId, "No user with such id"));
            return;
        }

        await user.Set(u => u.PublicKey, packet.Value).UpdateAsync();
        await caller.Write(new SuccessfulPacket(packet.CorrelationId));
    }
}