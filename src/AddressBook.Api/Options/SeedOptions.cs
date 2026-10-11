namespace AddressBook.Api.Options;

/// <summary>Bound from the "Seed" configuration section (env vars: Seed__AdminPassword, Seed__DemoPassword).</summary>
public sealed class SeedOptions
{
    public bool Enabled { get; set; } = true;

    public string AdminPassword { get; set; } = string.Empty;

    public string DemoPassword { get; set; } = string.Empty;
}
