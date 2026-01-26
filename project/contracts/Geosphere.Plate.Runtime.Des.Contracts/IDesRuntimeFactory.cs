using FantaSim.Geosphere.Plate.Runtime.Des.Runtime;
using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;

namespace FantaSim.Geosphere.Plate.Runtime.Des;

/// <summary>
/// Factory for creating configured DES Runtimes for specific truth streams.
/// </summary>
public interface IDesRuntimeFactory
{
    /// <summary>
    /// Creates a new DES Runtime instance for the specified stream.
    /// </summary>
    /// <param name="streamIdentity">The identity of the truth stream to simulate.</param>
    /// <returns>A configured runtime instance.</returns>
    IDesRuntime Create(TruthStreamIdentity streamIdentity);
}
