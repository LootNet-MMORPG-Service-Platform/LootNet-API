namespace Desktop.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using desktop_app.Enums;
using desktop_app.Models;
using desktop_app.Models.Economy;
using desktop_app.Models.EnemyGeneration;
using desktop_app.Services;
using desktop_app.Services.Economy;
using desktop_app.Services.Generation;
using desktop_app.ViewModels;
using desktop_app.ViewModels.Economy;
using desktop_app.ViewModels.Generation;
using desktop_app.ViewModels.Logs;
using desktop_app.ViewModels.Users;
using Xunit;

public class AdminPanelSectionsFlowTests
{
    [Fact]
    public async Task UsersSection_LoadsSelectsChangesRoleAndBlocksUser()
    {
        var api = new AdminPanelApiStub();
        using var client = new HttpClient(api) { BaseAddress = new Uri("https://api.test") };
        var service = new AdminService("desktop-token", client);
        var viewModel = new UsersViewModel(service, new AuthService(), _ => { }, () => Task.CompletedTask);
        viewModel.SetCurrentUserId(api.AdminId);

        await viewModel.LoadUsersAsync();

        Assert.Equal(2, viewModel.Users.Count);
        Assert.Equal("Page 1 of 1 loaded.", viewModel.StatusMessage);
        Assert.True(viewModel.Users.Single(x => x.Id == api.AdminId).IsSelf);

        var target = viewModel.Users.Single(x => x.Id == api.PlayerId);
        viewModel.SelectUserCommand.Execute(target);
        Assert.True(viewModel.HasSelectedUser);
        Assert.Equal("player", viewModel.SelectedUsername);

        await viewModel.ChangeRoleAsync(UserRole.Admin.ToString());
        await WaitForAsync(() => viewModel.SelectedUser?.Role == UserRole.Admin);
        Assert.Contains($"POST /api/admin/users/{api.PlayerId}/role", api.Calls);

        await viewModel.ToggleBlockStatusCommand.ExecuteAsync(viewModel.SelectedUser!);
        await WaitForAsync(() => viewModel.SelectedUser?.IsBlocked == true);
        Assert.Contains($"POST /api/admin/users/{api.PlayerId}/block", api.Calls);

        await viewModel.ToggleBlockStatusCommand.ExecuteAsync(viewModel.SelectedUser!);
        await WaitForAsync(() => viewModel.SelectedUser?.IsBlocked == false);
        Assert.Contains($"POST /api/admin/users/{api.PlayerId}/unblock", api.Calls);
    }

    [Fact]
    public async Task EconomySection_LoadsValidatesAndSavesSettings()
    {
        var api = new AdminPanelApiStub();
        using var client = new HttpClient(api) { BaseAddress = new Uri("https://api.test") };
        var viewModel = new EconomyViewModel(new EconomyAdminService("desktop-token", client));

        await viewModel.LoadAsync();

        Assert.True(viewModel.HasSettings);
        Assert.True(viewModel.HasStats);
        Assert.Equal("Economy data loaded.", viewModel.StatusMessage);
        Assert.Contains("GET /api/admin/market/economy", api.Calls);
        Assert.Contains("GET /api/admin/market/stats", api.Calls);

        viewModel.DailyCurrencyRewardValue = "-1";
        await viewModel.SaveAsync();

        Assert.True(viewModel.HasDailyCurrencyRewardError);
        Assert.DoesNotContain("PUT /api/admin/market/economy", api.Calls);

        viewModel.DailyCurrencyRewardValue = "125";
        viewModel.BotBasePriceValue = "20";
        viewModel.BotStatMultiplierValue = "2.5";
        viewModel.BotElementMultiplierValue = "1.5";
        viewModel.IsPlayerToPlayerTaxEnabled = false;

        await viewModel.SaveAsync();

        Assert.Contains("PUT /api/admin/market/economy", api.Calls);
        Assert.Equal(125, api.EconomySettings.DailyCurrencyReward);
        Assert.False(api.EconomySettings.IsPlayerToPlayerTaxEnabled);
    }

