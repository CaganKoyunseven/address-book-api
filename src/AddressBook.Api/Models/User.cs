namespace AddressBook.Api.Models;

/// <summary>An account that can log in and owns its own address book.</summary>
public sealed record User(Guid Id, string Username, string PasswordHash, Role Role);
