using System.Security.Cryptography;
using JWT;
using JWT.Algorithms;
using JWT.Builder;
using JWT.Exceptions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OnChat.Configuration;
using OnChat.Shared.Auth;

namespace OnChat.Encryption;

public class TokensService(IConfiguration configuration, IMemoryCache memoryCache, ILogger<TokensService> logger)
{
    private readonly TimeSpan _refreshTokenExpiration = TimeSpan.FromMinutes(30);
    private readonly TimeSpan _accessTokenExpiration = TimeSpan.FromMinutes(5);
    private readonly string _secret = configuration[ConfigurationConstants.Secret]!;

    private readonly ValidationParameters _validationParameters = new()
                                                                  {
                                                                      ValidateExpirationTime = true,
                                                                      ValidateSignature = true
                                                                  };
    
    public TokenModel CreateJwtToken(params (string, object)[] claims)
    {
        DateTimeOffset expiresAt = DateTimeOffset.UtcNow.Add(_accessTokenExpiration);
        JwtBuilder jwtBuilder = JwtBuilder.Create()
                                          .WithSecret(_secret)
                                          .WithAlgorithm(new HMACSHA256Algorithm())
                                          .WithValidationParameters(_validationParameters)
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

    public void SaveRefreshToken(Guid userId, string refreshToken)
    {
        if(memoryCache.TryGetValue(userId, out _))
            return;

        memoryCache.Set(userId, refreshToken, _refreshTokenExpiration);
    }

    public bool IsRefreshTokenExist(Guid userId) => memoryCache.TryGetValue(userId, out _);

    public bool TryValidateToken(string token, out Dictionary<string, object>? claims)
    {
        claims = null;
        try
        {
            claims = JwtBuilder.Create()
                               .WithAlgorithm(new HMACSHA256Algorithm())
                               .WithValidationParameters(_validationParameters)
                               .WithSecret(_secret)
                               .Decode<Dictionary<string, object>>(token);

            return true;
        }
        catch (TokenExpiredException exception)
        {
            logger.LogInformation($"JWT validation error occured{exception}");
        }
        
        return false;
    }
}