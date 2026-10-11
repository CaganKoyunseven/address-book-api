using AddressBook.Api.Extensions;
using AddressBook.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AddressBook.Api.Controllers;

/// <summary>Feeds an "All Tags" style dropdown with the distinct tags on the caller's own contacts.</summary>
[ApiController]
[Authorize]
[Route(ApiRoutes.V1 + "/tags")]
public sealed class TagsController(IContactService contactService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(string[]), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetTags(CancellationToken cancellationToken)
    {
        var tags = await contactService.GetTagsAsync(User.GetUserId(), cancellationToken);
        return Ok(tags);
    }
}
