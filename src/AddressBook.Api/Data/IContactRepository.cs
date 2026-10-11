using AddressBook.Api.Models;

namespace AddressBook.Api.Data;

/// <summary>
/// The only code allowed to touch the in-memory contact store. Returns full snapshots;
/// ownership checks, filtering, search, sort and paging are all done by the service layer.
/// </summary>
public interface IContactRepository
{
    Task<Contact?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Contact>> GetAllAsync(CancellationToken cancellationToken);

    Task<Contact> AddAsync(Contact contact, CancellationToken cancellationToken);

    /// <summary>
    /// Atomically replaces the contact identified by <paramref name="id"/> with the result of
    /// <paramref name="updater"/> applied to its current value (compare-and-swap, retried on
    /// conflict). Returns the new value, or null if no contact with that id exists.
    /// </summary>
    Task<Contact?> UpdateAsync(Guid id, Func<Contact, Contact> updater, CancellationToken cancellationToken);
}
