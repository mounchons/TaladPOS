using Microsoft.EntityFrameworkCore;
using TaladPOS.Application.Auth;
using TaladPOS.Domain.Staff;
using TaladPOS.Infrastructure.Persistence;

namespace TaladPOS.Infrastructure.Repositories;

public class StaffRepository : IStaffRepository
{
    private readonly TaladPOSDbContext _dbContext;

    public StaffRepository(TaladPOSDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Staff?> GetByUsernameAsync(string username, CancellationToken ct = default) =>
        _dbContext.Staff.SingleOrDefaultAsync(s => s.Username == username, ct);

    public Task<bool> UsernameExistsAsync(string username, CancellationToken ct = default) =>
        _dbContext.Staff.AnyAsync(s => s.Username == username, ct);

    public async Task AddAsync(Staff staff, CancellationToken ct = default)
    {
        await _dbContext.Staff.AddAsync(staff, ct);
        await _dbContext.SaveChangesAsync(ct);
    }
}
