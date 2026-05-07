using DataModel;
using LinqToDB;
using LinqToDB.Async;
using Microsoft.Extensions.Configuration;
using OnChat.Configuration;
using OnChat.Encryption;
using OnChat.Protocol.PacketHandler;
using OnChat.Shared.Auth;

namespace OnChat.UsersManagement.Registration;

public class RegistrationHandler(OnChatDb db, IConfiguration configuration/*, JwtTokensService tokensService*/) : PacketHandler<RegistrationPacket>
{
    protected override async Task Handle(RegistrationPacket packet, IConnection caller)
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