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
using UnifyStorage.Abstractions;
using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;
using Plate.TimeDete.Time.Primitives;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using TopologyEventId = FantaSim.Geosphere.Plate.Topology.Contracts.Events.EventId;

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

static bool TrySeedPluginDirectoryFromBuildOutput(
    string buildOutputDir,
    string pluginDir,
    ISet<string>? forceCopyFileNames = null)
{
    if (!Directory.Exists(buildOutputDir))
    {
        return false;
    }

    var hostFiles = Directory.GetFiles(AppContext.BaseDirectory)
        .Select(Path.GetFileName)
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

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
        var fileName = Path.GetFileName(file);
        if (hostFiles.Contains(fileName) && (forceCopyFileNames == null || !forceCopyFileNames.Contains(fileName)))
        {
            continue;
        }

        var dst = Path.Combine(pluginDir, fileName);
        File.Copy(file, dst, overwrite: true);
    }

    return true;
}

static bool TrySeedPluginDirectoryFromBuildOutputs(string projectDir, string pluginDir)
{
    var pluginNames = new[]
    {
        "Geosphere.Plate.Runtime.Des",
        "Space.Stellar.Solvers.Reference",
        "Geosphere.Plate.Reconstruction.Solver",
        "Geosphere.Plate.Kinematics.Serializers",
        "Geosphere.Plate.Kinematics.Materializer",
        "Geosphere.Plate.Topology.Serializers",
        "Geosphere.Plate.Topology.Materializer"
    };

    var anySeeded = false;

    foreach (var name in pluginNames)
    {
        var candidateDirs = new[]
        {
            Path.Combine(projectDir, "plugins", name, "bin", "Release", "net8.0"),
            Path.Combine(projectDir, "plugins", name, "bin", "Debug", "net8.0")
        };

        foreach (var dir in candidateDirs)
        {
            var forceCopy = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                name + ".dll",
                name + ".deps.json"
            };

            if (TrySeedPluginDirectoryFromBuildOutput(dir, pluginDir, forceCopy))
            {
                anySeeded = true;
                break;
            }
        }
    }

    return anySeeded;
}

static void PreloadSharedAssemblies(string pluginDir, ILogger logger)
{
    if (!Directory.Exists(pluginDir))
    {
        return;
    }

    static bool IsSharedByDefaultContext(string fileName)
    {
        return fileName.EndsWith(".Abstractions.dll", StringComparison.OrdinalIgnoreCase)
            || fileName.EndsWith(".Contracts.dll", StringComparison.OrdinalIgnoreCase)
            || fileName.EndsWith(".Primitives.dll", StringComparison.OrdinalIgnoreCase)
            || fileName.EndsWith(".Shared.dll", StringComparison.OrdinalIgnoreCase)
            || fileName.EndsWith(".Artifacts.dll", StringComparison.OrdinalIgnoreCase);
    }

    foreach (var path in Directory.GetFiles(pluginDir, "*.dll", SearchOption.TopDirectoryOnly))
    {
        var fileName = Path.GetFileName(path);
        if (!IsSharedByDefaultContext(fileName))
        {
            continue;
        }

        var simpleName = Path.GetFileNameWithoutExtension(path);
        var alreadyLoaded = AssemblyLoadContext.Default.Assemblies.Any(a =>
            string.Equals(a.GetName().Name, simpleName, StringComparison.OrdinalIgnoreCase));
        if (alreadyLoaded)
        {
            continue;
        }

        try
        {
            _ = Assembly.LoadFrom(path);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to preload shared assembly {AssemblyName} from {Path}", simpleName, path);
        }
    }
}

var registry = new ServiceArchi.Core.ServiceRegistry();

// We'll recreate the ServiceCollection for each iteration while keeping the
// ServiceRegistry (registry) and loggerFactory singleton outside the loop.

var projectDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
var seeded = TrySeedPluginDirectoryFromBuildOutputs(projectDir, pluginDir);
PreloadSharedAssemblies(pluginDir, logger);

Assembly.Load("Geosphere.Plate.Topology.Materializer");

for (var i = 0; i < 5; i++)
{
    logger.LogInformation("=== Iteration {Iteration}/5 ===", i + 1);

    var services = new ServiceCollection();
    services.AddSingleton(loggerFactory);
    services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));

    // Register the shared registry instance and other required services per-iteration
    services.AddSingleton<IRegistry>(registry);
    services.AddSingleton<IKeyValueStore, InMemoryOrderedKeyValueStore>();
    services.AddSingleton<ITopologyEventStore, NullTopologyEventStore>();
    services.AddSingleton<IPlateTopologySnapshotStore, NullPlateTopologySnapshotStore>();
    services.AddSingleton<PlateTopologyTimeline>();

    var collected = RunAndVerifyUnload(services, logger, registry, pluginDir, seeded);
    logger.LogInformation("Plugin load context collected after shutdown: {Collected}", collected);

    if (!collected)
    {
        Environment.ExitCode = 1;
        break;
    }
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

    var validateTopology = string.Equals(
        Environment.GetEnvironmentVariable("FANTASIM_SMOKE_VALIDATE_TOPOLOGY"),
        "1",
        StringComparison.OrdinalIgnoreCase);

    if (validateTopology)
    {
        ValidateTopologyPipeline(registry, logger);
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

static void ValidateTopologyPipeline(IRegistry registry, ILogger logger)
{
    var store = registry.TryGet<ITopologyEventStore>();
    if (store is null)
    {
        logger.LogWarning("Topology event store not registered; skipping validation.");
        return;
    }

    var stream = new TruthStreamIdentity(
        VariantId: "main",
        BranchId: "trunk",
        LLevel: 0,
        Domain: Domain.GeoPlatesTopology,
        Model: "M0");

    var existingHead = store.GetHeadAsync(stream, CancellationToken.None).GetAwaiter().GetResult();
    if (existingHead.Sequence > 0)
    {
        logger.LogInformation("Topology validation skipped; stream already contains data (Seq={Seq}).", existingHead.Sequence);
        return;
    }

    var evt = new PlateCreatedEvent(
        EventId: TopologyEventId.NewId().Value,
        PlateId: PlateId.NewId(),
        Tick: new CanonicalTick(0),
        Sequence: 1,
        StreamIdentity: stream,
        PreviousHash: ReadOnlyMemory<byte>.Empty,
        Hash: ReadOnlyMemory<byte>.Empty);

    store.AppendAsync(stream, new IPlateTopologyEvent[] { evt }, CancellationToken.None).GetAwaiter().GetResult();

    var head = store.GetHeadAsync(stream, CancellationToken.None).GetAwaiter().GetResult();
    logger.LogInformation("Topology validation append ok. Head: Seq={Seq}, Tick={Tick}", head.Sequence, head.LastTick);
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
