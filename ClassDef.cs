namespace SWAG;

public static class ClassDef
{
    public static readonly Dictionary<string, string> RoleCase = new(StringComparer.OrdinalIgnoreCase)
    {
        ["assault"] = "assault",
        ["exusec"] = "exUsec",
        ["marksman"] = "marksman",
        ["pmcbot"] = "pmcBot",
        ["sectantpriest"] = "sectantPriest",
        ["sectantwarrior"] = "sectantWarrior",
        ["assaultgroup"] = "assaultGroup",
        ["bossbully"] = "bossBully",
        ["bosstagilla"] = "bossTagilla",
        ["bossgluhar"] = "bossGluhar",
        ["bosskilla"] = "bossKilla",
        ["bosskojaniy"] = "bossKojaniy",
        ["bosssanitar"] = "bossSanitar",
        ["bossboar"] = "bossBoar",
        ["bossboarsniper"] = "bossBoarSniper",
        ["bosskolontay"] = "bossKolontay",
        ["bosspunisher"] = "bosspunisher",
        ["bosslegion"] = "bosslegion",
        ["bosspartisan"] = "bossPartisan",
        ["bosstagillaagro"] = "bossTagillaAgro",
        ["bosskillaagro"] = "bossKillaAgro",
        ["tagillahelperagro"] = "tagillaHelperAgro",
        ["followerboar"] = "followerBoar",
        ["followerboarclose1"] = "followerBoarClose1",
        ["followerboarclose2"] = "followerBoarClose2",
        ["followerbully"] = "followerBully",
        ["followergluharassault"] = "followerGluharAssault",
        ["followergluharscout"] = "followerGluharScout",
        ["followergluharsecurity"] = "followerGluharSecurity",
        ["followergluharsnipe"] = "followerGluharSnipe",
        ["followerkojaniy"] = "followerKojaniy",
        ["followersanitar"] = "followerSanitar",
        ["followertagilla"] = "followerTagilla",
        ["followerkolontayassault"] = "followerKolontayAssault",
        ["followerkolontaysecurity"] = "followerKolontaySecurity",
        ["cursedassault"] = "cursedAssault",
        ["pmc"] = "pmc",
        ["usec"] = "usec",
        ["bear"] = "bear",
        ["sptbear"] = "sptBear",
        ["sptusec"] = "sptUsec",
        ["bosstest"] = "bossTest",
        ["followertest"] = "followerTest",
        ["gifter"] = "gifter",
        ["bossknight"] = "bossKnight",
        ["followerbigpipe"] = "followerBigPipe",
        ["followerbirdeye"] = "followerBirdEye",
        ["bosszryachiy"] = "bossZryachiy",
        ["followerzryachiy"] = "followerZryachiy",
        ["arenafighterevent"] = "arenaFighterEvent",
        ["crazyassaultevent"] = "crazyAssaultEvent"
    };

    public static readonly Dictionary<string, string> ReverseBossNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["bossboar"] = "kaban",
        ["bossbully"] = "reshala",
        ["bosstagilla"] = "tagilla",
        ["bossgluhar"] = "gluhar",
        ["bosskilla"] = "killa",
        ["bosskojaniy"] = "shturman",
        ["bosssanitar"] = "sanitar",
        ["bossknight"] = "goons",
        ["bosszryachiy"] = "zryachiy",
        ["bosskolontay"] = "kolontay",
        ["bosspartisan"] = "partisan",
        ["marksman"] = "scav_snipers",
        ["sectantpriest"] = "cultists",
        ["exusec"] = "rogues",
        ["pmcbot"] = "raiders",
        ["crazyassaultevent"] = "crazyscavs",
        ["arenafighterevent"] = "bloodhounds",
        ["bosspunisher"] = "punisher",
        ["bosslegion"] = "legion",
        ["gifter"] = "santa"
    };

    public static readonly Dictionary<string, string> ReverseMapNames = new()
    {
        ["factory4_day"] = "factory",
        ["factory4_night"] = "factory_night",
        ["bigmap"] = "customs",
        ["woods"] = "woods",
        ["shoreline"] = "shoreline",
        ["lighthouse"] = "lighthouse",
        ["rezervbase"] = "reserve",
        ["interchange"] = "interchange",
        ["laboratory"] = "laboratory",
        ["tarkovstreets"] = "streets",
        ["sandbox"] = "groundzero",
        ["sandbox_high"] = "groundzero_high",
        ["labyrinth"] = "labyrinth"
    };

    public static readonly Dictionary<string, string> DiffProper = new(StringComparer.OrdinalIgnoreCase)
    {
        ["easy"] = "easy",
        ["asonline"] = "random",
        ["normal"] = "normal",
        ["hard"] = "hard",
        ["impossible"] = "impossible"
    };

    public static readonly string[] ValidMaps =
    [
        "bigmap",
        "factory4_day",
        "factory4_night",
        "interchange",
        "laboratory",
        "lighthouse",
        "rezervbase",
        "shoreline",
        "tarkovstreets",
        "woods",
        "sandbox",
        "sandbox_high",
        "labyrinth"
    ];

    public static readonly List<string> PmcAlwaysEnemies =
    [
        "arenaFighter",
        "arenaFighterEvent",
        "assault",
        "assaultGroup",
        "bossBoar",
        "bossBoarSniper",
        "bossBully",
        "bossGluhar",
        "bossKilla",
        "bossKnight",
        "bossKojaniy",
        "bossKolontay",
        "bossPartisan",
        "bossSanitar",
        "bossTagilla",
        "crazyAssaultEvent",
        "cursedAssault",
        "exUsec",
        "followerBigPipe",
        "followerBirdEye",
        "followerBoar",
        "followerBoarClose1",
        "followerBoarClose2",
        "followerGluharAssault",
        "followerGluharScout",
        "followerGluharSecurity",
        "followerGluharSnipe",
        "followerKojaniy",
        "followerKolontayAssault",
        "followerKolontaySecurity",
        "followerSanitar",
        "followerTagilla",
        "marksman",
        "peacemaker",
        "pmcBEAR",
        "pmcBot",
        "pmcUSEC",
        "sectactPriestEvent",
        "sectantPriest",
        "sectantWarrior",
        "skier",
        "spiritSpring",
        "spiritWinter"
    ];

    public static readonly List<string> PmcAlwaysFriends =
    [
        "bossZryachiy",
        "followerZryachiy",
        "gifter",
        "peacefullZryachiyEvent",
        "ravangeZryachiyEvent"
    ];
}
