namespace AddressBook.Api.Options;

/// <summary>Bound from the "Jwt" section. Jwt:Key must be at least 32 chars (env var Jwt__Key in production).</summary>
public sealed class JwtOptions
{
    public string Key { get; set; } = string.Empty;

    public string Issuer { get; set; } = "AddressBook.Api";

    public string Audience { get; set; } = "AddressBook.Clients";

    public int ExpiryMinutes { get; set; } = 60;
}
