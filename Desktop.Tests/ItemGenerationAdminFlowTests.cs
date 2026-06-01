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
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using desktop_app;
using desktop_app.Enums;
using desktop_app.Models.Generation;
using desktop_app.Services.Generation;
using desktop_app.ViewModels.Generation;
using desktop_app.Views;
using Xunit;

public class ItemGenerationAdminFlowTests
{
    [AvaloniaFact]
    public async Task ItemGenerationAdmin_CanManageFullGenerationFlow()
    {
        DesktopAvaloniaTestApp.EnsureStarted();
        var api = new GenerationAdminApiStub();
        using var client = new HttpClient(api) { BaseAddress = new Uri("https://api.test") };
        var viewModel = new ItemGenerationViewModel(new ItemGenerationAdminService("desktop-token", client));
        var window = CreateWindow(viewModel);

        await viewModel.LoadProfilesAsync();
        await SelectProfileAsync(viewModel, viewModel.Profiles.Single(), api);

        Assert.Contains("Seed Profile", VisibleText(window));
        Assert.Contains("GET /api/admin/generation/profiles", api.Calls);
        Assert.Contains($"GET /api/admin/generation/profiles/{api.SeedProfileId}/rules", api.Calls);

        await viewModel.UpdateSelectedProfileAsync("Renamed Profile");
        await WaitForAsync(() => viewModel.SelectedProfile?.Name == "Renamed Profile");
        Assert.Contains("PUT /api/admin/generation/profiles", api.Calls);
        Assert.Contains("Renamed Profile", VisibleText(window));

        await viewModel.CreateTypeWeightAsync(ItemCategory.Weapon, WeaponType.Sword, null, 10);
        await WaitForAsync(() => viewModel.Weights.Count == 1);
        Assert.Contains("POST /api/admin/generation/profiles/" + api.SeedProfileId + "/weights", api.Calls);
        Assert.Equal(10, viewModel.Weights[0].Weight);

        await viewModel.UpdateTypeWeightAsync(viewModel.Weights[0], ItemCategory.Weapon, WeaponType.Bow, null, 25);
        await WaitForAsync(() => viewModel.Weights[0].Weight == 25);
        Assert.Contains("PUT /api/admin/generation/weights", api.Calls);
        Assert.Equal(WeaponType.Bow, viewModel.Weights[0].WeaponType);

        await viewModel.CreateRuleAsync(ItemCategory.Weapon, WeaponType.Sword, null, true);
        await WaitForAsync(() => viewModel.Rules.Count == 1);
        Assert.Contains("POST /api/admin/generation/profiles/" + api.SeedProfileId + "/rules", api.Calls);
        Assert.Equal(WeaponType.Sword, viewModel.Rules[0].WeaponType);

        await viewModel.UpdateRuleAsync(viewModel.Rules[0], ItemCategory.Weapon, WeaponType.Bow, null, false);
        await WaitForAsync(() => viewModel.Rules[0].WeaponType == WeaponType.Bow);
        Assert.Contains("PUT /api/admin/generation/rules", api.Calls);

        var rule = viewModel.Rules[0];
        await viewModel.CreateParameterAsync(
            rule,
            ItemParameter.CutDamage,
            new List<CreateSegmentInput>
            {
                new() { Min = 1, Max = 2, Weight = 3 }
            });
        await WaitForAsync(() => viewModel.Rules[0].Parameters.Count == 1);
        Assert.Contains("POST /api/admin/generation/rules/" + rule.Id + "/parameters", api.Calls);
        Assert.Equal(ItemParameter.CutDamage, viewModel.Rules[0].Parameters[0].Parameter);

        await viewModel.UpdateParameterAsync(
            viewModel.Rules[0].Parameters[0],
            ItemParameter.BluntDamage,
            new List<CreateSegmentInput>
            {
                new() { Min = 10, Max = 20, Weight = 30 },
                new() { Min = 40, Max = 50, Weight = 60 }
            });
        await WaitForAsync(() => viewModel.Rules[0].Parameters[0].Parameter == ItemParameter.BluntDamage);
        Assert.Contains("PUT /api/admin/generation/parameters", api.Calls);
        Assert.Equal(ItemParameter.BluntDamage, viewModel.Rules[0].Parameters[0].Parameter);
        Assert.Equal(2, viewModel.Rules[0].Parameters[0].Segments.Count);

        await viewModel.CreateElementAsync(
            viewModel.Rules[0],
            ItemElementType.Fire,
            new List<CreateSegmentInput>
            {
                new() { Min = 4, Max = 5, Weight = 6 }
            });
        await WaitForAsync(() => viewModel.Rules[0].Elements.Count == 1);
        Assert.Contains("POST /api/admin/generation/rules/" + rule.Id + "/elements", api.Calls);
        Assert.Equal(ItemElementType.Fire, viewModel.Rules[0].Elements[0].ElementType);

        await viewModel.UpdateElementAsync(
            viewModel.Rules[0].Elements[0],
            ItemElementType.Water,
            new List<CreateSegmentInput>
            {
                new() { Min = 7, Max = 8, Weight = 9 }
            });
        await WaitForAsync(() => viewModel.Rules[0].Elements[0].ElementType == ItemElementType.Water);
        Assert.Contains("PUT /api/admin/generation/elements", api.Calls);
        Assert.Equal(ItemElementType.Water, viewModel.Rules[0].Elements[0].ElementType);

        await viewModel.DeleteParameterAsync(viewModel.Rules[0].Parameters[0]);
        await WaitForAsync(() => viewModel.Rules[0].Parameters.Count == 0);
        Assert.Contains("DELETE /api/admin/generation/parameters/" + api.LastParameterId, api.Calls);

        await viewModel.DeleteElementAsync(viewModel.Rules[0].Elements[0]);
        await WaitForAsync(() => viewModel.Rules[0].Elements.Count == 0);
        Assert.Contains("DELETE /api/admin/generation/elements/" + api.LastElementId, api.Calls);

        await viewModel.DeleteRuleAsync(viewModel.Rules[0]);
        await WaitForAsync(() => viewModel.Rules.Count == 0);
        Assert.Contains("DELETE /api/admin/generation/rules/" + api.LastRuleId, api.Calls);

        await viewModel.DeleteTypeWeightAsync(viewModel.Weights[0]);
        await WaitForAsync(() => viewModel.Weights.Count == 0);
        Assert.Contains("DELETE /api/admin/generation/weights/" + api.LastWeightId, api.Calls);

        await viewModel.DeleteSelectedProfileAsync();
        await WaitForAsync(() => viewModel.Profiles.Count == 0 && viewModel.SelectedProfile == null);
        Assert.Contains("DELETE /api/admin/generation/profiles/" + api.SeedProfileId, api.Calls);
    }

