using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace QualityOfBuilding.Source.Systems;

public class ServerSettings
{
    public const int ActualVersion = 1;

    public int Version { get; set; } = 0;

    public int StonesRequiredForPath { get; set; } = 4;

    public string[] ReplacableBlocksForPath { get; set; } =
    [
        "soil-*",
        "forestfloor-*",
        "sand-*",
        "gravel-*",
        "drypackeddirt",
        "packeddirt",
        "rammed-light-*",
        "trailmodupdated:soil-*",
        "trailmodupdated:trail-*",
    ];

    public string[] ReplacableSlabsForPath { get; set; } =
    ["terrainslabs:soil-*", "terrainslabs:trail-*", "terrainslabs:forestfloor-*", "terrainslabs:sand-*", "terrainslabs:gravel-*"];

    public string[] ShovelReplacableSoilBlocks { get; set; } =
    [
        "trailmodupdated:soil-*",
        "trailmodupdated:trail-*",
        "terrainslabs:trail-*",
        "terrainslabs:soil-*",
        "terrainslabs:forestfloor-*",
        "game:soil-*",
        "game:forestfloor-*",
        "drypackeddirt",
        "packeddirt",
        "rammed-light-*",
    ];

    public string[] ShovelBlocksToReplaceSoil { get; set; } =
    [
        "drypackeddirt",
        "packeddirt",
        "rammed-light-plain",
        "rammed-light-thickheavy",
        "rammed-light-thicklight",
        "rammed-light-thinheavy",
        "rammed-light-thinlight",
    ];
}

internal class ConfigSystem : ModSystem
{
    private const string fileName = "qualityofbuilding-server.json";

    public ServerSettings ServerSettings { get; private set; } = new();

    public override void StartServerSide(ICoreServerAPI api)
    {
        LoadConfig(api);
    }

    public override void StartClientSide(ICoreClientAPI api)
    {
        ServerSettings.ReplacableBlocksForPath = GetStringsFromWorldConfig(api, nameof(ServerSettings.ReplacableBlocksForPath));
        ServerSettings.ReplacableSlabsForPath = GetStringsFromWorldConfig(api, nameof(ServerSettings.ReplacableSlabsForPath));
        ServerSettings.ShovelReplacableSoilBlocks = GetStringsFromWorldConfig(api, nameof(ServerSettings.ShovelReplacableSoilBlocks));
        ServerSettings.ShovelBlocksToReplaceSoil = GetStringsFromWorldConfig(api, nameof(ServerSettings.ShovelBlocksToReplaceSoil));
        ServerSettings.StonesRequiredForPath = api.World.Config.GetInt(
            GetWorldConfigName(nameof(ServerSettings.StonesRequiredForPath)),
            ServerSettings.StonesRequiredForPath
        );
    }

    public void SaveConfig(ICoreServerAPI api)
    {
        api.StoreModConfig(ServerSettings, fileName);
        SetStringsToWorldConfig(api, nameof(ServerSettings.ReplacableBlocksForPath), ServerSettings.ReplacableBlocksForPath);
        SetStringsToWorldConfig(api, nameof(ServerSettings.ReplacableSlabsForPath), ServerSettings.ReplacableSlabsForPath);
        SetStringsToWorldConfig(api, nameof(ServerSettings.ShovelReplacableSoilBlocks), ServerSettings.ShovelReplacableSoilBlocks);
        SetStringsToWorldConfig(api, nameof(ServerSettings.ShovelBlocksToReplaceSoil), ServerSettings.ShovelBlocksToReplaceSoil);
        api.World.Config.SetInt(GetWorldConfigName(nameof(ServerSettings.StonesRequiredForPath)), ServerSettings.StonesRequiredForPath);
    }

    private static string[] GetStringsFromWorldConfig(ICoreAPI api, string name)
    {
        return api.World.Config.GetString(GetWorldConfigName(name), string.Empty).Split('|');
    }

    private static void SetStringsToWorldConfig(ICoreAPI api, string name, string[] values)
    {
        api.World.Config.SetString(GetWorldConfigName(name), string.Join('|', values));
    }

    private static string GetWorldConfigName(string field)
    {
        return "qualityofbuilding-" + field.ToLowerInvariant();
    }

    private void LoadConfig(ICoreServerAPI api)
    {
        try
        {
            ServerSettings settings = api.LoadModConfig<ServerSettings>(fileName);
            if (settings is not null && settings.Version == ServerSettings.ActualVersion)
            {
                ServerSettings = settings;
            }
            ServerSettings.Version = ServerSettings.ActualVersion;
            SaveConfig(api);
        }
        catch (Exception e)
        {
            Mod.Logger.Warning("Could not load config from {0}, loading default settings instead.", fileName);
            Mod.Logger.Warning(e);
        }
    }
}
