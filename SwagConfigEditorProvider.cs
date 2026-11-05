using System.Reflection;
using System.Text.Json.Nodes;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers.Server;
using SPTarkov.Server.Web.Models.Configs;
using SPTarkov.Server.Web.Services;

namespace SWAG;

[Injectable(InjectionType.Singleton)]
public sealed class SwagConfigEditorProvider : IConfigEditorConfigProvider
{
    private readonly SwagMod _swagMod;
    private readonly string _configPath;

    public SwagConfigEditorProvider(SwagMod swagMod, ModHelper modHelper)
    {
        _swagMod = swagMod;
        var modPath = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
        _configPath = Path.Combine(modPath, "config");
    }

    public IEnumerable<ConfigEditorConfigRegistration> GetConfigs()
    {
        if (_swagMod.Config is JsonObject config)
        {
            yield return CreateRegistration(
                "com.arys.swag.config",
                "SWAG Configuration",
                config,
                Path.Combine(_configPath, "config.json")
            );
        }

        if (_swagMod.BossConfigNode is JsonObject bossConfig)
        {
            yield return CreateRegistration(
                "com.arys.swag.bosses",
                "SWAG Boss Configuration",
                bossConfig,
                Path.Combine(_configPath, "bossConfig.json")
            );
        }
    }

    private static ConfigEditorConfigRegistration CreateRegistration(
        string id,
        string displayName,
        JsonObject runtimeConfig,
        string filePath
    )
    {
        return new ConfigEditorConfigRegistration
        {
            Id = id,
            DisplayName = displayName,
            RuntimeConfig = runtimeConfig,
            RuntimeType = typeof(JsonObject),
            FilePath = filePath,
            ApplyToRuntimeAsync = (editedConfig, _) =>
            {
                ReplaceContents(runtimeConfig, (JsonObject)editedConfig);
                return ValueTask.CompletedTask;
            },
        };
    }

    private static void ReplaceContents(JsonObject destination, JsonObject source)
    {
        destination.Clear();
        foreach (var (key, value) in source)
        {
            destination[key] = value?.DeepClone();
        }
    }
}