    [AvaloniaFact]
    public async Task ItemGenerationAdmin_ShowsEmptyStateWhenNoProfilesExist()
    {
        DesktopAvaloniaTestApp.EnsureStarted();
        var api = new GenerationAdminApiStub(seedProfile: false);
        using var client = new HttpClient(api) { BaseAddress = new Uri("https://api.test") };
        var viewModel = new ItemGenerationViewModel(new ItemGenerationAdminService("desktop-token", client));
        var window = CreateWindow(viewModel);

        await viewModel.LoadProfilesAsync();

        Assert.Empty(viewModel.Profiles);
        Assert.Equal("Loaded 0 profiles.", viewModel.StatusMessage);
        Assert.DoesNotContain("Details", VisibleText(window));
        Assert.Contains("GET /api/admin/generation/profiles", api.Calls);
    }

    [AvaloniaFact]
    public async Task ItemGenerationAdmin_ApiUnavailableShowsLoadFailure()
    {
        DesktopAvaloniaTestApp.EnsureStarted();
        var api = new GenerationAdminApiStub { FailProfilesRequest = true };
        using var client = new HttpClient(api) { BaseAddress = new Uri("https://api.test") };
        var viewModel = new ItemGenerationViewModel(new ItemGenerationAdminService("desktop-token", client));

        await viewModel.LoadProfilesAsync();

        Assert.Empty(viewModel.Profiles);
        Assert.Equal("Failed to load profiles.", viewModel.StatusMessage);
        Assert.Contains("GET /api/admin/generation/profiles", api.Calls);
    }

    [AvaloniaFact]
    public async Task ItemGenerationAdmin_UnauthorizedTokenShowsLoadFailure()
    {
        DesktopAvaloniaTestApp.EnsureStarted();
        var api = new GenerationAdminApiStub();
        using var client = new HttpClient(api) { BaseAddress = new Uri("https://api.test") };
        var viewModel = new ItemGenerationViewModel(new ItemGenerationAdminService("wrong-token", client));

        await viewModel.LoadProfilesAsync();

        Assert.Empty(viewModel.Profiles);
        Assert.Equal("Failed to load profiles.", viewModel.StatusMessage);
        Assert.Contains("GET /api/admin/generation/profiles", api.Calls);
    }

