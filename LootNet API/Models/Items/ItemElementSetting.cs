using LootNet_API.Enums;

namespace LootNet_API.Models.Items;

public class ItemElementSetting
{
    public Guid Id { get; set; }
    public Guid RuleId { get; set; }
    public ItemElementType ElementType { get; set; }
    public List<DistributionSegment> Segments { get; set; } = new();

    public double Sample(Random rand)
    {
        double totalWeight = Segments.Sum(s => s.Weight);
        double roll = rand.NextDouble() * totalWeight;
        double cumulative = 0;

        foreach (var seg in Segments)
        {
            cumulative += seg.Weight;
            if (roll <= cumulative)
                return rand.NextDouble() * (seg.Max - seg.Min) + seg.Min;
        }

        var last = Segments.Last();
        return rand.NextDouble() * (last.Max - last.Min) + last.Min;
    }
}
