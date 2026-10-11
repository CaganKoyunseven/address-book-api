using AddressBook.Api.Dtos;
using AddressBook.Api.Extensions;
using AddressBook.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AddressBook.Api.Controllers;

/// <summary>
/// A caller's own address book. Every action requires a valid JWT ([Authorize] at class
/// level). A contact that doesn't exist, belongs to someone else, or is soft-deleted all
/// look identical from here: 404, never 403, so existence is never leaked.
/// </summary>
[ApiController]
[Authorize]
[Route(ApiRoutes.V1 + "/contacts")]
public sealed class ContactsController(IContactService contactService) : ControllerBase
{
    /// <summary>Lists the caller's own contacts, with search, filter, sort and paging.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ContactResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> List([FromQuery] ContactQuery query, CancellationToken cancellationToken)
    {
        var result = await contactService.ListAsync(User.GetUserId(), query, cancellationToken);
        return Ok(result);
    }

    /// <summary>Gets one of the caller's own contacts.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ContactResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var contact = await contactService.GetByIdAsync(User.GetUserId(), id, cancellationToken);
        return contact is null ? NotFound() : Ok(contact);
    }

    /// <summary>Creates a new contact owned by the caller. The server assigns the Id.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ContactResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create(ContactCreateDto dto, CancellationToken cancellationToken)
    {
        var created = await contactService.CreateAsync(User.GetUserId(), dto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Full replacement of the editable fields of one of the caller's own contacts.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ContactResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, ContactUpdateDto dto, CancellationToken cancellationToken)
    {
        var updated = await contactService.UpdateAsync(User.GetUserId(), id, dto, cancellationToken);
        return updated is null ? NotFound() : Ok(updated);
    }

    /// <summary>Soft-deletes one of the caller's own contacts.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await contactService.DeleteAsync(User.GetUserId(), id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    /// <summary>Marks the contact as a favorite. Idempotent: calling it twice still returns 204.</summary>
    [HttpPut("{id:guid}/favorite")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkFavorite(Guid id, CancellationToken cancellationToken)
    {
        var ok = await contactService.SetFavoriteAsync(User.GetUserId(), id, favorite: true, cancellationToken);
        return ok ? NoContent() : NotFound();
    }

    /// <summary>Unmarks the contact as a favorite. Idempotent: calling it twice still returns 204.</summary>
    [HttpDelete("{id:guid}/favorite")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnmarkFavorite(Guid id, CancellationToken cancellationToken)
    {
        var ok = await contactService.SetFavoriteAsync(User.GetUserId(), id, favorite: false, cancellationToken);
        return ok ? NoContent() : NotFound();
    }
}