    [Fact]
    public async Task LogsSection_LoadsAdminOptionsAndLogs()
    {
        var api = new AdminPanelApiStub();
        using var client = new HttpClient(api) { BaseAddress = new Uri("https://api.test") };
        var viewModel = new LogsViewModel(new AdminService("desktop-token", client));

        await viewModel.LoadLogsAsync();

        Assert.Equal(2, viewModel.AdminOptions.Count);
        Assert.Equal("All admins", viewModel.AdminOptions[0].DisplayName);
        Assert.Single(viewModel.Logs);
        Assert.True(viewModel.HasLogs);
        Assert.Equal("UPDATE_PARAMETER", viewModel.Logs[0].Action);
        Assert.Contains("GET /api/admin/users/admins", api.Calls);
        Assert.Contains("GET /api/admin/logs", api.Calls);
    }

    [Fact]
    public async Task EnemyGenerationSection_ManagesProfilesScenariosSlotsAndClassProfiles()
    {
        var api = new AdminPanelApiStub();
        using var client = new HttpClient(api) { BaseAddress = new Uri("https://api.test") };
        var viewModel = new EnemyGenerationViewModel(new EnemyGenerationAdminService("desktop-token", client));

        await viewModel.LoadProfilesAsync();

        Assert.Empty(viewModel.Profiles);
        Assert.Empty(viewModel.ClassProfiles);
        Assert.Equal("Loaded 0 stage profiles and 0 class profiles.", viewModel.StatusMessage);

        await viewModel.CreateStageProfileAsync("Stage 1", 1, 10, 0.2, 3);
        await WaitForAsync(() => viewModel.Profiles.Count == 1);
        Assert.Contains("POST /api/admin/enemy-generation/profiles", api.Calls);

        await viewModel.CreateEnemyClassProfileAsync(
            "Tank basic",
            EnemyClass.Tank,
            new List<int> { 0, 1 },
            viewModel.Profiles[0].Id,
            5);
        await WaitForAsync(() => viewModel.ClassProfiles.Count == 1);
        Assert.Contains("POST /api/admin/enemy-generation/class-profiles", api.Calls);
        Assert.Equal("Stage 1", viewModel.ClassProfiles[0].GenerationProfileName);

        viewModel.SelectProfileCommand.Execute(viewModel.Profiles[0]);
        await WaitForAsync(() => viewModel.HasSelectedProfile);

        await viewModel.CreateStageScenarioAsync(2, 15);
        await WaitForAsync(() => viewModel.Scenarios.Count == 1);
        Assert.Contains($"POST /api/admin/enemy-generation/profiles/{api.LastStageProfileId}/scenarios", api.Calls);

        viewModel.SelectScenarioCommand.Execute(viewModel.Scenarios[0]);
        await WaitForAsync(() => viewModel.HasSelectedScenario);

        await viewModel.CreateScenarioSlotAsync(1, viewModel.ClassProfiles[0].Id, 20);
        await WaitForAsync(() => viewModel.Slots.Count == 1);
        Assert.Contains($"POST /api/admin/enemy-generation/scenarios/{api.LastScenarioId}/slots", api.Calls);
        Assert.Equal("Tank basic", viewModel.Slots[0].ClassProfileName);

        await viewModel.UpdateScenarioSlotAsync(viewModel.Slots[0], 2, viewModel.ClassProfiles[0].Id, 30);
        await WaitForAsync(() => viewModel.Slots[0].Position == 2);
        Assert.Contains("PUT /api/admin/enemy-generation/slots", api.Calls);

        await viewModel.DeleteScenarioSlotAsync(viewModel.Slots[0]);
        await WaitForAsync(() => viewModel.Slots.Count == 0);
        Assert.Contains($"DELETE /api/admin/enemy-generation/slots/{api.LastSlotId}", api.Calls);

        await viewModel.DeleteStageScenarioAsync(viewModel.Scenarios[0]);
        await WaitForAsync(() => viewModel.Scenarios.Count == 0);
        Assert.Contains($"DELETE /api/admin/enemy-generation/scenarios/{api.LastScenarioId}", api.Calls);

        await viewModel.DeleteEnemyClassProfileAsync(viewModel.ClassProfiles[0]);
        await WaitForAsync(() => viewModel.ClassProfiles.Count == 0);
        Assert.Contains($"DELETE /api/admin/enemy-generation/class-profiles/{api.LastClassProfileId}", api.Calls);

        await viewModel.DeleteStageProfileAsync(viewModel.Profiles[0]);
        await WaitForAsync(() => viewModel.Profiles.Count == 0);
        Assert.Contains($"DELETE /api/admin/enemy-generation/profiles/{api.LastStageProfileId}", api.Calls);
    }

