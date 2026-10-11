using AddressBook.Api.Dtos;
using AddressBook.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace AddressBook.Api.Controllers;

/// <summary>Account registration and login. No [Authorize] here, so these two actions are reachable by anyone.</summary>
[ApiController]
[Route(ApiRoutes.V1 + "/auth")]
public sealed class AuthController(IAuthService authService, ITokenService tokenService) : ControllerBase
{
    /// <summary>Creates a new user account. The new account always gets the "User" role.</summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(RegisterDto dto, CancellationToken cancellationToken)
    {
        var user = await authService.RegisterAsync(dto.Username, dto.Password, cancellationToken);
        if (user is null)
        {
            return Problem(statusCode: StatusCodes.Status409Conflict, title: "Username already taken");
        }

        var response = new RegisterResponseDto { Id = user.Id, Username = user.Username, Role = user.Role.ToString() };
        return StatusCode(StatusCodes.Status201Created, response);
    }

    /// <summary>Exchanges valid credentials for a JWT. Unknown username and wrong password return the same error.</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LoginDto dto, CancellationToken cancellationToken)
    {
        var user = await authService.LoginAsync(dto.Username, dto.Password, cancellationToken);
        if (user is null)
        {
            return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Invalid username or password");
        }

        var (token, expiresAt) = tokenService.CreateToken(user);
        return Ok(new AuthResponseDto
        {
            Token = token,
            ExpiresAt = expiresAt,
            Username = user.Username,
            Role = user.Role.ToString(),
        });
    }
}
