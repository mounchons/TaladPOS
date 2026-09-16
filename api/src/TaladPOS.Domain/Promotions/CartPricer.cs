namespace TaladPOS.Domain.Promotions;

/// <summary>
/// Prices a whole cart: conditional promotions first, then the existing
/// per-item and whole-bill percentage promotions on whatever is left
/// (003/FR-010 - FR-021, algorithm in research.md #10).
///
/// Deliberately a pure function - no repository, no clock, no I/O. That is
/// what lets checkout and the preview endpoint share it, which is the only
/// way the total the cashier sees before paying can be guaranteed to equal
/// the total that gets charged (003/FR-012, 003/SC-006).
/// </summary>
public static class CartPricer
{
    public static PricedCart Price(
        IReadOnlyList<CartLine> cartLines,
        IReadOnlyDictionary<Guid, PricedProduct> products,
        IReadOnlyList<Promotion> percentagePromotions,
        IReadOnlyList<ConditionalPromotion> conditionalPromotions,
        bool hasMember,
        DateOnly date)
    {
        // Step 1: fold duplicate rows together. The checkout request does not
        // dedupe, and pricing "A x2" twice instead of "A x4" once would hand
        // out a second set of gifts that were never bought.
        var quantities = new SortedDictionary<Guid, int>();
        foreach (var line in cartLines)
        {
            quantities.TryGetValue(line.ProductId, out var existing);
            quantities[line.ProductId] = existing + line.Quantity;
        }

        var prices = products.ToDictionary(pair => pair.Key, pair => pair.Value.Price);
        var names = products.ToDictionary(pair => pair.Key, pair => pair.Value.Name);

        // Step 2: only promotions that are in date, pass the member gate, and
        // whose products all still exist. A promotion pointing at a deleted
        // product must not even produce an "unclaimed gift" hint - the cashier
        // could not act on it (003/FR-023, research.md #7).
        var eligible = conditionalPromotions
            .Where(promotion =>
                promotion.IsActive(date)
                && (!promotion.AppliesToMembersOnly || hasMember)
                && promotion.ReferencedProductIds.All(products.ContainsKey))
            // Step 3: a total order, so the same cart always allocates the same
            // way. Id is the third key because value-per-set and start date can
            // both tie (003/FR-019, research.md #9).
            .OrderByDescending(promotion => promotion.Reward.ValuePerSet(prices, promotion.ConditionLines))
            .ThenBy(promotion => promotion.StartDate)
            .ThenBy(promotion => promotion.Id)
            .ToList();

        var remaining = new Dictionary<Guid, int>(quantities);
        var giftUnits = new Dictionary<Guid, int>();
        var bundleDiscount = new Dictionary<Guid, decimal>();
        var applied = new List<AppliedPromotionResult>();
        var unclaimed = new List<UnclaimedGift>();

        // Step 4: allocate units to promotions, in order.
        foreach (var promotion in eligible)
        {
            var reward = promotion.Reward;
            var giftIsConditionProduct = reward.Kind == RewardKind.Gift
                && promotion.ConditionLines.Any(line => line.ProductId == reward.GiftProductId);

            // 003/FR-011: when the gift is one of the condition products, a set
            // eats the minimum *plus* the gift - the free units come out of what
            // was scanned, not out of thin air.
            int ConsumptionPerSet(ConditionLine line) =>
                line.MinimumQuantity
                + (giftIsConditionProduct && line.ProductId == reward.GiftProductId
                    ? reward.GiftQuantity!.Value
                    : 0);

            var sets = int.MaxValue;
            foreach (var line in promotion.ConditionLines)
            {
                remaining.TryGetValue(line.ProductId, out var available);
                sets = Math.Min(sets, available / ConsumptionPerSet(line));
            }

            if (sets <= 0)
            {
                continue;
            }

            // 003/FR-018: these units are spoken for and cannot be counted again,
            // by another conditional promotion or by a per-item percentage one.
            foreach (var line in promotion.ConditionLines)
            {
                remaining[line.ProductId] -= sets * ConsumptionPerSet(line);
            }

            decimal discount;
            if (reward.Kind == RewardKind.Gift)
            {
                var giftProductId = reward.GiftProductId!.Value;
                var wanted = sets * reward.GiftQuantity!.Value;

                int granted;
                if (giftIsConditionProduct)
                {
                    // Already deducted above as part of the set - do not deduct twice,
                    // and there is nothing to be short of.
                    granted = wanted;
                }
                else
                {
                    remaining.TryGetValue(giftProductId, out var availableGifts);
                    granted = Math.Min(wanted, availableGifts);
                    if (granted > 0)
                    {
                        remaining[giftProductId] = availableGifts - granted;
                    }

                    // 003/FR-015: the set count is never capped by whether the gift
                    // happens to be in the cart; the shortfall becomes a hint instead.
                    if (wanted > granted)
                    {
                        unclaimed.Add(new UnclaimedGift(
                            promotion.Id,
                            promotion.Describe(names),
                            giftProductId,
                            names[giftProductId],
                            wanted - granted));
                    }
                }

                if (granted > 0)
                {
                    giftUnits.TryGetValue(giftProductId, out var already);
                    giftUnits[giftProductId] = already + granted;
                }

                discount = granted * prices[giftProductId];
            }
            else
            {
                // 003/FR-020: the percentage applies to the condition products at the
                // quantity that completed the sets - never to leftovers, never to the bill.
                discount = 0m;
                foreach (var line in promotion.ConditionLines)
                {
                    var lineDiscount = Math.Round(
                        reward.DiscountPercentage!.Value / 100m
                            * line.MinimumQuantity * sets * prices[line.ProductId],
                        2);

                    bundleDiscount.TryGetValue(line.ProductId, out var already);
                    bundleDiscount[line.ProductId] = already + lineDiscount;
                    discount += lineDiscount;
                }
            }

            applied.Add(new AppliedPromotionResult(
                promotion.Id, promotion.Describe(names), sets, discount));
        }

        // Steps 5 and 6: leftovers fall through to the existing per-item rule,
        // then each product becomes a paid line and, if it earned any, a gift line.
        var paidLines = new List<PricedLine>();
        var giftLines = new List<PricedLine>();

        foreach (var (productId, quantity) in quantities)
        {
            var product = products[productId];
            giftUnits.TryGetValue(productId, out var gifted);
            var paidQuantity = quantity - gifted;

            if (paidQuantity > 0)
            {
                remaining.TryGetValue(productId, out var leftover);
                var itemDiscount = leftover > 0
                    ? DiscountResolver.ResolveItemDiscount(
                        percentagePromotions, productId, hasMember, product.Price * leftover, date)
                    : 0m;

                bundleDiscount.TryGetValue(productId, out var bundle);

                paidLines.Add(new PricedLine(
                    productId, product.Name, product.Price, paidQuantity, bundle + itemDiscount, false));
            }

            if (gifted > 0)
            {
                giftLines.Add(new PricedLine(
                    productId, product.Name, product.Price, gifted, product.Price * gifted, true));
            }
        }

        // Step 7: the whole-bill discount sees only what the customer is paying for.
        // A gift line is already net zero; giving it a share would push it negative
        // and would also inflate the base a customer never paid (003/FR-021).
        var billBase = paidLines.Sum(line => line.Subtotal);
        var billDiscount = DiscountResolver.ResolveBillDiscount(
            percentagePromotions, hasMember, billBase, date);

        var finalPaidLines = DistributeBillDiscount(paidLines, billDiscount);

        var lines = new List<PricedLine>(finalPaidLines.Count + giftLines.Count);
        foreach (var (productId, _) in quantities)
        {
            lines.AddRange(finalPaidLines.Where(line => line.ProductId == productId));
            lines.AddRange(giftLines.Where(line => line.ProductId == productId));
        }

        return new PricedCart(lines, applied, unclaimed);
    }

    /// <summary>
    /// Spread the bill-level discount over the paid lines by each line's share
    /// of the paid subtotal, with the rounding remainder folded into the last
    /// line so the totals still add up exactly - same approach the checkout use
    /// case used before this feature existed.
    /// </summary>
    private static List<PricedLine> DistributeBillDiscount(
        IReadOnlyList<PricedLine> paidLines, decimal billDiscount)
    {
        if (billDiscount <= 0m || paidLines.Count == 0)
        {
            return paidLines.ToList();
        }

        var billBase = paidLines.Sum(line => line.Subtotal);
        var result = new List<PricedLine>(paidLines.Count);
        var distributed = 0m;

        for (var i = 0; i < paidLines.Count; i++)
        {
            var line = paidLines[i];
            var share = i == paidLines.Count - 1
                ? billDiscount - distributed
                : Math.Round(billDiscount * (line.Subtotal / billBase), 2);
            distributed += share;

            result.Add(line with { DiscountAmount = line.DiscountAmount + share });
        }

        return result;
    }
}
