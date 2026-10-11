namespace AddressBook.Api.Models;

/// <summary>A contact's postal address. All parts are optional.</summary>
public sealed record Address(string? Street, string? City, string? Country);
