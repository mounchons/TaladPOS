using TaladPOS.Domain.Staff;

namespace TaladPOS.Application.Auth;

public record JwtToken(string Value, DateTime ExpiresAtUtc);

/// <summary>
/// Issues JWTs for authenticated staff (contracts/auth.md). Implemented in
/// the Infrastructure layer since it depends on a signing-library package
/// (research.md #1) - kept as an interface here so Application logic never
/// depends on that package directly.
/// </summary>
public interface IJwtTokenService
{
    JwtToken GenerateToken(Staff staff);
}
