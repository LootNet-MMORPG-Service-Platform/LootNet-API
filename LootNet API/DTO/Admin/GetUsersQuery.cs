using LootNet_API.Enums;

namespace LootNet_API.DTO.Admin;

public class GetUsersQueryDTO
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    public string? Search { get; set; }
    public UserRole? Role { get; set; }
    public bool? IsBlocked { get; set; }

    public string SortBy { get; set; } = "Username";
    public string SortDir { get; set; } = "asc";
}
