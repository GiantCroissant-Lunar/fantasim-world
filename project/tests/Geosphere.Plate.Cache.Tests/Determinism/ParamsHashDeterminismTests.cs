using FantaSim.Geosphere.Plate.Cache.Materializer.Hashing;

namespace FantaSim.Geosphere.Plate.Cache.Tests.Determinism;

public class ParamsHashDeterminismTests
{
    [Fact]
    public void EmptyParams_HashMatchesExpected()
    {
        var hash = ParamsHashComputer.Compute(new Dictionary<string, object?>(StringComparer.Ordinal));

        Assert.Equal("76be8b528d0075f7aae98d6fa57a6d3c83ae480a8469e668d7b0af968995ac71", hash);
    }

    [Fact]
    public void SameParams_SameHash()
    {
        var parameters = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["compression"] = "lz4",
            ["snapshot_kind"] = "topology_state_view"
        };

        var hash1 = ParamsHashComputer.Compute(parameters);
        var hash2 = ParamsHashComputer.Compute(parameters);

        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void KeyOrderIndependent()
    {
        var parametersA = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["a"] = 1,
            ["b"] = 2
        };
        var parametersB = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["b"] = 2,
            ["a"] = 1
        };

        var hashA = ParamsHashComputer.Compute(parametersA);
        var hashB = ParamsHashComputer.Compute(parametersB);

        Assert.Equal(hashA, hashB);
    }

    [Fact]
    public void Repeatability_MultipleCalls()
    {
        var parameters = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["mode"] = "test",
            ["count"] = 42
        };

        var hashes = new[]
        {
            ParamsHashComputer.Compute(parameters),
            ParamsHashComputer.Compute(parameters),
            ParamsHashComputer.Compute(parameters)
        };

        Assert.All(hashes, hash => Assert.Equal(hashes[0], hash));
    }
}