    [Fact]
    public void HomeViewModel_ExposesAdminRoleAccessRules()
    {
        var home = new HomeViewModel(new MainWindowViewModel());

        home.SetRole(UserRole.GameModerator.ToString());
        Assert.False(home.CanAccessUsers);
        Assert.True(home.CanAccessItemGeneration);
        Assert.True(home.CanAccessEnemyGeneration);

        home.SetRole(UserRole.Admin.ToString());
        Assert.True(home.CanAccessUsers);
        Assert.False(home.CanAccessItemGeneration);
        Assert.False(home.CanAccessEnemyGeneration);

        home.SetRole(UserRole.SuperAdmin.ToString());
        Assert.True(home.CanAccessUsers);
        Assert.True(home.CanAccessItemGeneration);
        Assert.True(home.CanAccessEnemyGeneration);
    }

    private static async Task WaitForAsync(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(3);

        while (DateTime.UtcNow < deadline)
        {
            if (condition())
                return;

            await Task.Delay(25);
        }

        Assert.True(condition());
    }

    private sealed class AdminPanelApiStub : HttpMessageHandler
    {
        private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);
        private readonly List<AdminUser> _users;
        private readonly List<StageProfile> _stageProfiles = new();
        private readonly List<StageScenario> _scenarios = new();
        private readonly List<ScenarioSlot> _slots = new();
        private readonly List<EnemyClassProfile> _classProfiles = new();

        public string AdminId { get; } = Guid.NewGuid().ToString();
        public string PlayerId { get; } = Guid.NewGuid().ToString();
        public Guid LastStageProfileId { get; private set; }
        public Guid LastScenarioId { get; private set; }
        public Guid LastSlotId { get; private set; }
        public Guid LastClassProfileId { get; private set; }
        public EconomySettings EconomySettings { get; private set; } = new()
        {
            DailyCurrencyReward = 50,
            BotBasePrice = 10,
            BotStatMultiplier = 2,
            BotElementMultiplier = 1,
            IsPlayerToPlayerTaxEnabled = true,
            IsPlayerToBotTaxEnabled = true,
            BotSaleFormula = "test"
        };
        public List<string> Calls { get; } = new();

        public AdminPanelApiStub()
        {
            _users = new List<AdminUser>
            {
                new() { Id = AdminId, Username = "admin", Role = UserRole.SuperAdmin, Currency = 1000 },
                new() { Id = PlayerId, Username = "player", Role = UserRole.Player, Currency = 100 }
            };
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri!.AbsolutePath;
            Calls.Add($"{request.Method.Method} {path}");

            if (request.Headers.Authorization?.Scheme != "Bearer" ||
                request.Headers.Authorization.Parameter != "desktop-token")
            {
                return new HttpResponseMessage(HttpStatusCode.Unauthorized);
            }

            if (request.Method == HttpMethod.Get && path == "/api/admin/users")
                return Json(new PagedResult<AdminUser> { Items = _users.ToList(), TotalCount = _users.Count, Page = 1, PageSize = 10 });

            if (request.Method == HttpMethod.Post && TryMatchUserAction(path, "/api/admin/users/", "/role", out var userId))
            {
                var dto = await ReadAsync<RoleRequest>(request, cancellationToken);
                _users.Single(x => x.Id == userId).Role = (UserRole)dto.Role;
                return Json(new { ok = true });
            }

            if (request.Method == HttpMethod.Post && TryMatchUserAction(path, "/api/admin/users/", "/block", out userId))
            {
                _users.Single(x => x.Id == userId).IsBlocked = true;
                return Json(new { ok = true });
            }

            if (request.Method == HttpMethod.Post && TryMatchUserAction(path, "/api/admin/users/", "/unblock", out userId))
            {
                _users.Single(x => x.Id == userId).IsBlocked = false;
                return Json(new { ok = true });
            }

            if (request.Method == HttpMethod.Get && path == "/api/admin/users/admins")
            {
                return Json(_users
                    .Where(x => x.Role is UserRole.Admin or UserRole.SuperAdmin or UserRole.GameModerator)
                    .Select(x => new AdminUserList
                    {
                        Id = Guid.Parse(x.Id),
                        Username = x.Username,
                        Role = x.Role,
                        Currency = x.Currency,
                        IsBlocked = x.IsBlocked
                    })
                    .ToList());
            }

            if (request.Method == HttpMethod.Get && path == "/api/admin/logs")
                return Json(new PagedResult<AdminLog>
                {
                    Items = new List<AdminLog>
                    {
                        new()
                        {
                            Id = Guid.NewGuid(),
                            AdminId = Guid.Parse(AdminId),
                            Action = "UPDATE_PARAMETER",
                            TargetUserId = PlayerId,
                            CreatedAt = DateTime.UtcNow,
                            Data = "{\"ok\":true}"
                        }
                    },
                    TotalCount = 1,
                    Page = 1,
                    PageSize = 30
                });

            if (request.Method == HttpMethod.Get && path == "/api/admin/market/economy")
                return Json(EconomySettings);

            if (request.Method == HttpMethod.Get && path == "/api/admin/market/stats")
                return Json(new { totalListings = 3, totalSales = 2 });

            if (request.Method == HttpMethod.Put && path == "/api/admin/market/economy")
            {
                EconomySettings = await ReadAsync<EconomySettings>(request, cancellationToken);
                return Json(new { ok = true });
            }

            if (path.StartsWith("/api/admin/enemy-generation", StringComparison.Ordinal))
                return await HandleEnemyGenerationAsync(request, path, cancellationToken);

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }

