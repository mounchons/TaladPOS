using Microsoft.AspNetCore.Identity;
using TaladPOS.Domain.Staff;

namespace TaladPOS.Application.Auth;

/// <summary>
/// Marker type for <see cref="PasswordHasher{TUser}"/>. The default hasher
/// implementation never reads the user's members, so a marker avoids forcing
/// a real <see cref="Staff"/> instance to exist just to hash a password
/// (e.g. when seeding a brand-new account).
/// </summary>
internal sealed class PasswordHashSubject
{
}

/// <summary>
/// Verifies and hashes staff credentials (FR-007). Uses the standalone
/// <see cref="PasswordHasher{TUser}"/> rather than full ASP.NET Core Identity
/// so <see cref="Staff"/> stays a plain domain entity (research.md #1).
/// </summary>
public class StaffAuthenticator
{
    private static readonly PasswordHashSubject Subject = new();

    private readonly IStaffRepository _staffRepository;
    private readonly PasswordHasher<PasswordHashSubject> _passwordHasher = new();

    public StaffAuthenticator(IStaffRepository staffRepository)
    {
        _staffRepository = staffRepository;
    }

    public async Task<Staff?> AuthenticateAsync(string username, string password, CancellationToken ct = default)
    {
        var staff = await _staffRepository.GetByUsernameAsync(username, ct);
        if (staff is null)
        {
            return null;
        }

        var result = _passwordHasher.VerifyHashedPassword(Subject, staff.PasswordHash, password);
        return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded
            ? staff
            : null;
    }

    public string HashPassword(string password) => _passwordHasher.HashPassword(Subject, password);
}
