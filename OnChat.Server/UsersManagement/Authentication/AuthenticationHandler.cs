using DataModel;
using LinqToDB.Async;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OnChat.Configuration;
using OnChat.Encryption;
using OnChat.Protocol.PacketHandler;
using OnChat.Shared.Auth;

namespace OnChat.UsersManagement.Authentication;

public class AuthenticationHandler(
    OnChatDb db,
    JwtTokensService tokensService,
    IConfiguration configuration,
    ILogger<AuthenticationHandler> logger
) : PacketHandler<AuthenticationPacket>
{
    protected override async Task Handle(AuthenticationPacket packet, IConnection caller)
    {
        logger.LogInformation("Logging in");
        
        User? user = await db.Users.FirstOrDefaultAsync(user =>
            (user.Mail == packet.Login || user.Username == packet.Login) && 
            user.PasswordHash == HashHelper.GetHash(packet.Password, configuration[ConfigurationConstants.HashSalt]!)
        );
        
        if(user is not null)
        { 
            TokenModel jwtToken = tokensService.CreateJwtToken(("id", user.Id), ("username", user.Username)!);
            TokenModel refreshToken = tokensService.CreateRefreshToken();
            await caller.Write(new TokensPacket(packet.CorrelationId, jwtToken, refreshToken));
            return;
        }
        
        // todo failure packet
        // caller.Write(new AuthenticationFailurePacket());
    }
}