using Microsoft.EntityFrameworkCore;
using TaladPOS.Application.Members;
using TaladPOS.Domain.Members;
using TaladPOS.Infrastructure.Persistence;

namespace TaladPOS.Infrastructure.Repositories;

public class MemberRepository : IMemberRepository
{
    private readonly TaladPOSDbContext _dbContext;

    public MemberRepository(TaladPOSDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Member?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _dbContext.Members.FirstOrDefaultAsync(m => m.Id == id, ct);

    public async Task<IReadOnlyList<Member>> SearchAsync(string? search, CancellationToken ct = default)
    {
        var query = _dbContext.Members.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(m => EF.Functions.ILike(m.Name, $"%{search}%") || m.PhoneNumber.Contains(search));
        }

        return await query.ToListAsync(ct);
    }

    public Task<bool> PhoneNumberExistsAsync(string phoneNumber, CancellationToken ct = default) =>
        _dbContext.Members.AnyAsync(m => m.PhoneNumber == phoneNumber, ct);

    public async Task AddAsync(Member member, CancellationToken ct = default)
    {
        await _dbContext.Members.AddAsync(member, ct);
        await _dbContext.SaveChangesAsync(ct);
    }

    public Task IncreaseAccumulatedPurchaseTotalAsync(Guid memberId, decimal amount, CancellationToken ct = default) =>
        _dbContext.Members
            .Where(m => m.Id == memberId)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    m => m.AccumulatedPurchaseTotal, m => m.AccumulatedPurchaseTotal + amount),
                ct);
}
