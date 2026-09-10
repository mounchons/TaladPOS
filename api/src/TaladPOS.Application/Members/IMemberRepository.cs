using TaladPOS.Domain.Members;

namespace TaladPOS.Application.Members;

public interface IMemberRepository
{
    Task<Member?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<Member>> SearchAsync(string? search, CancellationToken ct = default);

    Task<bool> PhoneNumberExistsAsync(string phoneNumber, CancellationToken ct = default);

    Task AddAsync(Member member, CancellationToken ct = default);

    /// <summary>
    /// Atomic increment (research.md #2 pattern, applied to member
    /// accumulation): issues <c>UPDATE members SET accumulated_purchase_total
    /// = accumulated_purchase_total + @amount WHERE id = @id</c> directly
    /// against the database, so two registers ringing up the same member at
    /// once can't lose an increment the way a read-modify-write would.
    /// </summary>
    Task IncreaseAccumulatedPurchaseTotalAsync(Guid memberId, decimal amount, CancellationToken ct = default);
}
