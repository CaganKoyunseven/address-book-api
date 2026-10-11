namespace AddressBook.Api.Models;

/// <summary>
/// An address book entry owned by a <see cref="User"/>. Immutable: changes are made by
/// creating a new instance with `with { ... }` (see <see cref="Data.IContactRepository"/>).
/// </summary>
public sealed record Contact(
    Guid Id,
    Guid OwnerId,
    string FirstName,
    string LastName,
    string? Email,
    string? Phone,
    Address? Address,
    IReadOnlyList<string> Tags,
    bool IsFavorite,
    bool IsDeleted,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
