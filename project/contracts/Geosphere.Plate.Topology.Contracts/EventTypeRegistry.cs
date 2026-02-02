using System;
using System.Collections.Generic;
using FantaSim.Geosphere.Plate.Topology.Contracts.Events;

namespace FantaSim.Geosphere.Plate.Topology.Contracts;

/// <summary>
/// Registry for stable bidirectional mapping between event type IDs and CLR types.
///
/// Provides O(1) lookup for both directions with startup validation to ensure
/// one-to-one mapping. Used by MessagePackEventSerializer for polymorphic dispatch.
/// </summary>
public static class EventTypeRegistry
{
    private static readonly Dictionary<string, Type> IdToType = new(StringComparer.Ordinal);
    private static readonly Dictionary<Type, string> TypeToId = new();

    static EventTypeRegistry()
    {
        // One-to-one mapping validation at startup
        var mappings = new (string id, Type type)[]
        {
            ("PlateCreatedEvent", typeof(PlateCreatedEvent)),
            ("PlateRetiredEvent", typeof(PlateRetiredEvent)),
            ("BoundaryCreatedEvent", typeof(BoundaryCreatedEvent)),
            ("BoundaryTypeChangedEvent", typeof(BoundaryTypeChangedEvent)),
            ("BoundaryGeometryUpdatedEvent", typeof(BoundaryGeometryUpdatedEvent)),
            ("BoundaryRetiredEvent", typeof(BoundaryRetiredEvent)),
            ("JunctionCreatedEvent", typeof(JunctionCreatedEvent)),
            ("JunctionUpdatedEvent", typeof(JunctionUpdatedEvent)),
            ("JunctionRetiredEvent", typeof(JunctionRetiredEvent))
        };

        foreach (var (id, type) in mappings)
        {
            if (IdToType.ContainsKey(id))
                throw new InvalidOperationException($"Duplicate event type ID: {id}");
            if (TypeToId.ContainsKey(type))
                throw new InvalidOperationException($"Duplicate event type: {type}");

            IdToType[id] = type;
            TypeToId[type] = id;
        }
    }

    /// <summary>
    /// Resolves a CLR type from its event type ID string.
    /// </summary>
    /// <param name="id">The event type ID string (e.g., "PlateCreatedEvent").</param>
    /// <returns>The CLR Type.</returns>
    /// <exception cref="UnknownEventTypeException">Thrown when the ID is not registered.</exception>
    public static Type Resolve(string id)
    {
        if (IdToType.TryGetValue(id, out var type))
            return type;

        throw new UnknownEventTypeException(id);
    }

    /// <summary>
    /// Gets the event type ID string for a CLR type.
    /// </summary>
    /// <param name="type">The CLR event type.</param>
    /// <returns>The event type ID string.</returns>
    /// <exception cref="UnregisteredEventTypeException">Thrown when the type is not registered.</exception>
    public static string GetId(Type type)
    {
        if (TypeToId.TryGetValue(type, out var id))
            return id;

        throw new UnregisteredEventTypeException(type);
    }

    /// <summary>
    /// Tries to resolve a CLR type from its event type ID string.
    /// </summary>
    /// <param name="id">The event type ID string.</param>
    /// <param name="type">The resolved CLR type if found; otherwise, null.</param>
    /// <returns>True if the type was resolved; otherwise, false.</returns>
    public static bool TryResolve(string id, out Type? type)
    {
        return IdToType.TryGetValue(id, out type);
    }
}
