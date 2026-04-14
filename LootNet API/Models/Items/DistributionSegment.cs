namespace LootNet_API.Models.Items;

public class DistributionSegment
{
    public Guid Id { get; set; }
    public Guid? ItemElementSettingId { get; set; }
    public Guid? ItemParameterSettingId { get; set; }
    public double Min { get; set; }
    public double Max { get; set; }
    public double Weight { get; set; }
}
