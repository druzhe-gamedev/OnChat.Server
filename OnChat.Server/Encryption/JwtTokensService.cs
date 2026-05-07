using System.Security.Cryptography;
using JWT.Algorithms;
using JWT.Builder;
using Microsoft.Extensions.Configuration;
using OnChat.Configuration;
using OnChat.Shared.Auth;

namespace OnChat.Encryption;

public class JwtTokensService(IConfiguration configuration)
{
    private readonly TimeSpan _refreshTokenExpiration = TimeSpan.FromMinutes(30);
    
    public TokenModel CreateJwtToken(params (string, object)[] claims)
    {
        DateTimeOffset expiresAt = DateTimeOffset.UtcNow.AddMinutes(5);
        
        JwtBuilder jwtBuilder = JwtBuilder.Create()
                                          .WithAlgorithm(new RS256Algorithm(RSA.Create(), RSA.Create()))
                                          .WithSecret(configuration[ConfigurationConstants.Secret])
                                          .AddClaim("exp", expiresAt.ToUnixTimeSeconds())
                                          .AddClaims(claims.ToDictionary());

        return new TokenModel(jwtBuilder.Encode(), expiresAt);
    }
    
    public TokenModel CreateRefreshToken()
    {
        DateTimeOffset expiresAt = DateTimeOffset.UtcNow.Add(_refreshTokenExpiration);
        
        using RandomNumberGenerator random = RandomNumberGenerator.Create();
        Span<byte> tokenBytes = stackalloc byte[64];
        random.GetBytes(tokenBytes);
        return new TokenModel(Convert.ToBase64String(tokenBytes), expiresAt);
    }
}