    [AvaloniaFact]
    public async Task ItemGenerationAdmin_CommandsWithoutSelectedProfileDoNotCallMutationEndpoints()
    {
        DesktopAvaloniaTestApp.EnsureStarted();
        var api = new GenerationAdminApiStub();
        using var client = new HttpClient(api) { BaseAddress = new Uri("https://api.test") };
        var viewModel = new ItemGenerationViewModel(new ItemGenerationAdminService("desktop-token", client));

        await viewModel.CreateProfileAsync("   ");
        await viewModel.CreateRuleAsync(ItemCategory.Weapon, WeaponType.Sword, null, true);
        await viewModel.CreateTypeWeightAsync(ItemCategory.Weapon, WeaponType.Sword, null, 10);
        await viewModel.DeleteSelectedProfileAsync();
        await viewModel.UpdateSelectedProfileAsync("No selection");

        Assert.Empty(api.Calls);
        Assert.Empty(viewModel.Rules);
        Assert.Empty(viewModel.Weights);
        Assert.Null(viewModel.SelectedProfile);
    }

    [AvaloniaFact]
    public async Task ItemGenerationAdmin_ClearSelectionHidesDetailsAndResetsNestedState()
    {
        DesktopAvaloniaTestApp.EnsureStarted();
        var api = new GenerationAdminApiStub();
        using var client = new HttpClient(api) { BaseAddress = new Uri("https://api.test") };
        var viewModel = new ItemGenerationViewModel(new ItemGenerationAdminService("desktop-token", client));
        var window = CreateWindow(viewModel);

        await viewModel.LoadProfilesAsync();
        await SelectProfileAsync(viewModel, viewModel.Profiles.Single(), api);

        Assert.True(viewModel.HasSelectedProfile);
        Assert.Contains("Seed Profile", VisibleText(window));

        viewModel.ClearSelection();

        Assert.False(viewModel.HasSelectedProfile);
        Assert.Null(viewModel.SelectedProfile);
        Assert.Empty(viewModel.Rules);
        Assert.Empty(viewModel.Weights);
    }

    private static Window CreateWindow(ItemGenerationViewModel viewModel)
    {
        var window = new Window
        {
            Width = 1280,
            Height = 800,
            Content = new ItemGenerationView { DataContext = viewModel }
        };

        window.Show();
        return window;
    }

    private static async Task SelectProfileAsync(
        ItemGenerationViewModel viewModel,
        GenerationProfile profile,
        GenerationAdminApiStub api)
    {
        viewModel.SelectProfileCommand.Execute(profile);
        await WaitForAsync(() =>
            viewModel.SelectedProfile?.Id == profile.Id &&
            api.Calls.Contains($"GET /api/admin/generation/profiles/{profile.Id}/rules") &&
            api.Calls.Contains($"GET /api/admin/generation/profiles/{profile.Id}/weights"));
    }

