using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaladPOS.Application.Auth;

namespace TaladPOS.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly StaffAuthenticator _authenticator;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthController(StaffAuthenticator authenticator, IJwtTokenService jwtTokenService)
    {
        _authenticator = authenticator;
        _jwtTokenService = jwtTokenService;
    }

    public record LoginRequest(string Username, string Password);

    public record StaffSummaryDto(Guid Id, string Name, string Role);

    public record LoginResponse(string Token, DateTime ExpiresAt, StaffSummaryDto Staff);

    /// <summary>contracts/auth.md - POST /api/auth/login (FR-007)</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken ct)
    {
        var staff = await _authenticator.AuthenticateAsync(request.Username, request.Password, ct);
        if (staff is null)
        {
            return Unauthorized(new { error = "invalid_credentials" });
        }

        var token = _jwtTokenService.GenerateToken(staff);
        return Ok(new LoginResponse(
            token.Value,
            token.ExpiresAtUtc,
            new StaffSummaryDto(staff.Id, staff.Name, staff.Role.ToString())));
    }
}
