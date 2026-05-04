namespace LootNet_API.DTO.Admin;

using LootNet_API.Enums;

public class GetRunsQueryDTO
{
    public Guid? UserId { get; set; }
    public RunStatus? Status { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
