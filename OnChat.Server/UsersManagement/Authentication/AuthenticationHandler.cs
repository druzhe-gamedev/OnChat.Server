using DataModel;
using LinqToDB.Async;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OnChat.Configuration;
using OnChat.Connection;
using OnChat.Encryption;
using OnChat.Protocol.PacketHandler;
using OnChat.Protocol.Packets;
using OnChat.Shared.Auth;

namespace OnChat.UsersManagement.Authentication;

public class AuthenticationHandler(
    OnChatDb db,
    TokensService tokensService,
    ConnectionsProvider connectionsProvider,
    IConfiguration configuration,
    ILogger<AuthenticationHandler> logger
) : PacketHandler<AuthenticationPacket>
{
    protected override async Task<IResponse> Handle(AuthenticationPacket packet, IConnection caller)
    {
        logger.LogInformation("Logging in");
        
        User? user = await db.Users.FirstOrDefaultAsync(user => user.Mail == packet.Login || user.Username == packet.Login);
        // todo add validations
        if(user is not null)
        {
            if (user.PasswordHash != HashHelper.GetHash(
                    packet.Password,
                    configuration[ConfigurationConstants.HashSalt]!
                ))
            {
                return new WrongPasswordPacket(packet.CorrelationId, "Wrong password");
            }
            
            TokenModel jwtToken = tokensService.CreateJwtToken(("id", user.Id), ("username", user.Username)!);
            TokenModel refreshToken = tokensService.CreateRefreshToken();
            tokensService.SaveRefreshToken(user.Id, refreshToken.Token);
            caller.Authenticate(user.Id, user.Username!);

            // todo check auth
            Authenticated? authenticated = caller.AuthenticationState as Authenticated;
            connectionsProvider.Users.TryAdd(authenticated!.UserId, authenticated);
            return new TokensPacket(packet.CorrelationId, jwtToken, refreshToken);
        }
        
        return new WrongLoginPacket(packet.CorrelationId, "Wrong login");
    }
}