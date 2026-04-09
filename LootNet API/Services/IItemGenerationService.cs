using LootNet_API.Models.Items;

namespace LootNet_API.Services;

public interface IItemGenerationService
{
    Task<Item> GenerateItemAsync(Guid ownerId);
}
