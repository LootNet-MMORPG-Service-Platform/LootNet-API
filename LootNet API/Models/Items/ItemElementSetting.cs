using LootNet_API.Enums;

namespace LootNet_API.Models.Items;

public class ItemElementSetting
{
    public Guid Id { get; set; }
    public Guid RuleId { get; set; }
    public ItemElementType ElementType { get; set; }
    public List<DistributionSegment> Segments { get; set; } = new();
}
