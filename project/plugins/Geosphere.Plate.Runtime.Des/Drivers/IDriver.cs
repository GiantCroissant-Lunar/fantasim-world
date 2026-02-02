using System.Threading;
using System.Threading.Tasks;
using FantaSim.World.Contracts.Time;
using FantaSim.Geosphere.Plate.Runtime.Des.Runtime;

namespace FantaSim.Geosphere.Plate.Runtime.Des.Drivers;

public interface IDriver
{
    DriverId Id { get; }
    SphereId Sphere { get; }

    // Called at a scheduled tick with a read-only view of state.
    Task<DriverOutput> EvaluateAsync(
        DesContext context,
        CancellationToken ct = default);
}
