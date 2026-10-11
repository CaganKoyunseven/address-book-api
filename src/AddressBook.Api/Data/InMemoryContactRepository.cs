using System.Collections.Concurrent;
using AddressBook.Api.Models;

namespace AddressBook.Api.Data;

/// <summary>
/// Thread-safe in-memory store. Contacts are immutable records, so updates never mutate a
/// stored instance: they read the current value, compute a new one, and swap it in with
/// <see cref="ConcurrentDictionary{TKey,TValue}.TryUpdate"/> (compare-and-swap). If another
/// thread wins the race, we retry with the fresh value. Registered as a singleton (this is
/// where the "database" actually lives).
/// </summary>
public sealed class InMemoryContactRepository : IContactRepository
{
    private const int MaxUpdateAttempts = 5;

    private readonly ConcurrentDictionary<Guid, Contact> _contacts = new();

    public Task<Contact?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => Task.FromResult(_contacts.TryGetValue(id, out var contact) ? contact : null);

    public Task<IReadOnlyList<Contact>> GetAllAsync(CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<Contact>>(_contacts.Values.ToList());

    public Task<Contact> AddAsync(Contact contact, CancellationToken cancellationToken)
    {
        if (!_contacts.TryAdd(contact.Id, contact))
        {
            throw new InvalidOperationException($"A contact with id '{contact.Id}' already exists.");
        }

        return Task.FromResult(contact);
    }

    public Task<Contact?> UpdateAsync(Guid id, Func<Contact, Contact> updater, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < MaxUpdateAttempts; attempt++)
        {
            if (!_contacts.TryGetValue(id, out var current))
            {
                return Task.FromResult<Contact?>(null);
            }

            var updated = updater(current);
            if (_contacts.TryUpdate(id, updated, current))
            {
                return Task.FromResult<Contact?>(updated);
            }
        }

        throw new InvalidOperationException(
            $"Could not update contact '{id}' after {MaxUpdateAttempts} attempts due to concurrent modifications.");
    }
}
