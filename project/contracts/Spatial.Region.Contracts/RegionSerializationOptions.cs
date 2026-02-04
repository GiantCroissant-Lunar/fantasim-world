using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;
using UnifyGeometry;
using FantaSim.Spatial.Region.Contracts.Formatters;

namespace FantaSim.Spatial.Region.Contracts;

/// <summary>
/// Frozen MessagePack serialization options for spatial region contracts.
/// Uses custom formatters for geometry types (Point3, Point2, Vec3, Quaternion)
/// to avoid duplicating types with MessagePack attributes.
/// </summary>
public static class RegionSerializationOptions
{
    /// <summary>
    /// Gets the frozen MessagePack serialization options with custom formatters.
    /// </summary>
    public static readonly MessagePackSerializerOptions Options = MessagePackSerializerOptions.Standard
        .WithResolver(CompositeResolver.Create(
            new IMessagePackFormatter[]
            {
                // UnifyGeometry types
                new Point3Formatter(),
                new Point2Formatter(),

                // Region-specific types
                new Vec3Formatter(),
                new RegionQuaternionFormatter()
            },
            new IFormatterResolver[]
            {
                NativeGuidResolver.Instance,
                BuiltinResolver.Instance,
                StandardResolver.Instance
            }
        ));
}
