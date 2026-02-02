using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;
using MessagePack;
using Plate.TimeDete.Time.Primitives;

namespace FantaSim.Geosphere.Plate.Cache.Contracts.Models;

[MessagePackObject]
public readonly record struct KinematicsSliceIdentity(
    [property: Key(0)] TruthStreamIdentity Stream,
    [property: Key(1)] CanonicalTick Tick,
    [property: Key(2)] long SliceLastSequence,
    [property: Key(3)] int SchemaVersion);
