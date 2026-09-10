namespace TaladPOS.Domain.Members;

/// <summary>Store member (FR-010-FR-014). No lifecycle/tiers - registers once, stays forever.</summary>
public class Member
{
    public Guid Id { get; private set; }

    public string Name { get; private set; }

    public string PhoneNumber { get; private set; }

    public decimal AccumulatedPurchaseTotal { get; private set; }

    // EF Core materialization constructor.
    private Member()
    {
        Name = string.Empty;
        PhoneNumber = string.Empty;
    }

    public Member(string name, string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name is required.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            throw new ArgumentException("PhoneNumber is required.", nameof(phoneNumber));
        }

        Id = Guid.NewGuid();
        Name = name;
        PhoneNumber = phoneNumber;
        AccumulatedPurchaseTotal = 0m;
    }

    /// <summary>
    /// In-memory guard used by domain unit tests. The authoritative,
    /// concurrency-safe update for multi-register sales happens as an
    /// atomic conditional SQL UPDATE in the infrastructure layer (same
    /// pattern as Product.DecreaseStock, research.md #2) - this method is
    /// not what protects against races between two registers ringing up
    /// the same member at once.
    /// </summary>
    public void IncreaseAccumulatedPurchaseTotal(decimal amount)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "Amount cannot be negative.");
        }

        AccumulatedPurchaseTotal += amount;
    }
}
