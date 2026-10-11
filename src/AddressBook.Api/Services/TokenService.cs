using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AddressBook.Api.Models;
using AddressBook.Api.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AddressBook.Api.Services;

/// <summary>
/// Issues HS256 JWTs. Claims use short names (sub/name/role) so they survive without the
/// default inbound-claim mapping that would otherwise rewrite them to long URNs.
/// </summary>
public sealed class TokenService(IOptions<JwtOptions> jwtOptions) : ITokenService
{
    public (string Token, DateTimeOffset ExpiresAt) CreateToken(User user)
    {
        var options = jwtOptions.Value;
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(options.ExpiryMinutes);

        Claim[] claims =
        [
            new Claim("sub", user.Id.ToString()),
            new Claim("name", user.Username),
            new Claim("role", user.Role.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        ];

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Key));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: options.Issuer,
            audience: options.Audience,
            claims: claims,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        var handler = new JwtSecurityTokenHandler();
        return (handler.WriteToken(token), expiresAt);
    }
}
