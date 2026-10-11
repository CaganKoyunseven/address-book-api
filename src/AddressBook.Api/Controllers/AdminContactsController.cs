using AddressBook.Api.Dtos;
using AddressBook.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AddressBook.Api.Controllers;

/// <summary>Admin-only view across every user's address book, and the only place that can restore a soft-deleted contact.</summary>
[ApiController]
[Authorize(Roles = "Admin")]
[Route(ApiRoutes.V1 + "/admin/contacts")]
public sealed class AdminContactsController(IContactService contactService) : ControllerBase
{
    /// <summary>Lists contacts across all owners. Add ownerId to scope to one user, includeDeleted=true to see soft-deleted ones too.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AdminContactResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> List([FromQuery] AdminContactQuery query, CancellationToken cancellationToken)
    {
        var result = await contactService.AdminListAsync(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>Un-deletes a contact regardless of its owner. Idempotent: restoring an already-active contact also returns 204.</summary>
    [HttpPost("{id:guid}/restore")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Restore(Guid id, CancellationToken cancellationToken)
    {
        var restored = await contactService.RestoreAsync(id, cancellationToken);
        return restored ? NoContent() : NotFound();
    }
}
