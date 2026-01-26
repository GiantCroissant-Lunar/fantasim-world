using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PluginArchi.Extensibility.Hosting;
using ServiceArchi.Contracts;
using ServiceArchi.Core;

using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.SetMinimumLevel(LogLevel.Information);
    builder.AddSimpleConsole(options =>
    {
        options.SingleLine = true;
        options.TimestampFormat = "HH:mm:ss ";
    });
});

var logger = loggerFactory.CreateLogger("FantaSim.World.PluginSmokeHost");

var pluginDir = args.Length > 0 && !string.IsNullOrWhiteSpace(args[0])
    ? args[0]
    : Path.Combine(AppContext.BaseDirectory, "plugins");

Directory.CreateDirectory(pluginDir);

static bool TrySeedPluginDirectoryFromBuildOutput(string buildOutputDir, string pluginDir)
{
    if (!Directory.Exists(buildOutputDir))
    {
        return false;
    }

    var files = Directory.GetFiles(buildOutputDir)
        .Where(f =>
            f.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
            || f.EndsWith(".deps.json", StringComparison.OrdinalIgnoreCase))
        .ToArray();

    if (files.Length == 0)
    {
        return false;
    }

    foreach (var file in files)
    {
        var dst = Path.Combine(pluginDir, Path.GetFileName(file));
        File.Copy(file, dst, overwrite: true);
    }

    return true;
}

var services = new ServiceCollection();
services.AddSingleton(loggerFactory);
services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));

var registry = new ServiceArchi.Core.ServiceRegistry();
services.AddSingleton<IRegistry>(registry);

var projectDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
var desPluginBuildOutputDir = Path.Combine(
    projectDir,
    "plugins",
    "Geosphere.Plate.Runtime.Des",
    "bin",
    "Debug",
    "net8.0");

var seeded = TrySeedPluginDirectoryFromBuildOutput(desPluginBuildOutputDir, pluginDir);

var loader = new IsolatedLoader(pluginDir, "*.dll", SearchOption.TopDirectoryOnly, contextName: "FantaSimWorldPlugins", diagnosticsEnabled: true);
var builder = new PluginHostBuilder(services)
    .UseLoader(loader);

await using var host = builder.Build();

logger.LogInformation("Plugin directory: {PluginDir}", pluginDir);
logger.LogInformation("Seeded plugin directory from build output: {Seeded}", seeded);
logger.LogInformation("SupportsReload={SupportsReload}", host.SupportsReload);

await host.InitializeAsync();

logger.LogInformation("Discovered plugins: {Count}", host.Registry.Plugins.Count);
foreach (var plugin in host.Registry.Plugins)
{
    logger.LogInformation("- {PluginId} ({PluginType})", plugin.Descriptor.Id, plugin.GetType().FullName);
}

var reloadOk = await host.ReloadPluginsAsync();
logger.LogInformation("ReloadPluginsAsync result: {ReloadOk}", reloadOk);

await host.ShutdownAsync();

var collected = loader.IsContextCollected(forceGC: true);
logger.LogInformation("Plugin load context collected after shutdown: {Collected}", collected);
