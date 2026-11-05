using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers.Server;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Utils.Json;

namespace SWAG;

[Injectable(TypePriority = OnLoadOrder.PostLoad + 1)]
public class SwagMod(
    ISptLogger<SwagMod> logger,
    LocationTable locationTable,
    BotConfig botConfig,
    LocationConfig locationConfig,
    ModHelper modHelper) : IOnLoad
{
    private static readonly Random Rng = new();
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    internal JsonNode? Config;
    internal JsonNode? BossConfigNode;

    private readonly Dictionary<string, List<JsonNode>> _bossSpawnConfigs = new();
    private readonly Dictionary<string, List<JsonNode>> _otherSpawnConfigs = new();
    private readonly Dictionary<string, List<JsonNode>> _customSpawnConfigs = new();

    private readonly Dictionary<string, LocationBackup> _savedLocationData = new();
    private int _bossCount;

    private static readonly string[] BossFiles =
        ["gluhar", "goons", "kaban", "killa", "kolontay", "reshala", "sanitar", "shturman", "tagilla", "zryachiy", "partisan"];

    private static readonly string[] OtherFiles =
        ["bloodhounds", "cultists", "raiders", "rogues", "scav_snipers"];

    private static readonly string[] CustomFiles =
        ["punisher", "legion", "santa"];

    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        var modPath = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
        LoadConfigs(modPath);
        DisableVanillaSpawnSettings();
        logger.Success("SWAG: Configs loaded and vanilla spawn settings disabled");
        return Task.CompletedTask;
    }

    private void LoadConfigs(string modPath)
    {
        var configPath = Path.Combine(modPath, "config");
        Config = JsonNode.Parse(File.ReadAllText(Path.Combine(configPath, "config.json")));
        BossConfigNode = JsonNode.Parse(File.ReadAllText(Path.Combine(configPath, "bossConfig.json")));

        foreach (var file in BossFiles)
            LoadSpawnFile(Path.Combine(configPath, "bosses", $"{file}.json"), _bossSpawnConfigs);

        foreach (var file in OtherFiles)
            LoadSpawnFile(Path.Combine(configPath, "other", $"{file}.json"), _otherSpawnConfigs);

        foreach (var file in CustomFiles)
        {
            var filePath = Path.Combine(configPath, "custom", $"{file}.json");
            if (File.Exists(filePath))
                LoadSpawnFile(filePath, _customSpawnConfigs);
        }
    }

    private static void LoadSpawnFile(string filePath, Dictionary<string, List<JsonNode>> target)
    {
        if (!File.Exists(filePath)) return;
        var doc = JsonNode.Parse(File.ReadAllText(filePath));
        if (doc is not JsonObject obj) return;

        foreach (var (mapKey, value) in obj)
        {
            if (value is not JsonArray arr) continue;
            if (!target.ContainsKey(mapKey))
                target[mapKey] = new List<JsonNode>();
            foreach (var item in arr)
            {
                if (item != null)
                    target[mapKey].Add(item.DeepClone());
            }
        }
    }

    private void DisableVanillaSpawnSettings()
    {
        var useDefaults = Config?["UseDefaultSpawns"];
        bool useWaves = useDefaults?["Waves"]?.GetValue<bool>() ?? true;
        bool useBosses = useDefaults?["Bosses"]?.GetValue<bool>() ?? true;
        bool useTriggered = useDefaults?["TriggeredWaves"]?.GetValue<bool>() ?? true;

        if (!useWaves || !useBosses || !useTriggered)
        {
            DisableSpawnSystems();
        }

        locationConfig.RogueLighthouseSpawnTimeSettings.Enabled = false;
        locationConfig.AddOpenZonesToAllMaps = false;
        locationConfig.AddCustomBotWavesToMaps = false;
        locationConfig.EnableBotTypeLimits = false;
    }

    internal void DisableSpawnSystems()
    {
        var locations = locationTable.GetDictionary();
        foreach (var (_, loc) in locations)
        {
            var locBase = loc?.Base;
            if (locBase == null) continue;
            locBase.NewSpawn = false;
            locBase.OfflineNewSpawn = false;
            locBase.OldSpawn = true;
            locBase.OfflineOldSpawn = true;
        }
        logger.Info("SWAG: Spawn Systems Set");
    }

    internal void ClearDefaultSpawns()
    {
        var locations = locationTable.GetDictionary();
        foreach (var (mapName, loc) in locations)
        {
            if (loc?.Base == null) continue;
            var mapId = mapName.ToLowerInvariant();
            if (mapId == "base" || mapId == "hideout") continue;

            var locBase = loc.Base;

            if (!_savedLocationData.ContainsKey(mapId))
            {
                _savedLocationData[mapId] = new LocationBackup
                {
                    Waves = locBase.Waves?.ToList() ?? [],
                    BossLocationSpawn = locBase.BossLocationSpawn?.ToList() ?? [],
                    OpenZones = GetOpenZones(locBase)
                };
            }

            SetPmcHostility(locBase);

            var backup = _savedLocationData[mapId];
            locBase.Waves = new List<Wave>(backup.Waves);
            locBase.BossLocationSpawn = new List<BossLocationSpawn>(backup.BossLocationSpawn);

            var useDefaults = Config?["UseDefaultSpawns"];
            bool useWaves = useDefaults?["Waves"]?.GetValue<bool>() ?? true;
            bool useBosses = useDefaults?["Bosses"]?.GetValue<bool>() ?? true;
            bool useTriggered = useDefaults?["TriggeredWaves"]?.GetValue<bool>() ?? true;

            if (!useWaves)
                locBase.Waves = [];

            if (!useBosses && !useTriggered)
            {
                locBase.BossLocationSpawn = [];
                continue;
            }

            if (!useBosses)
            {
                locBase.BossLocationSpawn = locBase.BossLocationSpawn
                    .Where(b => !string.IsNullOrEmpty(b.TriggerName))
                    .ToList();
            }

            if (!useTriggered)
            {
                locBase.BossLocationSpawn = locBase.BossLocationSpawn
                    .Where(b => string.IsNullOrEmpty(b.TriggerName))
                    .ToList();
            }
        }
    }

    private static void SetPmcHostility(LocationBase locBase)
    {
        var hostilitySettings = locBase.BotLocationModifier?.AdditionalHostilitySettings;
        if (hostilitySettings == null) return;

        var settingsList = hostilitySettings.ToList();
        for (int i = settingsList.Count - 1; i >= 0; i--)
        {
            var setting = settingsList[i];
            if (setting.BotRole == "pmcUSEC" || setting.BotRole == "pmcBEAR")
            {
                settingsList[i] = new AdditionalHostilitySettings
                {
                    BotRole = setting.BotRole,
                    AlwaysEnemies = new HashSet<string>(ClassDef.PmcAlwaysEnemies),
                    AlwaysFriends = new HashSet<string>(ClassDef.PmcAlwaysFriends),
                    BearEnemyChance = 100,
                    BearPlayerBehaviour = "AlwaysEnemies",
                    ChancedEnemies = [],
                    Neutral = new HashSet<string>(),
                    SavagePlayerBehaviour = "AlwaysEnemies",
                    UsecEnemyChance = 100,
                    UsecPlayerBehaviour = "AlwaysEnemies",
                    Warn = Array.Empty<string>()
                };
            }
        }
        locBase.BotLocationModifier!.AdditionalHostilitySettings = settingsList;
    }

    private static List<string> GetOpenZones(LocationBase locBase)
    {
        var zones = new List<string>();

        if (!string.IsNullOrEmpty(locBase.OpenZones))
        {
            zones.AddRange(
                locBase.OpenZones.Split(',')
                    .Select(z => z.Trim())
                    .Where(z => !string.IsNullOrEmpty(z) && !z.Contains("Snipe"))
            );
        }

        if (locBase.SpawnPointParams != null)
        {
            foreach (var spawn in locBase.SpawnPointParams)
            {
                if (!string.IsNullOrEmpty(spawn.BotZoneName) &&
                    !zones.Contains(spawn.BotZoneName) &&
                    !spawn.BotZoneName.Contains("Snipe"))
                {
                    zones.Add(spawn.BotZoneName);
                }
            }
        }

        return zones;
    }

    internal void ConfigureMaps()
    {
        var bossConfigs = BuildFilteredConfigs(_bossSpawnConfigs, FilterBossSpawn);
        var otherConfigs = BuildFilteredConfigs(_otherSpawnConfigs, FilterOtherSpawn);
        var customConfigs = BuildFilteredConfigs(_customSpawnConfigs, FilterCustomSpawn);

        ShuffleAll(bossConfigs);
        ShuffleAll(otherConfigs);
        ShuffleAll(customConfigs);

        var locations = locationTable.GetDictionary();

        foreach (var globalMap in ClassDef.ValidMaps)
        {
            if (!ClassDef.ReverseMapNames.TryGetValue(globalMap, out var friendlyName)) continue;

            var mappedKey = locationTable.GetMappedKey(globalMap);
            if (!locations.TryGetValue(mappedKey, out var loc) || loc?.Base == null) continue;

            _bossCount = 0;

            if (bossConfigs.TryGetValue(friendlyName, out var bossList))
            {
                foreach (var boss in bossList)
                {
                    SpawnBoss(boss, loc.Base, globalMap, friendlyName);
                    _bossCount++;
                }
            }

            if (otherConfigs.TryGetValue(friendlyName, out var otherList))
            {
                foreach (var spawn in otherList)
                    AddWave(spawn, loc.Base, globalMap);
            }

            if (customConfigs.TryGetValue(friendlyName, out var customList))
            {
                foreach (var custom in customList)
                    AddWave(custom, loc.Base, globalMap);
            }
        }
    }

    private Dictionary<string, List<JsonNode>> BuildFilteredConfigs(
        Dictionary<string, List<JsonNode>> source,
        Func<JsonNode, string, bool> filter)
    {
        var result = new Dictionary<string, List<JsonNode>>();
        foreach (var (mapKey, spawns) in source)
        {
            var filtered = spawns.Where(s => filter(s, mapKey)).ToList();
            if (filtered.Count > 0)
                result[mapKey] = filtered;
        }
        return result;
    }

    private bool FilterBossSpawn(JsonNode spawn, string mapKey)
    {
        var totalBosses = BossConfigNode?["TotalBossesPerMap"]?[mapKey]?.GetValue<int>() ?? -1;
        if (totalBosses == 0) return false;

        bool disableBosses = Config?["disableAllSpawns"]?["bosses"]?.GetValue<bool>() ?? false;
        if (disableBosses) return false;

        var bossName = spawn["BossName"]?.GetValue<string>()?.ToLowerInvariant() ?? "";
        if (bossName == "bossboarsniper") return false;

        var bossChance = spawn["BossChance"]?.GetValue<int>() ?? 0;
        if (bossChance == 0) return false;

        bool useGlobal = BossConfigNode?["Bosses"]?["useGlobalBossSpawnChance"]?.GetValue<bool>() ?? false;
        if (useGlobal && ClassDef.ReverseBossNames.TryGetValue(bossName, out var friendlyBossName))
        {
            var globalChance = BossConfigNode?["Bosses"]?[friendlyBossName]?[mapKey]?.GetValue<int>() ?? 0;
            if (globalChance == 0) return false;
        }

        return true;
    }

    private bool FilterOtherSpawn(JsonNode spawn, string mapKey)
    {
        var bossName = spawn["BossName"]?.GetValue<string>()?.ToLowerInvariant() ?? "";
        if (!ClassDef.ReverseBossNames.TryGetValue(bossName, out var bossType)) return true;

        bool disabled = Config?["disableAllSpawns"]?[bossType]?.GetValue<bool>() ?? false;
        if (disabled) return false;

        var bossChance = spawn["BossChance"]?.GetValue<int>() ?? 0;
        if (bossChance == 0) return false;

        bool useGlobal = Config?["Spawns"]?["useGlobalSpawnChance"]?.GetValue<bool>() ?? false;
        if (useGlobal)
        {
            var globalChance = Config?["Spawns"]?[bossType]?[mapKey]?.GetValue<int>() ?? 0;
            if (globalChance == 0) return false;
        }

        return true;
    }

    private bool FilterCustomSpawn(JsonNode spawn, string mapKey)
    {
        var bossName = spawn["BossName"]?.GetValue<string>()?.ToLowerInvariant() ?? "";

        if (!ClassDef.ReverseBossNames.TryGetValue(bossName, out var friendlyName))
            return true;

        if (bossName == "gifter")
        {
            bool santaEnabled = BossConfigNode?["CustomBosses"]?["santa"]?["enabled"]?.GetValue<bool>() ?? false;
            if (!santaEnabled) return false;

            bool forceOutside = BossConfigNode?["CustomBosses"]?["santa"]?["forceSpawnOutsideEvent"]?.GetValue<bool>() ?? false;
            if (!forceOutside) return false;
        }

        var bossChance = spawn["BossChance"]?.GetValue<int>() ?? 0;
        if (bossChance == 0) return false;

        bool enabled = BossConfigNode?["CustomBosses"]?[friendlyName]?["enabled"]?.GetValue<bool>() ?? false;
        if (!enabled) return false;

        var mapChance = BossConfigNode?["CustomBosses"]?[friendlyName]?[mapKey]?.GetValue<int>() ?? 0;
        if (enabled && mapChance == 0) return false;

        return true;
    }

    private void SpawnBoss(JsonNode boss, LocationBase locBase, string globalMap, string friendlyName)
    {
        var totalBosses = BossConfigNode?["TotalBossesPerMap"]?[friendlyName]?.GetValue<int>() ?? -1;
        bool debug = Config?["DebugOutput"]?.GetValue<bool>() ?? false;

        if (totalBosses == 0)
        {
            if (debug) logger.Info("SWAG: TotalBosses set to 0 for this map, skipping boss spawn");
            return;
        }

        if (totalBosses != -1 && _bossCount >= totalBosses)
        {
            if (debug) logger.Info("SWAG: Skipping boss spawn as total boss count has been met already");
            return;
        }

        var wave = ConfigureBossWave(boss, globalMap);
        locBase.BossLocationSpawn.Add(wave);
    }

    private void AddWave(JsonNode spawn, LocationBase locBase, string globalMap)
    {
        var wave = ConfigureBossWave(spawn, globalMap);
        locBase.BossLocationSpawn.Add(wave);
    }

    private BossLocationSpawn ConfigureBossWave(JsonNode boss, string globalMap)
    {
        var rawName = boss["BossName"]?.GetValue<string>() ?? "";
        var bossName = ClassDef.RoleCase.TryGetValue(rawName.ToLowerInvariant(), out var mapped) ? mapped : rawName;

        var diffKey = boss["BossDifficult"]?.GetValue<string>()
            ?? Config?["BossDifficulty"]?.GetValue<string>()?.ToLowerInvariant()
            ?? "normal";
        var difficulty = GetDifficulty(diffKey);

        var escortDiffKey = boss["BossEscortDifficult"]?.GetValue<string>()
            ?? Config?["BossEscortDifficulty"]?.GetValue<string>()?.ToLowerInvariant()
            ?? "normal";
        var escortDifficulty = GetDifficulty(escortDiffKey);

        var supports = BuildSupports(boss, escortDifficulty);

        int spawnChance = DetermineSpawnChance(boss, globalMap);

        string bossZone = DetermineZone(boss, globalMap);

        var escortType = boss["BossEscortType"]?.GetValue<string>() ?? "";
        escortType = ClassDef.RoleCase.TryGetValue(escortType.ToLowerInvariant(), out var mappedEscort)
            ? mappedEscort : escortType;

        var wave = new BossLocationSpawn
        {
            BossChance = spawnChance,
            BossDifficulty = difficulty,
            BossName = bossName,
            IsBossPlayer = false,
            BossEscortAmount = boss["BossEscortAmount"]?.GetValue<string>() ?? "0",
            BossEscortDifficulty = escortDifficulty,
            BossEscortType = escortType,
            BossZone = bossZone,
            ForceSpawn = boss["ForceSpawn"]?.GetValue<bool>() ?? false,
            IgnoreMaxBots = true,
            IsRandomTimeSpawn = boss["RandomTimeSpawn"]?.GetValue<bool>() ?? false,
            SpawnMode = new[] { "pve", "regular" },
            Supports = supports,
            Time = boss["Time"]?.GetValue<int>() ?? -1,
            TriggerId = boss["TriggerId"]?.GetValue<string>() ?? "",
            TriggerName = boss["TriggerName"]?.GetValue<string>() ?? ""
        };

        bool debug = Config?["DebugOutput"]?.GetValue<bool>() ?? false;
        if (spawnChance != 0 && debug)
        {
            logger.Warning($"SWAG: Configured Boss Wave: {JsonSerializer.Serialize(wave)}");
        }

        return wave;
    }

    private int DetermineSpawnChance(JsonNode boss, string globalMap)
    {
        var rawName = boss["BossName"]?.GetValue<string>()?.ToLowerInvariant() ?? "";
        if (!ClassDef.ReverseMapNames.TryGetValue(globalMap, out var friendlyMap))
            friendlyMap = globalMap;

        if (rawName.StartsWith("boss"))
            return AdjustBossSpawnChance(boss, rawName, friendlyMap);

        bool useGlobal = Config?["Spawns"]?["useGlobalSpawnChance"]?.GetValue<bool>() ?? false;
        if (useGlobal && ClassDef.ReverseBossNames.TryGetValue(rawName, out var spawnType))
        {
            return Config?["Spawns"]?[spawnType]?[friendlyMap]?.GetValue<int>() ?? 0;
        }

        return boss["BossChance"]?.GetValue<int>() ?? 0;
    }

    private int AdjustBossSpawnChance(JsonNode boss, string bossName, string friendlyMap)
    {
        if (bossName == "bosspunisher")
        {
            bool enabled = BossConfigNode?["CustomBosses"]?["punisher"]?["enabled"]?.GetValue<bool>() ?? false;
            if (!enabled) return 0;

            bool useProgress = BossConfigNode?["CustomBosses"]?["punisher"]?["useProgressSpawnChance"]?.GetValue<bool>() ?? false;
            if (!useProgress)
                return BossConfigNode?["CustomBosses"]?["punisher"]?[friendlyMap]?.GetValue<int>() ?? 0;

            return TryReadProgressFile("WTT-RogueJustice", "actualPunisherChance", 1);
        }

        if (bossName == "bosslegion")
        {
            bool enabled = BossConfigNode?["CustomBosses"]?["legion"]?["enabled"]?.GetValue<bool>() ?? false;
            if (!enabled) return 0;

            bool useProgress = BossConfigNode?["CustomBosses"]?["legion"]?["useProgressSpawnChance"]?.GetValue<bool>() ?? false;
            if (!useProgress)
                return BossConfigNode?["CustomBosses"]?["legion"]?[friendlyMap]?.GetValue<int>() ?? 0;

            return TryReadLegionProgress();
        }

        if (bossName == "bossboarsniper")
            return boss["BossChance"]?.GetValue<int>() ?? 0;

        bool useGlobalBoss = BossConfigNode?["Bosses"]?["useGlobalBossSpawnChance"]?.GetValue<bool>() ?? false;
        if (useGlobalBoss && ClassDef.ReverseBossNames.TryGetValue(bossName, out var friendlyBossName))
        {
            return BossConfigNode?["Bosses"]?[friendlyBossName]?[friendlyMap]?.GetValue<int>() ?? 0;
        }

        return boss["BossChance"]?.GetValue<int>() ?? 0;
    }

    private int TryReadProgressFile(string modFolder, string propertyName, int defaultValue)
    {
        try
        {
            var modPath = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
            var modsRoot = Path.GetDirectoryName(modPath);
            if (modsRoot == null) return defaultValue;

            var progressDir = Path.Combine(modsRoot, modFolder, "profiles");
            if (!Directory.Exists(progressDir)) return defaultValue;

            var profileDirs = Directory.GetDirectories(progressDir);
            foreach (var dir in profileDirs)
            {
                var progressFile = Path.Combine(dir, "progress.json");
                if (File.Exists(progressFile))
                {
                    var doc = JsonNode.Parse(File.ReadAllText(progressFile));
                    return doc?[propertyName]?.GetValue<int>() ?? defaultValue;
                }
            }
        }
        catch (Exception)
        {
            logger.Warning($"SWAG: Unable to load {modFolder} progress file");
        }
        return defaultValue;
    }

    private int TryReadLegionProgress()
    {
        try
        {
            var modPath = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
            var modsRoot = Path.GetDirectoryName(modPath);
            if (modsRoot == null) return 15;

            var legionFile = Path.Combine(modsRoot, "RaidOverhaul", "config", "LegionChance.json");
            if (File.Exists(legionFile))
            {
                var doc = JsonNode.Parse(File.ReadAllText(legionFile));
                return doc?["legionChance"]?.GetValue<int>() ?? 15;
            }
        }
        catch (Exception)
        {
            logger.Warning("SWAG: Unable to load Legion Boss progress file");
        }
        return 15;
    }

    private string DetermineZone(JsonNode boss, string globalMap)
    {
        var zoneNode = boss["BossZone"];
        if (zoneNode is JsonArray zoneArray && zoneArray.Count > 0)
        {
            var zones = zoneArray.Select(z => z?.GetValue<string>() ?? "").Where(z => !string.IsNullOrEmpty(z)).ToList();
            if (zones.Count > 1)
                return zones[Rng.Next(zones.Count)];
            if (zones.Count == 1)
                return zones[0];
        }
        else if (zoneNode is JsonValue zoneVal)
        {
            var zone = zoneVal.GetValue<string>();
            if (!string.IsNullOrEmpty(zone)) return zone;
        }

        if (_savedLocationData.TryGetValue(globalMap, out var backup) && backup.OpenZones.Count > 0)
            return backup.OpenZones[Rng.Next(backup.OpenZones.Count)];

        return "";
    }

    private List<BossSupport>? BuildSupports(JsonNode boss, string escortDifficulty)
    {
        var supportsNode = boss["Supports"];
        if (supportsNode is not JsonArray supportsArray || supportsArray.Count == 0)
            return null;

        var result = new List<BossSupport>();
        foreach (var support in supportsArray)
        {
            if (support == null) continue;

            var escortType = support["BossEscortType"]?.GetValue<string>() ?? "";
            escortType = ClassDef.RoleCase.TryGetValue(escortType.ToLowerInvariant(), out var mapped) ? mapped : escortType;

            result.Add(new BossSupport
            {
                BossEscortAmount = support["BossEscortAmount"]?.GetValue<string>() ?? "1",
                BossEscortDifficulty = new(new List<string> { escortDifficulty }, null!),
                BossEscortType = escortType
            });
        }

        return result.Count > 0 ? result : null;
    }

    private static string GetDifficulty(string key)
    {
        if (string.Equals(key, "asonline", StringComparison.OrdinalIgnoreCase))
        {
            string[] options = ["easy", "normal", "hard", "impossible"];
            return options[Rng.Next(options.Length)];
        }
        return ClassDef.DiffProper.TryGetValue(key, out var proper) ? proper : "normal";
    }

    internal void SetBotCaps(string timeOfDay)
    {
        var capConfig = string.Equals(timeOfDay, "night", StringComparison.OrdinalIgnoreCase)
            ? Config?["NightMaxBotCap"]
            : Config?["MaxBotCap"];

        if (capConfig is JsonObject capObj)
        {
            foreach (var (key, value) in capObj)
            {
                if (value != null)
                    botConfig.MaxBotCap[key] = value.GetValue<int>();
            }
        }

        logger.Info($"SWAG: {timeOfDay} Raid Max Bot Caps set");
    }

    private static void ShuffleAll(Dictionary<string, List<JsonNode>> configs)
    {
        foreach (var list in configs.Values)
            Shuffle(list);
    }

    private static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}

internal class LocationBackup
{
    public List<Wave> Waves { get; set; } = [];
    public List<BossLocationSpawn> BossLocationSpawn { get; set; } = [];
    public List<string> OpenZones { get; set; } = [];
}
