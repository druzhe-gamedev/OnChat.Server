using DataModel;
using OnChat.Protocol.PacketHandler;
using OnChat.Protocol.Packets;
using OnChat.Shared.Messages;
using OnChat.Shared.Users;

namespace OnChat.UsersManagement.Fetch;

public class GetUserModelHandler(OnChatDb db) : PacketHandler<GetUserModelPacket>
{
    protected override async Task<IResponse> Handle(GetUserModelPacket packet, IConnection caller)
    {
        User? user = db.Users.FirstOrDefault(user => user.Id == packet.UserId && user.PublicKey != null && user.PublicKey.Length != 0);

        if (user == null)
            return new WrongIdPacket(packet.CorrelationId, "No user with such id");
        
        return new ReceiveUserModelPacket(packet.CorrelationId, new UserModel(user.Id, user.Username, user.PublicKey!));
    }
}