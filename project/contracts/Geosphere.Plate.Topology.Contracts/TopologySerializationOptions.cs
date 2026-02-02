using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;
using UnifyGeometry;

namespace FantaSim.Geosphere.Plate.Topology.Contracts;

/// <summary>
/// Frozen MessagePack serialization options for plate topology events.
///
/// This options instance is shared by both legacy and new serialization paths
/// to ensure byte-for-byte identical output. The options are frozen at startup
/// to prevent runtime modifications that could cause serialization divergence.
///
/// Includes:
/// - Custom formatters for geometry types (Point2, Segment2, Polyline2)
/// - Generated MessagePack formatters for event types (from [UnifyModel] attribute)
/// </summary>
public static class TopologySerializationOptions
{
    /// <summary>
    /// Gets the frozen MessagePack serialization options.
    ///
    /// This instance is initialized once at startup and never modified,
    /// ensuring deterministic serialization across the application lifetime.
    /// </summary>
    public static readonly MessagePackSerializerOptions Options = MessagePackSerializerOptions.Standard
        .WithResolver(CompositeResolver.Create(
            new IMessagePackFormatter[]
            {
                // Geometry formatters
                new Point2Formatter(),
                new Segment2Formatter(),
                new Polyline2Formatter(),

                // Generated Formatters (from [UnifyModel] attribute on event types)
                new FantaSim.Geosphere.Plate.Topology.Contracts.Identity.TruthStreamIdentityMessagePackFormatter(),
                new FantaSim.Geosphere.Plate.Topology.Contracts.Events.PlateCreatedEventMessagePackFormatter(),
                new FantaSim.Geosphere.Plate.Topology.Contracts.Events.PlateRetiredEventMessagePackFormatter(),
                new FantaSim.Geosphere.Plate.Topology.Contracts.Events.BoundaryCreatedEventMessagePackFormatter(),
                new FantaSim.Geosphere.Plate.Topology.Contracts.Events.BoundaryTypeChangedEventMessagePackFormatter(),
                new FantaSim.Geosphere.Plate.Topology.Contracts.Events.BoundaryGeometryUpdatedEventMessagePackFormatter(),
                new FantaSim.Geosphere.Plate.Topology.Contracts.Events.BoundaryRetiredEventMessagePackFormatter(),
                new FantaSim.Geosphere.Plate.Topology.Contracts.Events.JunctionCreatedEventMessagePackFormatter(),
                new FantaSim.Geosphere.Plate.Topology.Contracts.Events.JunctionUpdatedEventMessagePackFormatter(),
                new FantaSim.Geosphere.Plate.Topology.Contracts.Events.JunctionRetiredEventMessagePackFormatter()
            },
            new IFormatterResolver[]
            {
                NativeGuidResolver.Instance,
                BuiltinResolver.Instance,
                StandardResolver.Instance
            }
        ));
}
