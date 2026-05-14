using DataModel;
using LinqToDB.Async;
using OnChat.Protocol.PacketHandler;
using OnChat.Protocol.Packets;
using OnChat.Shared.Users;

namespace OnChat.UsersManagement.Fetch;

public class GetUsersModelsHandlers(OnChatDb db) : PacketHandler<GetUsersModelsPacket>
{
    // todo paging
    protected override async Task<IResponse> Handle(GetUsersModelsPacket packet, IConnection caller)
    {
        UserModel[] usersModels = await db.Users
                                          .Where(user => user.PublicKey != null && user.PublicKey.Length != 0)
                                          .Select(user => new UserModel(user.Id, user.Username!, user.PublicKey!)).ToArrayAsync();
        
        return new ReceiveUsersModelsPacket(packet.CorrelationId, usersModels);
    }
}