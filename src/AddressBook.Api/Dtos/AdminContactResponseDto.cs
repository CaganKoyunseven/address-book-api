namespace AddressBook.Api.Dtos;

/// <summary>Response shape for GET /admin/contacts: a ContactResponseDto plus owner and soft-delete info.</summary>
public sealed class AdminContactResponseDto
{
    public Guid Id { get; init; }

    public Guid OwnerId { get; init; }

    public string OwnerUsername { get; init; } = string.Empty;

    public string FirstName { get; init; } = string.Empty;

    public string LastName { get; init; } = string.Empty;

    public string? Email { get; init; }

    public string? Phone { get; init; }

    public AddressDto? Address { get; init; }

    public IReadOnlyList<string> Tags { get; init; } = [];

    public bool IsFavorite { get; init; }

    public bool IsDeleted { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset UpdatedAt { get; init; }
}
