using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;

namespace FantaSim.Geosphere.Plate.Kinematics.Contracts.Events;

public enum TickMonotonicityPolicy
{
    Allow = 0,
    Warn = 1,
    Reject = 2
}

public sealed class AppendOptions
{
    public static readonly AppendOptions Default = new();

    public TickMonotonicityPolicy TickPolicy { get; init; } = TickMonotonicityPolicy.Allow;
}

public interface IKinematicsEventStore
{
    Task AppendAsync(
        TruthStreamIdentity stream,
        IEnumerable<IPlateKinematicsEvent> events,
        CancellationToken cancellationToken);

    Task AppendAsync(
        TruthStreamIdentity stream,
        IEnumerable<IPlateKinematicsEvent> events,
        AppendOptions options,
        CancellationToken cancellationToken);

    IAsyncEnumerable<IPlateKinematicsEvent> ReadAsync(
        TruthStreamIdentity stream,
        long fromSequenceInclusive,
        CancellationToken cancellationToken);

    Task<long?> GetLastSequenceAsync(
        TruthStreamIdentity stream,
        CancellationToken cancellationToken);
}
