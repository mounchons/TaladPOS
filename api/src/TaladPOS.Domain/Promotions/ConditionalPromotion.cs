using System.Globalization;
using System.Text;

namespace TaladPOS.Domain.Promotions;

/// <summary>
/// A "buy this, get that" promotion (003/FR-001). Deliberately a separate
/// aggregate from <see cref="Promotion"/>: 003/FR-026 requires the existing
/// percentage promotions to keep behaving exactly as before, and keeping the
/// two apart makes that checkable by reading the diff rather than by trusting
/// a test run (research.md #1).
/// </summary>
public class ConditionalPromotion
{
    public const int MaxNameLength = 100;

    private readonly List<ConditionLine> _conditionLines = new();

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public IReadOnlyList<ConditionLine> ConditionLines =>
        _conditionLines.OrderBy(line => line.SortOrder).ToList();

    public Reward Reward { get; private set; } = null!;

    public bool AppliesToMembersOnly { get; private set; }

    public DateOnly StartDate { get; private set; }

    public DateOnly EndDate { get; private set; }

    // EF Core materialization constructor.
    private ConditionalPromotion()
    {
    }

    public ConditionalPromotion(
        string name,
        IEnumerable<ConditionLine> conditionLines,
        Reward reward,
        bool appliesToMembersOnly,
        DateOnly startDate,
        DateOnly endDate)
    {
        Id = Guid.NewGuid();
        Apply(name, conditionLines, reward, appliesToMembersOnly, startDate, endDate);
    }

    public void Update(
        string name,
        IEnumerable<ConditionLine> conditionLines,
        Reward reward,
        bool appliesToMembersOnly,
        DateOnly startDate,
        DateOnly endDate)
    {
        Apply(name, conditionLines, reward, appliesToMembersOnly, startDate, endDate);
    }

    /// <summary>003/FR-006: active exactly when today falls within [StartDate, EndDate], inclusive.</summary>
    public bool IsActive(DateOnly date) => date >= StartDate && date <= EndDate;

    /// <summary>
    /// Every product this promotion points at - condition products plus the
    /// gift product. The caller checks these still exist before the promotion
    /// is allowed to price a cart (003/FR-023, research.md #7).
    /// </summary>
    public IReadOnlyList<Guid> ReferencedProductIds
    {
        get
        {
            var ids = _conditionLines.Select(line => line.ProductId).ToList();
            if (Reward.Kind == RewardKind.Gift)
            {
                ids.Add(Reward.GiftProductId!.Value);
            }

            return ids.Distinct().ToList();
        }
    }

    /// <summary>
    /// The one-line human description shown on the promotions screen, the
    /// register and the receipt (003/FR-008). Product names are passed in
    /// rather than looked up: the domain layer has no business reaching for a
    /// product repository (research.md #11).
    /// </summary>
    public string Describe(IReadOnlyDictionary<Guid, string> productNames)
    {
        string NameOf(Guid id) => productNames.TryGetValue(id, out var name) ? name : "สินค้าที่ถูกลบแล้ว";

        // Reads through the ordered property, not the backing field, so the text
        // is identical every time the same promotion is described.
        var lines = ConditionLines;
        var text = new StringBuilder("ซื้อ ");
        for (var i = 0; i < lines.Count; i++)
        {
            if (i > 0)
            {
                text.Append(" + ");
            }

            text.Append(NameOf(lines[i].ProductId))
                .Append(' ')
                .Append(lines[i].MinimumQuantity.ToString(CultureInfo.InvariantCulture));
        }

        if (Reward.Kind == RewardKind.Gift)
        {
            text.Append(" แถม ")
                .Append(NameOf(Reward.GiftProductId!.Value))
                .Append(' ')
                .Append(Reward.GiftQuantity!.Value.ToString(CultureInfo.InvariantCulture));
        }
        else
        {
            text.Append(" ลด ")
                .Append(Reward.DiscountPercentage!.Value.ToString("0.##", CultureInfo.InvariantCulture))
                .Append('%');
        }

        return text.ToString();
    }

    private void Apply(
        string name,
        IEnumerable<ConditionLine> conditionLines,
        Reward reward,
        bool appliesToMembersOnly,
        DateOnly startDate,
        DateOnly endDate)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name is required.", nameof(name));
        }

        if (name.Length > MaxNameLength)
        {
            throw new ArgumentException(
                $"Name must be at most {MaxNameLength} characters.", nameof(name));
        }

        var lines = conditionLines?.ToList() ?? new List<ConditionLine>();
        if (lines.Count == 0)
        {
            throw new ArgumentException("At least one condition line is required.", nameof(conditionLines));
        }

        if (lines.Select(line => line.ProductId).Distinct().Count() != lines.Count)
        {
            throw new ArgumentException(
                "A product may appear at most once in the buy condition.", nameof(conditionLines));
        }

        if (reward is null)
        {
            throw new ArgumentNullException(nameof(reward));
        }

        if (endDate < startDate)
        {
            throw new ArgumentException("EndDate must be greater than or equal to StartDate.", nameof(endDate));
        }

        Name = name.Trim();
        _conditionLines.Clear();
        for (var i = 0; i < lines.Count; i++)
        {
            lines[i].PlaceAt(i);
            _conditionLines.Add(lines[i]);
        }

        Reward = reward;
        AppliesToMembersOnly = appliesToMembersOnly;
        StartDate = startDate;
        EndDate = endDate;
    }
}
