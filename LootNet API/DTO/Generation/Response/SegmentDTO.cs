namespace LootNet_API.DTO.Generation.Response;

public class SegmentDTO
{
    public Guid Id { get; set; }
    public double Min { get; set; }
    public double Max { get; set; }
    public double Weight { get; set; }
}