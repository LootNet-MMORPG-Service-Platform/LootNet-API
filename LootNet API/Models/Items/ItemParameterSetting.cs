namespace LootNet_API.Models.Items;
using Enums;

public class ItemParameterSetting
{
    public Guid Id { get; set; }
    public Guid RuleId { get; set; }
    public ItemParameter Parameter { get; set; }
    public List<DistributionSegment> Segments { get; set; } = new();
}
