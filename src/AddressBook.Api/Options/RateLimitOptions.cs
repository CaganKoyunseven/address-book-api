namespace AddressBook.Api.Options;

/// <summary>Bound from the "RateLimiting" section. Kept configurable so tests can relax the limits.</summary>
public sealed class RateLimitOptions
{
    public RateLimitPolicyOptions Global { get; set; } = new() { PermitLimit = 100, WindowSeconds = 60 };

    public RateLimitPolicyOptions Auth { get; set; } = new() { PermitLimit = 10, WindowSeconds = 60 };
}

public sealed class RateLimitPolicyOptions
{
    public int PermitLimit { get; set; }

    public int WindowSeconds { get; set; }
}
