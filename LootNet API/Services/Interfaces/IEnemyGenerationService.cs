using LootNet_API.Models.GameRun;

namespace LootNet_API.Services.Interfaces;

public interface IEnemyGenerationService
{
    Task<List<BattleEnemy>> GenerateEnemiesAsync(int stageIndex);
}
