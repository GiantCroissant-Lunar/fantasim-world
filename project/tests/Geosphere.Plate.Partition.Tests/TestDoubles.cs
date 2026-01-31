using System.Collections.Immutable;
using FantaSim.Geosphere.Plate.Polygonization.Contracts.Products;
using FantaSim.Geosphere.Plate.Polygonization.Contracts.Solvers;
using FantaSim.Geosphere.Plate.Topology.Contracts.Derived;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;
using Plate.TimeDete.Time.Primitives;
using UnifyGeometry;

using PlateEntity = FantaSim.Geosphere.Plate.Topology.Contracts.Entities.Plate;
using BoundaryEntity = FantaSim.Geosphere.Plate.Topology.Contracts.Entities.Boundary;
using JunctionEntity = FantaSim.Geosphere.Plate.Topology.Contracts.Entities.Junction;

namespace FantaSim.Geosphere.Plate.Partition.Tests;

/// <summary>
/// In-memory implementation of topology state view for testing.
/// Allows construction of topology scenarios without event sourcing.
/// </summary>
public sealed class InMemoryTopologyStateView : IPlateTopologyStateView
{
    public TruthStreamIdentity Identity { get; }

    public Dictionary<PlateId, PlateEntity> Plates { get; } = new();
    public Dictionary<BoundaryId, BoundaryEntity> Boundaries { get; } = new();
    public Dictionary<JunctionId, JunctionEntity> Junctions { get; } = new();

    public long LastEventSequence { get; set; }

    public InMemoryTopologyStateView(string? streamName = null)
    {
        var domain = Domain.Parse("test.partition");
        var variant = streamName ?? $"test-{Guid.NewGuid():N}";
        Identity = new TruthStreamIdentity(variant, "main", 0, domain, "M0");
        LastEventSequence = 1;
    }

    public InMemoryTopologyStateView(TruthStreamIdentity identity)
    {
        Identity = identity;
        LastEventSequence = 1;
    }

    IReadOnlyDictionary<PlateId, PlateEntity> IPlateTopologyStateView.Plates => Plates;
    IReadOnlyDictionary<BoundaryId, BoundaryEntity> IPlateTopologyStateView.Boundaries => Boundaries;
    IReadOnlyDictionary<JunctionId, JunctionEntity> IPlateTopologyStateView.Junctions => Junctions;

    long IPlateTopologyStateView.LastEventSequence => LastEventSequence;
}

/// <summary>
/// Fake polygonizer for testing partition logic without full polygonization.
/// Allows controlled testing of partition service behavior.
/// </summary>
public sealed class FakePolygonizer : IPlatePolygonizer
{
    private readonly Func<CanonicalTick, IPlateTopologyStateView, PolygonizationOptions?, PlatePolygonSet>? _polygonizeFunc;
    private readonly Func<CanonicalTick, IPlateTopologyStateView, PolygonizationOptions?, PolygonizationDiagnostics>? _validateFunc;
    private readonly List<(CanonicalTick Tick, IPlateTopologyStateView Topology)> _calls = new();

    public FakePolygonizer(
        Func<CanonicalTick, IPlateTopologyStateView, PolygonizationOptions?, PlatePolygonSet>? polygonizeFunc = null,
        Func<CanonicalTick, IPlateTopologyStateView, PolygonizationOptions?, PolygonizationDiagnostics>? validateFunc = null)
    {
        _polygonizeFunc = polygonizeFunc;
        _validateFunc = validateFunc;
    }

    public IReadOnlyList<(CanonicalTick Tick, IPlateTopologyStateView Topology)> Calls => _calls;

    public PlatePolygonSet PolygonizeAtTick(
        CanonicalTick tick,
        IPlateTopologyStateView topology,
        PolygonizationOptions? options = null)
    {
        _calls.Add((tick, topology));

        if (_polygonizeFunc != null)
        {
            return _polygonizeFunc(tick, topology, options);
        }

        // Default: return empty polygon set
        return new PlatePolygonSet(tick, ImmutableArray<PlatePolygon>.Empty);
    }

    public BoundaryFaceAdjacencyMap GetBoundaryFaceAdjacency(
        CanonicalTick tick,
        IPlateTopologyStateView topology,
        PolygonizationOptions? options = null)
    {
        return new BoundaryFaceAdjacencyMap(tick, ImmutableArray<BoundaryFaceAdjacency>.Empty);
    }

    public PolygonizationDiagnostics Validate(
        CanonicalTick tick,
        IPlateTopologyStateView topology,
        PolygonizationOptions? options = null)
    {
        if (_validateFunc != null)
        {
            return _validateFunc(tick, topology, options);
        }

        return PolygonizationDiagnostics.Valid();
    }

    public void ResetCalls() => _calls.Clear();
}

/// <summary>
/// Fake polygonizer that simulates deterministic behavior.
/// Returns predictable results based on topology content.
/// </summary>
public sealed class DeterministicFakePolygonizer : IPlatePolygonizer
{
    private readonly Dictionary<string, PlatePolygonSet> _results = new();

    public void RegisterResult(string topologyKey, PlatePolygonSet result)
    {
        _results[topologyKey] = result;
    }

    public PlatePolygonSet PolygonizeAtTick(
        CanonicalTick tick,
        IPlateTopologyStateView topology,
        PolygonizationOptions? options = null)
    {
        var key = ComputeTopologyKey(topology);

        if (_results.TryGetValue(key, out var result))
        {
            // Return a copy with the requested tick
            return new PlatePolygonSet(tick, result.Polygons);
        }

        // Generate deterministic result from topology
        return GenerateDeterministicResult(tick, topology);
    }

