using System.Reflection;
using SemanticVersioning;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Helpers.Server;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using Version = SemanticVersioning.Version;
using Range = SemanticVersioning.Range;

namespace SOCOMExpansionCore;

public record ModMetadata : IModMetadata
{
    public string ModGuid { get; init; } = "com.socom.expansion.core";
    public string Name { get; init; } = "SOCOM Expansion Core";
    public string Author { get; init; } = "SOCOM Expansion Project";
    public List<string> Contributors { get; init; } = [];
    public Version Version { get; init; } = new("0.1.0");
    public Range SptVersion { get; init; } = new("~4.1.6");
    public Dictionary<string, Range> ModDependencies { get; init; } = new()
    {
        ["com.mtmilitia.socom"] = new("~2.0.3")
    };
    public bool HasPrepatcher { get; init; } = false;
    public List<string> Incompatibilities { get; init; } = [];
    public string? Url { get; init; } = null;
    public string License { get; init; } = "MIT";
}

// Expansion modules register their own resource assembly before trader registration is applied.
// Registering is deliberately separate from item creation: WTT CommonLib still owns that.
public static class SocomTraderOffers
{
    private static readonly List<(Assembly Assembly, string ResourcePath)> Pending = [];
    public static void Register(Assembly assembly, string resourcePath = "db/TraderSOCOM/additional_offers.json")
    {
        ArgumentNullException.ThrowIfNull(assembly);
        if (string.IsNullOrWhiteSpace(resourcePath)) throw new ArgumentException("Resource path required", nameof(resourcePath));
        Pending.Add((assembly, resourcePath));
    }
    internal static IReadOnlyList<(Assembly Assembly, string ResourcePath)> GetPending() => Pending;
}

[Injectable(TypePriority = OnLoadOrder.TraderRegistration + 3)]
public class ApplySocomOffers(ModHelper modHelper, TradersTable tradersTable) : IOnLoad
{
    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        const string socomTraderId = "69325b507cd59da087ad2912";
        var pending = SocomTraderOffers.GetPending();
        if (pending.Count == 0) return Task.CompletedTask;
        if (!tradersTable.TryGetValue(socomTraderId, out var trader))
            throw new InvalidOperationException("SOCOM trader missing. Verify SOCOM Armory dependency and load order.");

        // Preflight every offer before mutating the trader. This also catches duplicates
        // across separate expansion modules rather than only within a single module.
        var loaded = new List<TraderAssort>();
        var itemIds = trader.Assort.Items.Select(x => x.Id.ToString()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var barterIds = trader.Assort.BarterScheme.Keys.Select(x => x.ToString()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var loyaltyIds = trader.Assort.LoyalLevelItems.Keys.Select(x => x.ToString()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var (assembly, resourcePath) in pending)
        {
            var folder = modHelper.GetAbsolutePathToModFolder(assembly);
            var additions = modHelper.GetJsonDataFromFile<TraderAssort>(folder, resourcePath);
            foreach (var item in additions.Items)
                if (!itemIds.Add(item.Id.ToString()))
                    throw new InvalidOperationException($"Duplicate SOCOM offer ID: {item.Id}");
            foreach (var key in additions.BarterScheme.Keys)
                if (!barterIds.Add(key.ToString()))
                    throw new InvalidOperationException($"Duplicate SOCOM barter key: {key}");
            foreach (var key in additions.LoyalLevelItems.Keys)
                if (!loyaltyIds.Add(key.ToString()))
                    throw new InvalidOperationException($"Duplicate SOCOM loyalty key: {key}");
            loaded.Add(additions);
        }
        foreach (var additions in loaded)
        {
            foreach (var item in additions.Items) trader.Assort.Items.Add(item);
            foreach (var entry in additions.BarterScheme) trader.Assort.BarterScheme.Add(entry.Key, entry.Value);
            foreach (var entry in additions.LoyalLevelItems) trader.Assort.LoyalLevelItems.Add(entry.Key, entry.Value);
        }
        return Task.CompletedTask;
    }
}
