using TaladPOS.Domain.Staff;

namespace TaladPOS.Application.Auth;

public interface IStaffRepository
{
    Task<Staff?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<Staff?> GetByUsernameAsync(string username, CancellationToken ct = default);

    Task<bool> UsernameExistsAsync(string username, CancellationToken ct = default);

    Task AddAsync(Staff staff, CancellationToken ct = default);

    /// <summary>Single-store scale (a handful of staff accounts) - used to resolve names for the sales-by-staff report.</summary>
    Task<IReadOnlyList<Staff>> GetAllAsync(CancellationToken ct = default);
}