    private static List<string> VisibleText(Window window)
        => window.GetVisualDescendants()
            .OfType<TextBlock>()
            .Select(x => x.Text)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!)
            .ToList();

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

    private sealed class GenerationAdminApiStub : HttpMessageHandler
    {
        private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);
        private readonly List<GenerationProfile> _profiles = new();
        private readonly List<GenerationRule> _rules = new();
        private readonly List<TypeWeight> _weights = new();
        private readonly List<GenerationParameter> _parameters = new();
        private readonly List<GenerationElement> _elements = new();

        public Guid SeedProfileId { get; } = Guid.NewGuid();
        public Guid LastRuleId { get; private set; }
        public Guid LastWeightId { get; private set; }
        public Guid LastParameterId { get; private set; }
        public Guid LastElementId { get; private set; }
        public List<string> Calls { get; } = new();

        public bool FailProfilesRequest { get; set; }

        public GenerationAdminApiStub(bool seedProfile = true)
        {
            if (seedProfile)
                _profiles.Add(new GenerationProfile { Id = SeedProfileId, Name = "Seed Profile" });
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var path = request.RequestUri!.AbsolutePath;
            Calls.Add($"{request.Method.Method} {path}");

            if (request.Headers.Authorization?.Scheme != "Bearer" ||
                request.Headers.Authorization.Parameter != "desktop-token")
            {
                return new HttpResponseMessage(HttpStatusCode.Unauthorized);
            }

            if (request.Method == HttpMethod.Get && path == "/api/admin/generation/profiles" && FailProfilesRequest)
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);

            if (request.Method == HttpMethod.Get && path == "/api/admin/generation/profiles")
                return Json(_profiles);

            if (request.Method == HttpMethod.Post && path == "/api/admin/generation/profiles")
            {
                var dto = await ReadAsync<NameRequest>(request, cancellationToken);
                var id = Guid.NewGuid();
                _profiles.Add(new GenerationProfile { Id = id, Name = dto.Name });
                return Json(id);
            }

            if (request.Method == HttpMethod.Put && path == "/api/admin/generation/profiles")
            {
                var dto = await ReadAsync<UpdateProfileRequest>(request, cancellationToken);
                _profiles.Single(x => x.Id == dto.Id).Name = dto.Name;
                return Json(new { ok = true });
            }

            if (request.Method == HttpMethod.Delete && TryMatchId(path, "/api/admin/generation/profiles/", out var profileId))
            {
                _profiles.RemoveAll(x => x.Id == profileId);
                _rules.RemoveAll(x => x.Id == profileId);
                _weights.RemoveAll(x => x.Id == profileId);
                return Json(new { ok = true });
            }

            if (request.Method == HttpMethod.Get && TryMatchId(path, "/api/admin/generation/profiles/", "/rules", out profileId))
                return Json(_rules);

            if (request.Method == HttpMethod.Post && TryMatchId(path, "/api/admin/generation/profiles/", "/rules", out profileId))
            {
                var dto = await ReadAsync<RuleRequest>(request, cancellationToken);
                LastRuleId = Guid.NewGuid();
                _rules.Add(new GenerationRule
                {
                    Id = LastRuleId,
                    Category = dto.Category,
                    WeaponType = dto.WeaponType,
                    ArmorType = dto.ArmorType,
                    IsFallback = dto.IsFallback
                });
                return Json(LastRuleId);
            }

            if (request.Method == HttpMethod.Put && path == "/api/admin/generation/rules")
            {
                var dto = await ReadAsync<UpdateRuleRequest>(request, cancellationToken);
                var rule = _rules.Single(x => x.Id == dto.Id);
                rule.Category = dto.Category;
                rule.WeaponType = dto.WeaponType;
                rule.ArmorType = dto.ArmorType;
                rule.IsFallback = dto.IsFallback;
                return Json(new { ok = true });
            }

            if (request.Method == HttpMethod.Delete && TryMatchId(path, "/api/admin/generation/rules/", out var ruleId))
            {
                _rules.RemoveAll(x => x.Id == ruleId);
                _parameters.RemoveAll(x => x.Id == ruleId);
                _elements.RemoveAll(x => x.Id == ruleId);
                return Json(new { ok = true });
            }

            if (request.Method == HttpMethod.Get && TryMatchId(path, "/api/admin/generation/profiles/", "/weights", out profileId))
                return Json(_weights);

            if (request.Method == HttpMethod.Post && TryMatchId(path, "/api/admin/generation/profiles/", "/weights", out profileId))
            {
                var dto = await ReadAsync<TypeWeightRequest>(request, cancellationToken);
                LastWeightId = Guid.NewGuid();
                _weights.Add(new TypeWeight
                {
                    Id = LastWeightId,
                    Category = dto.Category,
                    WeaponType = dto.WeaponType,
                    ArmorType = dto.ArmorType,
                    Weight = dto.Weight
                });
                return Json(LastWeightId);
            }

            if (request.Method == HttpMethod.Put && path == "/api/admin/generation/weights")
            {
                var dto = await ReadAsync<UpdateTypeWeightRequest>(request, cancellationToken);
                var weight = _weights.Single(x => x.Id == dto.Id);
                weight.Category = dto.Category;
                weight.WeaponType = dto.WeaponType;
                weight.ArmorType = dto.ArmorType;
                weight.Weight = dto.Weight;
                return Json(new { ok = true });
            }

            if (request.Method == HttpMethod.Delete && TryMatchId(path, "/api/admin/generation/weights/", out var weightId))
            {
                _weights.RemoveAll(x => x.Id == weightId);
                return Json(new { ok = true });
            }

            if (request.Method == HttpMethod.Get && TryMatchId(path, "/api/admin/generation/rules/", "/parameters", out ruleId))
                return Json(_parameters);

            if (request.Method == HttpMethod.Post && TryMatchId(path, "/api/admin/generation/rules/", "/parameters", out ruleId))
            {
                var dto = await ReadAsync<ParameterRequest>(request, cancellationToken);
                LastParameterId = Guid.NewGuid();
                _parameters.Add(new GenerationParameter
                {
                    Id = LastParameterId,
                    Parameter = dto.Parameter,
                    Segments = dto.Segments.Select(ToSegment).ToList()
                });
                return Json(LastParameterId);
            }

            if (request.Method == HttpMethod.Put && path == "/api/admin/generation/parameters")
            {
                var dto = await ReadAsync<UpdateParameterRequest>(request, cancellationToken);
                var parameter = _parameters.Single(x => x.Id == dto.Id);
                parameter.Parameter = dto.Parameter;
                parameter.Segments = dto.Segments.Select(ToSegment).ToList();
                return Json(new { ok = true });
            }

            if (request.Method == HttpMethod.Delete && TryMatchId(path, "/api/admin/generation/parameters/", out var parameterId))
            {
                _parameters.RemoveAll(x => x.Id == parameterId);
                return Json(new { ok = true });
            }

            if (request.Method == HttpMethod.Get && TryMatchId(path, "/api/admin/generation/rules/", "/elements", out ruleId))
                return Json(_elements);

            if (request.Method == HttpMethod.Post && TryMatchId(path, "/api/admin/generation/rules/", "/elements", out ruleId))
            {
                var dto = await ReadAsync<ElementRequest>(request, cancellationToken);
                LastElementId = Guid.NewGuid();
                _elements.Add(new GenerationElement
                {
                    Id = LastElementId,
                    ElementType = dto.ElementType,
                    Segments = dto.Segments.Select(ToSegment).ToList()
                });
                return Json(LastElementId);
            }

            if (request.Method == HttpMethod.Put && path == "/api/admin/generation/elements")
            {
                var dto = await ReadAsync<UpdateElementRequest>(request, cancellationToken);
                var element = _elements.Single(x => x.Id == dto.Id);
                element.ElementType = dto.ElementType;
                element.Segments = dto.Segments.Select(ToSegment).ToList();
                return Json(new { ok = true });
            }

            if (request.Method == HttpMethod.Delete && TryMatchId(path, "/api/admin/generation/elements/", out var elementId))
            {
                _elements.RemoveAll(x => x.Id == elementId);
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

        private static Segment ToSegment(CreateSegmentInput input)
            => new()
            {
                Id = Guid.NewGuid(),
                Min = input.Min,
                Max = input.Max,
                Weight = input.Weight
            };

        private static bool TryMatchId(string path, string prefix, out Guid id)
        {
            id = Guid.Empty;
            return path.StartsWith(prefix, StringComparison.Ordinal)
                && Guid.TryParse(path[prefix.Length..], out id);
        }

        private static bool TryMatchId(string path, string prefix, string suffix, out Guid id)
        {
            id = Guid.Empty;
            return path.StartsWith(prefix, StringComparison.Ordinal)
                && path.EndsWith(suffix, StringComparison.Ordinal)
                && Guid.TryParse(path[prefix.Length..^suffix.Length], out id);
        }
    }

    private class NameRequest
    {
        public string Name { get; set; } = "";
    }

    private sealed class UpdateProfileRequest : NameRequest
    {
        public Guid Id { get; set; }
    }

    private class RuleRequest
    {
        public ItemCategory Category { get; set; }
        public WeaponType? WeaponType { get; set; }
        public ArmorType? ArmorType { get; set; }
        public bool IsFallback { get; set; }
    }

    private sealed class UpdateRuleRequest : RuleRequest
    {
        public Guid Id { get; set; }
    }

    private class TypeWeightRequest
    {
        public ItemCategory Category { get; set; }
        public WeaponType? WeaponType { get; set; }
        public ArmorType? ArmorType { get; set; }
        public double Weight { get; set; }
    }

    private sealed class UpdateTypeWeightRequest : TypeWeightRequest
    {
        public Guid Id { get; set; }
    }

    private class ParameterRequest
    {
        public ItemParameter Parameter { get; set; }
        public List<CreateSegmentInput> Segments { get; set; } = new();
    }

    private sealed class UpdateParameterRequest : ParameterRequest
    {
        public Guid Id { get; set; }
    }

    private class ElementRequest
    {
        public ItemElementType ElementType { get; set; }
        public List<CreateSegmentInput> Segments { get; set; } = new();
    }

    private sealed class UpdateElementRequest : ElementRequest
    {
        public Guid Id { get; set; }
    }
}

public static class DesktopAvaloniaTestApp
{
    private static bool _started;

    public static void EnsureStarted()
    {
        if (_started)
            return;

        BuildAvaloniaApp().SetupWithoutStarting();
        _started = true;
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions());
}
