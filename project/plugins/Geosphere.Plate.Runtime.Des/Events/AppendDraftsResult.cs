namespace FantaSim.Geosphere.Plate.Runtime.Des.Events;

public record AppendDraftsResult(
    long LastSequence,
    ReadOnlyMemory<byte> LastHash
);
