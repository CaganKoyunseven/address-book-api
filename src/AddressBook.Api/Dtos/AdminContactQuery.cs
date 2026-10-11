namespace AddressBook.Api.Dtos;

/// <summary>Query parameters for GET /admin/contacts: same as ContactQuery, plus owner and soft-delete filters.</summary>
public sealed class AdminContactQuery : ContactQuery
{
    public Guid? OwnerId { get; init; }

    public bool IncludeDeleted { get; init; }
}