    public BoundaryFaceAdjacencyMap GetBoundaryFaceAdjacency(
        CanonicalTick tick,
        IPlateTopologyStateView topology,
        PolygonizationOptions? options = null)
    {
        return new BoundaryFaceAdjacencyMap(tick, ImmutableArray<BoundaryFaceAdjacency>.Empty);
    }

    public PolygonizationDiagnostics Validate(
        CanonicalTick tick,
        IPlateTopologyStateView topology,
        PolygonizationOptions? options = null)
    {
        // Check for basic validity
        var openBoundaries = new List<OpenBoundaryDiagnostic>();
        var nonManifold = new List<NonManifoldJunctionDiagnostic>();

        foreach (var boundary in topology.Boundaries.Values.Where(b => !b.IsRetired))
        {
            var junctions = topology.Junctions.Values
                .Where(j => !j.IsRetired && j.BoundaryIds.Contains(boundary.BoundaryId))
                .ToList();

            if (junctions.Count < 2)
            {
                openBoundaries.Add(new OpenBoundaryDiagnostic(
                    boundary.BoundaryId,
                    new Point3(0, 0, 0),
                    $"Boundary {boundary.BoundaryId} is connected to {junctions.Count} junctions"));
            }
        }

        var isValid = openBoundaries.Count == 0 && nonManifold.Count == 0;
        return new PolygonizationDiagnostics(
            isValid,
            openBoundaries.ToImmutableArray(),
            nonManifold.ToImmutableArray(),
            ImmutableArray<DisconnectedComponentDiagnostic>.Empty);
    }

    private static string ComputeTopologyKey(IPlateTopologyStateView topology)
    {
        // Simple hash based on active boundary count
        var activeCount = topology.Boundaries.Count(b => !b.Value.IsRetired);
        return $"b{activeCount}_j{topology.Junctions.Count}";
    }

    private static PlatePolygonSet GenerateDeterministicResult(CanonicalTick tick, IPlateTopologyStateView topology)
    {
        var polygons = new List<PlatePolygon>();
        var activePlates = topology.Plates.Where(p => !p.Value.IsRetired).ToList();

        foreach (var plate in activePlates)
        {
            // Create a simple square polygon for each plate
            var ring = new Polyline3([
                new Point3(0, 0, 0),
                new Point3(1, 0, 0),
                new Point3(1, 1, 0),
                new Point3(0, 1, 0),
                new Point3(0, 0, 0)
            ]);

            polygons.Add(new PlatePolygon(plate.Key, ring, ImmutableArray<Polyline3>.Empty));
        }

        return new PlatePolygonSet(tick, polygons.ToImmutableArray());
    }
}

/// <summary>
/// Fake polygonizer that simulates failures for testing error handling.
/// </summary>
public sealed class FailingPolygonizer : IPlatePolygonizer
{
    private readonly Exception _exception;
    private readonly bool _failOnValidate;

    public FailingPolygonizer(Exception exception, bool failOnValidate = false)
    {
        _exception = exception;
        _failOnValidate = failOnValidate;
    }

    public PlatePolygonSet PolygonizeAtTick(
        CanonicalTick tick,
        IPlateTopologyStateView topology,
        PolygonizationOptions? options = null)
    {
        throw _exception;
    }

    public BoundaryFaceAdjacencyMap GetBoundaryFaceAdjacency(
        CanonicalTick tick,
        IPlateTopologyStateView topology,
        PolygonizationOptions? options = null)
    {
        throw _exception;
    }

    public PolygonizationDiagnostics Validate(
        CanonicalTick tick,
        IPlateTopologyStateView topology,
        PolygonizationOptions? options = null)
    {
        if (_failOnValidate)
        {
            throw _exception;
        }

        return PolygonizationDiagnostics.Valid();
    }
}

/// <summary>
/// Fake polygonizer that returns invalid topology diagnostics.
/// </summary>
public sealed class InvalidTopologyPolygonizer : IPlatePolygonizer
{
    private readonly ImmutableArray<OpenBoundaryDiagnostic> _openBoundaries;
    private readonly ImmutableArray<NonManifoldJunctionDiagnostic> _nonManifoldJunctions;

    public InvalidTopologyPolygonizer(
        ImmutableArray<OpenBoundaryDiagnostic>? openBoundaries = null,
        ImmutableArray<NonManifoldJunctionDiagnostic>? nonManifoldJunctions = null)
    {
        _openBoundaries = openBoundaries ?? ImmutableArray<OpenBoundaryDiagnostic>.Empty;
        _nonManifoldJunctions = nonManifoldJunctions ?? ImmutableArray<NonManifoldJunctionDiagnostic>.Empty;
    }

    public PlatePolygonSet PolygonizeAtTick(
        CanonicalTick tick,
        IPlateTopologyStateView topology,
        PolygonizationOptions? options = null)
    {
        var diagnostics = new PolygonizationDiagnostics(
            false,
            _openBoundaries,
            _nonManifoldJunctions,
            ImmutableArray<DisconnectedComponentDiagnostic>.Empty);
        throw new PolygonizationException(
            "Cannot polygonize invalid topology",
            diagnostics);
    }

    public BoundaryFaceAdjacencyMap GetBoundaryFaceAdjacency(
        CanonicalTick tick,
        IPlateTopologyStateView topology,
        PolygonizationOptions? options = null)
    {
        return new BoundaryFaceAdjacencyMap(tick, ImmutableArray<BoundaryFaceAdjacency>.Empty);
    }

    public PolygonizationDiagnostics Validate(
        CanonicalTick tick,
        IPlateTopologyStateView topology,
        PolygonizationOptions? options = null)
    {
        return new PolygonizationDiagnostics(
            false,
            _openBoundaries,
            _nonManifoldJunctions,
            ImmutableArray<DisconnectedComponentDiagnostic>.Empty);
    }
}
