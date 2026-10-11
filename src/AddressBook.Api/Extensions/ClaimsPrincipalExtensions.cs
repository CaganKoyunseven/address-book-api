using System.Security.Claims;

namespace AddressBook.Api.Extensions;

/// <summary>Reads identity info out of the JWT claims set by <see cref="Services.TokenService"/>.</summary>
public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue("sub")
            ?? throw new InvalidOperationException("The current principal has no 'sub' claim.");
        return Guid.Parse(value);
    }
}
