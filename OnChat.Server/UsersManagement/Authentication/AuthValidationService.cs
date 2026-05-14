using OnChat.Encryption;
using OnChat.Protocol.PacketHandler;
using OnChat.Shared;
using OnChat.Shared.Auth;

namespace OnChat.UsersManagement.Authentication;

public class AuthValidationService(TokensService tokensService)
{
    public async Task<AuthenticationState> CheckAuthentication(AuthenticatedPacket packet, IConnection connection)
    {
        if (tokensService.TryValidateToken(packet.Token, out _) &&
            connection.AuthenticationState is Authenticated authenticated) 
            return authenticated;
        
        await connection.Write(new UnauthorizedPacket(packet.CorrelationId, "Unauthorized"));
        return new NotAuthenticated();
    }
}