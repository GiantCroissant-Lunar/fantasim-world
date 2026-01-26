using FantaSim.Geosphere.Plate.Topology.Contracts.Simulation;

namespace FantaSim.Geosphere.Plate.SolverLab.Core.Registry;

internal interface ISolverFactory
{
    object Create();
}

internal class SolverFactory<TSolver, TInput, TOutput> : ISolverFactory
    where TSolver : ISolver<TInput, TOutput>, new()
{
    public object Create() => new TSolver();
}

public sealed class SolverRegistry
{
    private readonly Dictionary<string, ISolverFactory> _factories = new();
    private readonly Dictionary<string, string> _activeVariants = new();

    /// <summary>
    /// Register a solver implementation.
    /// </summary>
    public void Register<TSolver, TInput, TOutput>(string domain, string variant)
        where TSolver : ISolver<TInput, TOutput>, new()
    {
        var key = $"{domain}:{variant}";
        _factories[key] = new SolverFactory<TSolver, TInput, TOutput>();
    }

    /// <summary>
    /// Set the active variant for a domain.
    /// </summary>
    public void SetActiveVariant(string domain, string variant)
    {
        _activeVariants[domain] = variant;
    }

    /// <summary>
    /// Get the active solver for a domain.
    /// </summary>
    public ISolver<TInput, TOutput> GetSolver<TInput, TOutput>(string domain)
    {
        var variant = _activeVariants.GetValueOrDefault(domain, "Reference");
        var key = $"{domain}:{variant}";

        if (!_factories.TryGetValue(key, out var factory))
        {
            throw new KeyNotFoundException($"No solver registered for domain '{domain}' with variant '{variant}'");
        }

        return (ISolver<TInput, TOutput>)factory.Create();
    }
}
