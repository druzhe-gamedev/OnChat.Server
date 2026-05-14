using OnChat.Encryption;
using OnChat.Protocol.PacketHandler;
using OnChat.Shared.Auth;

namespace OnChat.UsersManagement.Tokens;

public class TokensRotationHandler(TokensService tokensService) : PacketHandler<TokensRotationPacket>
{
    protected override async Task Handle(TokensRotationPacket packet, IConnection caller)
    {
        if (caller.AuthenticationState is not Authenticated authenticated)
        {
            await caller.Write(new UnauthorizedPacket(packet.CorrelationId, "Unauthorized"));
            return;
        }

        if (!tokensService.IsRefreshTokenExist(authenticated.UserId))
        {
            await caller.Write(new UnauthorizedPacket(packet.CorrelationId, "Refresh token has been expired"));
            return;
        }

        TokenModel jwtToken = tokensService.CreateJwtToken(("id", authenticated.UserId), ("username", authenticated.Username));
        TokenModel refreshToken = tokensService.CreateRefreshToken();
        tokensService.SaveRefreshToken(authenticated.UserId, refreshToken.Token);
        await caller.Write(new TokensPacket(packet.CorrelationId, jwtToken, refreshToken));
    }
}