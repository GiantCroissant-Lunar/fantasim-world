// <copyright file="TestEventFactory.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System;
using System.Collections.Immutable;
using Plate.TimeDete.Time.Primitives;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using FantaSim.Geosphere.Plate.Topology.Contracts.Events;
using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;
using FantaSim.Geosphere.Plate.Topology.Contracts.Numerics;
using UnifyGeometry;

namespace FantaSim.Geosphere.Plate.Topology.Tests;

/// <summary>
/// Factory methods for creating test events with default (empty) hash fields.
/// In production code, use EventChainBuilder.WithComputedHash() to compute proper hashes.
/// These factory methods exist solely to ease test migrations after adding hash fields to the envelope.
/// </summary>
public static class TestEventFactory
{
    /// <summary>Empty hash bytes for test events.</summary>
    private static readonly ReadOnlyMemory<byte> EmptyHash = ReadOnlyMemory<byte>.Empty;

    /// <summary>Creates a PlateCreatedEvent with empty hash fields.</summary>
    public static PlateCreatedEvent PlateCreated(
        Guid eventId,
        PlateId plateId,
        CanonicalTick tick,
        long sequence,
        TruthStreamIdentity streamIdentity)
        => new(eventId, plateId, tick, sequence, streamIdentity, EmptyHash, EmptyHash);

    /// <summary>Creates a PlateCreatedEvent with custom hash fields (for testing hash computation).</summary>
    public static PlateCreatedEvent PlateCreated(
        Guid eventId,
        PlateId plateId,
        CanonicalTick tick,
        long sequence,
        TruthStreamIdentity streamIdentity,
        ReadOnlyMemory<byte> previousHash,
        ReadOnlyMemory<byte> hash)
        => new(eventId, plateId, tick, sequence, streamIdentity, previousHash, hash);

    /// <summary>Creates a PlateRetiredEvent with empty hash fields.</summary>
    public static PlateRetiredEvent PlateRetired(
        Guid eventId,
        PlateId plateId,
        string? reason,
        CanonicalTick tick,
        long sequence,
        TruthStreamIdentity streamIdentity)
        => new(eventId, plateId, reason, tick, sequence, streamIdentity, EmptyHash, EmptyHash);

    /// <summary>Creates a BoundaryCreatedEvent with empty hash fields.</summary>
    public static BoundaryCreatedEvent BoundaryCreated(
        Guid eventId,
        BoundaryId boundaryId,
        PlateId plateA,
        PlateId plateB,
        BoundaryType boundaryType,
        IGeometry geometry,
        CanonicalTick tick,
        long sequence,
        TruthStreamIdentity streamIdentity)
        => new(eventId, boundaryId, plateA, plateB, boundaryType, geometry, tick, sequence, streamIdentity, EmptyHash, EmptyHash);

    /// <summary>Creates a BoundaryTypeChangedEvent with empty hash fields.</summary>
    public static BoundaryTypeChangedEvent BoundaryTypeChanged(
        Guid eventId,
        BoundaryId boundaryId,
        BoundaryType oldType,
        BoundaryType newType,
        CanonicalTick tick,
        long sequence,
        TruthStreamIdentity streamIdentity)
        => new(eventId, boundaryId, oldType, newType, tick, sequence, streamIdentity, EmptyHash, EmptyHash);

    /// <summary>Creates a BoundaryGeometryUpdatedEvent with empty hash fields.</summary>
    public static BoundaryGeometryUpdatedEvent BoundaryGeometryUpdated(
        Guid eventId,
        BoundaryId boundaryId,
        IGeometry newGeometry,
        CanonicalTick tick,
        long sequence,
        TruthStreamIdentity streamIdentity)
        => new(eventId, boundaryId, newGeometry, tick, sequence, streamIdentity, EmptyHash, EmptyHash);

    /// <summary>Creates a BoundaryRetiredEvent with empty hash fields.</summary>
    public static BoundaryRetiredEvent BoundaryRetired(
        Guid eventId,
        BoundaryId boundaryId,
        string? reason,
        CanonicalTick tick,
        long sequence,
        TruthStreamIdentity streamIdentity)
        => new(eventId, boundaryId, reason, tick, sequence, streamIdentity, EmptyHash, EmptyHash);

    /// <summary>Creates a JunctionCreatedEvent with empty hash fields.</summary>
    public static JunctionCreatedEvent JunctionCreated(
        Guid eventId,
        JunctionId junctionId,
        ImmutableArray<BoundaryId> boundaryIds,
        SurfacePoint location,
        CanonicalTick tick,
        long sequence,
        TruthStreamIdentity streamIdentity)
        => new(eventId, junctionId, boundaryIds, location, tick, sequence, streamIdentity, EmptyHash, EmptyHash);

    /// <summary>Creates a JunctionCreatedEvent from 2D coords (legacy helper - converts to sphere).</summary>
    public static JunctionCreatedEvent JunctionCreated(
        Guid eventId,
        JunctionId junctionId,
        BoundaryId[] boundaryIds,
        Point2 location,
        CanonicalTick tick,
        long sequence,
        TruthStreamIdentity streamIdentity)
    {
        // Convert 2D planar coords to sphere surface point (z=0 plane, radius=1)
        var normal = UnitVector3d.FromComponents(location.X, location.Y, 0) ?? UnitVector3d.UnitZ;
        var surfacePoint = SurfacePoint.UnitSphere(normal);
        return new(eventId, junctionId, boundaryIds.ToImmutableArray(), surfacePoint, tick, sequence, streamIdentity, EmptyHash, EmptyHash);
    }

    /// <summary>Creates a JunctionUpdatedEvent with empty hash fields.</summary>
    public static JunctionUpdatedEvent JunctionUpdated(
        Guid eventId,
        JunctionId junctionId,
        ImmutableArray<BoundaryId> newBoundaryIds,
        SurfacePoint? newLocation,
        CanonicalTick tick,
        long sequence,
        TruthStreamIdentity streamIdentity)
        => new(eventId, junctionId, newBoundaryIds, newLocation, tick, sequence, streamIdentity, EmptyHash, EmptyHash);

    /// <summary>Creates a JunctionUpdatedEvent from 2D coords (legacy helper - converts to sphere).</summary>
    public static JunctionUpdatedEvent JunctionUpdated(
        Guid eventId,
        JunctionId junctionId,
        BoundaryId[] newBoundaryIds,
        Point2? newLocation,
        CanonicalTick tick,
        long sequence,
        TruthStreamIdentity streamIdentity)
    {
        SurfacePoint? surfacePoint = null;
        if (newLocation.HasValue)
        {
            var normal = UnitVector3d.FromComponents(newLocation.Value.X, newLocation.Value.Y, 0) ?? UnitVector3d.UnitZ;
            surfacePoint = SurfacePoint.UnitSphere(normal);
        }
        return new(eventId, junctionId, newBoundaryIds.ToImmutableArray(), surfacePoint, tick, sequence, streamIdentity, EmptyHash, EmptyHash);
    }

    /// <summary>Creates a JunctionRetiredEvent with empty hash fields.</summary>
    public static JunctionRetiredEvent JunctionRetired(
        Guid eventId,
        JunctionId junctionId,
        string? reason,
        CanonicalTick tick,
        long sequence,
        TruthStreamIdentity streamIdentity)
        => new(eventId, junctionId, reason, tick, sequence, streamIdentity, EmptyHash, EmptyHash);
}
