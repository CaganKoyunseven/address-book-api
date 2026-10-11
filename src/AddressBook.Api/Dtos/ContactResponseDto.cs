namespace AddressBook.Api.Dtos;

/// <summary>Response body shape for a contact. Never exposes OwnerId to a non-admin caller.</summary>
public sealed class ContactResponseDto
{
    public Guid Id { get; init; }

    public string FirstName { get; init; } = string.Empty;

    public string LastName { get; init; } = string.Empty;

    public string? Email { get; init; }

    public string? Phone { get; init; }

    public AddressDto? Address { get; init; }

    public IReadOnlyList<string> Tags { get; init; } = [];

    public bool IsFavorite { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset UpdatedAt { get; init; }
}
