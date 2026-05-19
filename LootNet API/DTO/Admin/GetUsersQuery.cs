using System.ComponentModel.DataAnnotations;
using LootNet_API.DTO.Validation;
using LootNet_API.Enums;

namespace LootNet_API.DTO.Admin;

public class GetUsersQueryDTO
{
    [Range(1, 10_000)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;

    [StringLength(80)]
    public string? Search { get; set; }

    public UserRole? Role { get; set; }
    public bool? IsBlocked { get; set; }

    [Required]
    [AllowedStringValues("Username", "Currency", "Role", "Blocked")]
    public string SortBy { get; set; } = "Username";

    [Required]
    [AllowedStringValues("asc", "desc")]
    public string SortDir { get; set; } = "asc";
}

