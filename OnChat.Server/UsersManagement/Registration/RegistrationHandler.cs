using DataModel;
using LinqToDB;
using LinqToDB.Async;
using Microsoft.Extensions.Configuration;
using OnChat.Common.Packet;
using OnChat.Configuration;
using OnChat.Encryption;
using OnChat.Protocol.PacketHandler;
using OnChat.Shared.Auth;
using OnChat.Shared.Auth.Validation;

namespace OnChat.UsersManagement.Registration;

public class RegistrationHandler(Server server, OnChatDb db, IConfiguration configuration)
    : ValidationHandlerBase<RegistrationPacket>(server, new RegistrationValidator())
{
    protected override async Task PacketHandle(RegistrationPacket packet, IConnection caller)
    {
        User? user =
            await db.Users.FirstOrDefaultAsync(user => user.Username == packet.Username || user.Mail == packet.Email);
        
        if (user is null)
        {
            RegistrationSuccessfulResponse response = new(packet.CorrelationId, "Success");

            string passwordHash = HashHelper.GetHash(packet.Password, configuration[ConfigurationConstants.HashSalt]);
            await db.InsertAsync(new User
                                 {
                                     Username = packet.Username,
                                     Mail = packet.Email,
                                     Age = packet.Age,
                                     PasswordHash = passwordHash
                                 });
            
            await caller.Write(response);
            return;
        }

        await caller.Write(new RegistrationFailureResponse(packet.CorrelationId, "User already exists"));
    }
}