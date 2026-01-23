using System.Threading;
using System.Threading.Tasks;
using FantaSim.World.Contracts.Time;
using Plate.Runtime.Des.Runtime;

namespace Plate.Runtime.Des.Drivers;

public readonly record struct DriverId(string Value);

public record DriverOutput(
    object? Signal // Opaque for now
);

public interface IDriver
{
    DriverId Id { get; }
    SphereId Sphere { get; }

    // Called at a scheduled tick with a read-only view of state.
    Task<DriverOutput> EvaluateAsync(
        DesContext context,
        CancellationToken ct = default);
}
