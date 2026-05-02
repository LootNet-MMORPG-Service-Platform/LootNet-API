namespace LootNet_API.Tests.Helpers;

using LootNet_API.Data;
using Microsoft.EntityFrameworkCore;

public class SingleContextFactory : IDbContextFactory<AppDbContext>
{
    private readonly string _dbName;

    public SingleContextFactory(string dbName)
    {
        _dbName = dbName;
    }

    public AppDbContext CreateDbContext()
        => new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(_dbName)
                .Options);
}

public static class DbHelper
{
    public static (AppDbContext db, SingleContextFactory factory) Create()
    {
        var dbName = Guid.NewGuid().ToString();
        var factory = new SingleContextFactory(dbName);
        var db = factory.CreateDbContext();
        return (db, factory);
    }
}