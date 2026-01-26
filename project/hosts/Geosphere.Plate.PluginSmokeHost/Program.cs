using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using FantaSim.Geosphere.Plate.Runtime.Des;
using FantaSim.Geosphere.Plate.Topology.Contracts.Derived;
using FantaSim.Geosphere.Plate.Topology.Contracts.Events;
using FantaSim.Geosphere.Plate.Topology.Materializer;
using PluginArchi.Extensibility.Hosting;
using FantaSim.Geosphere.Plate.PluginSmokeHost;
using ServiceArchi.Contracts;
using ServiceArchi.Core;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Threading;

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

static bool TrySeedPluginDirectoryFromBuildOutputs(string projectDir, string pluginDir)
{
    var candidateDirs = new[]
    {
        Path.Combine(projectDir, "plugins", "Geosphere.Plate.Runtime.Des", "bin", "Release", "net8.0"),
        Path.Combine(projectDir, "plugins", "Geosphere.Plate.Runtime.Des", "bin", "Debug", "net8.0")
    };

    foreach (var dir in candidateDirs)
    {
        if (TrySeedPluginDirectoryFromBuildOutput(dir, pluginDir))
        {
            return true;
        }
    }

    return false;
}

var services = new ServiceCollection();
services.AddSingleton(loggerFactory);
services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));

var registry = new ServiceArchi.Core.ServiceRegistry();
services.AddSingleton<IRegistry>(registry);
services.AddSingleton<ITopologyEventStore, NullTopologyEventStore>();
services.AddSingleton<IPlateTopologySnapshotStore, NullPlateTopologySnapshotStore>();
services.AddSingleton<PlateTopologyTimeline>();

var projectDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
var seeded = TrySeedPluginDirectoryFromBuildOutputs(projectDir, pluginDir);

Assembly.Load("Geosphere.Plate.Topology.Materializer");

var collected = RunAndVerifyUnload(services, logger, registry, pluginDir, seeded);
logger.LogInformation("Plugin load context collected after shutdown: {Collected}", collected);

if (!collected)
{
    Environment.ExitCode = 1;
}

[MethodImpl(MethodImplOptions.NoInlining)]
static bool RunAndVerifyUnload(IServiceCollection services, ILogger logger, IRegistry registry, string pluginDir, bool seeded)
{
    var weakRef = RunPluginHostOnce(services, logger, registry, pluginDir, seeded);
    var collected = WaitForUnload(weakRef);

    if (!collected)
    {
        DumpContexts(logger, weakRef);
    }

    return collected;
}

static void DumpContexts(ILogger logger, WeakReference weakRef)
{
    if (weakRef.Target is AssemblyLoadContext alc)
    {
        logger.LogInformation("Unloaded context still alive: Name={Name}, IsCollectible={IsCollectible}, Assemblies={AssemblyCount}", alc.Name, alc.IsCollectible, alc.Assemblies.Count());
        foreach (var a in alc.Assemblies.OrderBy(a => a.GetName().Name, StringComparer.OrdinalIgnoreCase))
        {
            logger.LogInformation("- ALC Assembly: {Assembly}", a.GetName().Name);
        }
    }

    var all = AssemblyLoadContext.All.ToArray();
    logger.LogInformation("AssemblyLoadContext.All: {Count}", all.Length);
    foreach (var c in all.OrderBy(c => c.Name ?? string.Empty, StringComparer.OrdinalIgnoreCase))
    {
        logger.LogInformation("- ALC: Name={Name}, IsCollectible={IsCollectible}, Assemblies={AssemblyCount}", c.Name, c.IsCollectible, c.Assemblies.Count());
    }
}

[MethodImpl(MethodImplOptions.NoInlining)]
static WeakReference RunPluginHostOnce(IServiceCollection services, ILogger logger, IRegistry registry, string pluginDir, bool seeded)
{
    var loader = new IsolatedLoader(pluginDir, "*.dll", SearchOption.TopDirectoryOnly, contextName: "FantaSimWorldPlugins");
    var builder = new PluginHostBuilder(services)
        .UseLoader(loader)
        .UseActivator(new NonCachingPluginActivator());

    var host = builder.Build();
    try
    {

    logger.LogInformation("Plugin directory: {PluginDir}", pluginDir);
    logger.LogInformation("Seeded plugin directory from build output: {Seeded}", seeded);
    logger.LogInformation("SupportsReload={SupportsReload}", host.SupportsReload);

    host.InitializeAsync().GetAwaiter().GetResult();
    logger.LogInformation("DES factory registered (pre-shutdown): {Registered}", registry.IsRegistered<IDesRuntimeFactory>());

    logger.LogInformation("Discovered plugins: {Count}", host.Registry.Plugins.Count);
    foreach (var plugin in host.Registry.Plugins)
    {
        logger.LogInformation("- {PluginId}", plugin.Descriptor.Id);
    }

    var enableReload = string.Equals(
        Environment.GetEnvironmentVariable("FANTASIM_SMOKEHOTRELOAD"),
        "1",
        StringComparison.OrdinalIgnoreCase);

    if (enableReload)
    {
        var reloadOk = host.ReloadPluginsAsync().GetAwaiter().GetResult();
        logger.LogInformation("ReloadPluginsAsync result: {ReloadOk}", reloadOk);
    }

    var context = loader.Context;
    var weakRef = new WeakReference(context ?? throw new InvalidOperationException("Loader context is null after host initialization."));
    context = null;

    host.ShutdownAsync().GetAwaiter().GetResult();
    logger.LogInformation("DES factory registered (post-shutdown): {Registered}", registry.IsRegistered<IDesRuntimeFactory>());
    return weakRef;
    }
    finally
    {
        host.DisposeAsync().GetAwaiter().GetResult();
    }
}

static bool WaitForUnload(WeakReference weakRef)
{
    for (var i = 0; i < 200; i++)
    {
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
        GC.WaitForPendingFinalizers();
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);

        if (IsCollected(weakRef))
        {
            return true;
        }

        Thread.Sleep(50);
    }

    return IsCollected(weakRef);
}

[MethodImpl(MethodImplOptions.NoInlining)]
static bool IsCollected(WeakReference weakRef)
{
    return !weakRef.IsAlive;
}
