namespace LootNet_API.Services.Interfaces;

public interface IRealtimeNotifier
{
    Task AppChangedAsync(string domain, string action, Guid? userId = null, object? data = null);
}
