using TaladPOS.Domain.Staff;

namespace TaladPOS.Application.Auth;

public interface IStaffRepository
{
    Task<Staff?> GetByUsernameAsync(string username, CancellationToken ct = default);

    Task<bool> UsernameExistsAsync(string username, CancellationToken ct = default);

    Task AddAsync(Staff staff, CancellationToken ct = default);
}
