using DataModel;
using LinqToDB;
using OnChat.Protocol.PacketHandler;
using OnChat.Protocol.Packets;
using OnChat.Shared;
using OnChat.Shared.Encryption;
using OnChat.Shared.Messages;

namespace OnChat.UsersManagement.Authentication;

public class PublicKeyHandler(OnChatDb db, AuthValidationService authService) : PacketHandler<PublicKeyPacket>
{
    protected override async Task<IResponse> Handle(PublicKeyPacket packet, IConnection caller)
    {
        AuthenticationState authState = authService.CheckAuthentication(packet, caller);
        if (authState is not Authenticated authenticated) 
            return new SuccessfulPacket(packet.CorrelationId);

        IQueryable<User> user = db.Users.Where(user => user.Id == authenticated.UserId);
        if (!user.Any())
            return new WrongIdPacket(packet.CorrelationId, "No user with such id");

        await user.Set(u => u.PublicKey, packet.Value).UpdateAsync();
        return new SuccessfulPacket(packet.CorrelationId);
    }
}