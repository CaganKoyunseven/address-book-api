using System.ComponentModel.DataAnnotations;

namespace AddressBook.Api.Dtos;

/// <summary>Query parameters for GET /contacts. Invalid values (bad enum text, out-of-range paging) are rejected with 400, never silently coerced.</summary>
public class ContactQuery
{
    [StringLength(100)]
    public string? Search { get; init; }

    [StringLength(30)]
    public string? Tag { get; init; }

    public bool? Favorite { get; init; }

    public ContactSortBy SortBy { get; init; } = ContactSortBy.Name;

    public SortDirection SortDir { get; init; } = SortDirection.Asc;

    public bool FavoritesFirst { get; init; }

    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 10;
}
