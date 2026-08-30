using FirstReg.Data;
using FirstReg.Mobile.Core.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace FirstReg.Mobile.Core.Services;

public interface ITokenService
{
    string GetAccessToken(User user);
}

public class TokenService : ITokenService
{
    private readonly TokenParameters _jwtSettings;

    public TokenService()
    {
        _jwtSettings = TokenParameter;
    }

    public string GetAccessToken(User user)
    {
        var creds = new SigningCredentials(
            _jwtSettings.GetKey(),
            SecurityAlgorithms.HmacSha512Signature
        );

        var claims = new[]
        {
            new Claim(ClaimKeys.UserId, user.Id.ToString()),
            new Claim(ClaimKeys.FullName, user.FullName),
            new Claim(JwtRegisteredClaimNames.Sub, user.UserName ?? ""),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience, // use the tenant
            claims: claims,
            expires: DateTime.UtcNow.AddDays(1),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
    public static TokenParameters TokenParameter => new()
    {
        Secret = "#p@G=cm=g(8@2s,#/>Xm#kN78B2gDVcQZRfrT-yZ/E3=nFk%=;6%n>EqhF4J!6f$pF463UWRGq6rkn3rZ3N5@hu74Mtw>V",
        Issuer = "https://firstregistrars.com",
        Audience = "https://firstregistrars.com"
    };

    public static class ClaimKeys
    {
        public const string UserId = nameof(UserId);
        public const string FullName = nameof(FullName);
    }
}