        private async Task<HttpResponseMessage> HandleEnemyGenerationAsync(
            HttpRequestMessage request,
            string path,
            CancellationToken cancellationToken)
        {
            if (request.Method == HttpMethod.Get && path == "/api/admin/enemy-generation/profiles")
                return Json(_stageProfiles);

            if (request.Method == HttpMethod.Post && path == "/api/admin/enemy-generation/profiles")
            {
                var dto = await ReadAsync<StageProfileRequest>(request, cancellationToken);
                LastStageProfileId = Guid.NewGuid();
                _stageProfiles.Add(new StageProfile
                {
                    Id = LastStageProfileId,
                    Name = dto.Name,
                    StageIndex = dto.StageIndex,
                    Weight = dto.Weight,
                    Falloff = dto.Falloff,
                    Threshold = dto.Threshold
                });
                return Json(LastStageProfileId);
            }

            if (request.Method == HttpMethod.Delete && TryMatchGuid(path, "/api/admin/enemy-generation/profiles/", out var profileId))
            {
                _stageProfiles.RemoveAll(x => x.Id == profileId);
                _scenarios.RemoveAll(x => x.StageProfileId == profileId);
                return Json(new { ok = true });
            }

            if (request.Method == HttpMethod.Get && TryMatchGuid(path, "/api/admin/enemy-generation/profiles/", "/scenarios", out profileId))
                return Json(_scenarios.Where(x => x.StageProfileId == profileId).ToList());

            if (request.Method == HttpMethod.Post && TryMatchGuid(path, "/api/admin/enemy-generation/profiles/", "/scenarios", out profileId))
            {
                var dto = await ReadAsync<ScenarioRequest>(request, cancellationToken);
                LastScenarioId = Guid.NewGuid();
                _scenarios.Add(new StageScenario
                {
                    Id = LastScenarioId,
                    StageProfileId = profileId,
                    EnemyCount = dto.EnemyCount,
                    Weight = dto.Weight
                });
                return Json(LastScenarioId);
            }

            if (request.Method == HttpMethod.Delete && TryMatchGuid(path, "/api/admin/enemy-generation/scenarios/", out var scenarioId))
            {
                _scenarios.RemoveAll(x => x.Id == scenarioId);
                _slots.RemoveAll(x => x.ScenarioId == scenarioId);
                return Json(new { ok = true });
            }

            if (request.Method == HttpMethod.Get && TryMatchGuid(path, "/api/admin/enemy-generation/scenarios/", "/slots", out scenarioId))
                return Json(_slots.Where(x => x.ScenarioId == scenarioId).ToList());

            if (request.Method == HttpMethod.Post && TryMatchGuid(path, "/api/admin/enemy-generation/scenarios/", "/slots", out scenarioId))
            {
                var dto = await ReadAsync<SlotRequest>(request, cancellationToken);
                LastSlotId = Guid.NewGuid();
                _slots.Add(new ScenarioSlot
                {
                    Id = LastSlotId,
                    ScenarioId = scenarioId,
                    Position = dto.Position,
                    ClassProfileId = dto.ClassProfileId,
                    Weight = dto.Weight
                });
                return Json(LastSlotId);
            }

            if (request.Method == HttpMethod.Put && path == "/api/admin/enemy-generation/slots")
            {
                var dto = await ReadAsync<UpdateSlotRequest>(request, cancellationToken);
                var slot = _slots.Single(x => x.Id == dto.Id);
                slot.Position = dto.Position;
                slot.ClassProfileId = dto.ClassProfileId;
                slot.Weight = dto.Weight;
                return Json(new { ok = true });
            }

            if (request.Method == HttpMethod.Delete && TryMatchGuid(path, "/api/admin/enemy-generation/slots/", out var slotId))
            {
                _slots.RemoveAll(x => x.Id == slotId);
                return Json(new { ok = true });
            }

            if (request.Method == HttpMethod.Get && path == "/api/admin/enemy-generation/class-profiles")
                return Json(_classProfiles);

            if (request.Method == HttpMethod.Post && path == "/api/admin/enemy-generation/class-profiles")
            {
                var dto = await ReadAsync<ClassProfileRequest>(request, cancellationToken);
                LastClassProfileId = Guid.NewGuid();
                _classProfiles.Add(new EnemyClassProfile
                {
                    Id = LastClassProfileId,
                    Name = dto.Name,
                    Class = dto.Class,
                    AllowedColumns = dto.AllowedColumns,
                    GenerationProfileId = dto.GenerationProfileId,
                    Weight = dto.Weight
                });
                return Json(LastClassProfileId);
            }

            if (request.Method == HttpMethod.Delete && TryMatchGuid(path, "/api/admin/enemy-generation/class-profiles/", out var classProfileId))
            {
                _classProfiles.RemoveAll(x => x.Id == classProfileId);
                return Json(new { ok = true });
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }

        private async Task<T> ReadAsync<T>(HttpRequestMessage request, CancellationToken cancellationToken)
            => (await request.Content!.ReadFromJsonAsync<T>(_jsonOptions, cancellationToken))!;

        private HttpResponseMessage Json<T>(T value)
            => new(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(value, options: _jsonOptions)
            };

        private static bool TryMatchUserAction(string path, string prefix, string suffix, out string userId)
        {
            userId = "";
            if (!path.StartsWith(prefix, StringComparison.Ordinal) || !path.EndsWith(suffix, StringComparison.Ordinal))
                return false;

            userId = path[prefix.Length..^suffix.Length];
            return !string.IsNullOrWhiteSpace(userId);
        }

        private static bool TryMatchGuid(string path, string prefix, out Guid id)
        {
            id = Guid.Empty;
            return path.StartsWith(prefix, StringComparison.Ordinal)
                && Guid.TryParse(path[prefix.Length..], out id);
        }

        private static bool TryMatchGuid(string path, string prefix, string suffix, out Guid id)
        {
            id = Guid.Empty;
            return path.StartsWith(prefix, StringComparison.Ordinal)
                && path.EndsWith(suffix, StringComparison.Ordinal)
                && Guid.TryParse(path[prefix.Length..^suffix.Length], out id);
        }
    }

    private sealed class RoleRequest
    {
        public int Role { get; set; }
    }

    private sealed class StageProfileRequest
    {
        public string Name { get; set; } = "";
        public int StageIndex { get; set; }
        public double Weight { get; set; }
        public double Falloff { get; set; }
        public int Threshold { get; set; }
    }

    private sealed class ScenarioRequest
    {
        public int EnemyCount { get; set; }
        public double Weight { get; set; }
    }

    private class SlotRequest
    {
        public int Position { get; set; }
        public Guid ClassProfileId { get; set; }
        public double Weight { get; set; }
    }

    private sealed class UpdateSlotRequest : SlotRequest
    {
        public Guid Id { get; set; }
    }

    private sealed class ClassProfileRequest
    {
        public string Name { get; set; } = "";
        public EnemyClass Class { get; set; }
        public List<int> AllowedColumns { get; set; } = new();
        public Guid GenerationProfileId { get; set; }
        public double Weight { get; set; }
    }
}
