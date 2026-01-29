using FantaSim.Geosphere.Plate.Cache.Materializer.Storage;
using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;

namespace FantaSim.Geosphere.Plate.Cache.Tests.Storage;

public class KeyBuilderTests
{
    [Fact]
    public void BuildManifestKey_UsesRfcFormat()
    {
        var stream = new TruthStreamIdentity("V1", "main", 0, Domain.Parse("geo.plates"), "M0");
        var key = KeyBuilder.BuildManifestKey(stream, "PlateTopologySnapshot", "abc123");
        Assert.Equal("S:V1:main:L0:geo.plates:M0:Derived:PlateTopologySnapshot:abc123:Manifest", key);
    }

    [Fact]
    public void BuildPayloadKey_UsesRfcFormat()
    {
        var stream = new TruthStreamIdentity("V1", "main", 2, Domain.Parse("geo.plates.topology"), "M1");
        var key = KeyBuilder.BuildPayloadKey(stream, "Atlas", "ff" + new string('0', 62));
        Assert.Equal("S:V1:main:L2:geo.plates.topology:M1:Derived:Atlas:ff" + new string('0', 62) + ":Payload", key);
    }

    [Fact]
    public void BuildPrefixForEnumeration_UsesRfcFormat()
    {
        var stream = new TruthStreamIdentity("V2", "branch-a", 1, Domain.Parse("geo.plates"), "M3");
        var prefix = KeyBuilder.BuildPrefixForEnumeration(stream, "Foo");
        Assert.Equal("S:V2:branch-a:L1:geo.plates:M3:Derived:Foo:", prefix);
    }
}
