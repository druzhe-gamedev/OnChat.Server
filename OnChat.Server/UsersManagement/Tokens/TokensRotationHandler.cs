using OnChat.Encryption;
using OnChat.Protocol.PacketHandler;
using OnChat.Protocol.Packets;
using OnChat.Shared.Auth;

namespace OnChat.UsersManagement.Tokens;

public class TokensRotationHandler(TokensService tokensService) : PacketHandler<TokensRotationPacket>
{
    protected override async Task<IResponse> Handle(TokensRotationPacket packet, IConnection caller)
    {
        if (caller.AuthenticationState is not Authenticated authenticated)
            return new UnauthorizedPacket(packet.CorrelationId, "Unauthorized");

        if (!tokensService.IsRefreshTokenExist(authenticated.UserId))
            return new UnauthorizedPacket(packet.CorrelationId, "Refresh token has been expired");

        TokenModel jwtToken = tokensService.CreateJwtToken(("id", authenticated.UserId), ("username", authenticated.Username));
        TokenModel refreshToken = tokensService.CreateRefreshToken();
        tokensService.SaveRefreshToken(authenticated.UserId, refreshToken.Token);
        return new TokensPacket(packet.CorrelationId, jwtToken, refreshToken);
    }
}