using System.Buffers.Binary;
using System.Text;
using Plate.Topology.Contracts.Derived;
using Plate.Topology.Contracts.Events;
using Plate.Topology.Contracts.Identity;
using Plate.Topology.Serializers;
using RocksDb.Managed;

namespace Plate.Topology.Materializer;

/// <summary>
/// RocksDB-based implementation of ITopologyEventStore per RFC-V2-0004 and RFC-V2-0005.
///
/// Key design per persistence RFC:
/// - Stream prefix: "S:{variant}:{branch}:L{l}:{domain}:M{m}:"
/// - Event key: "{prefix}E:{seq}" where seq is big-endian uint64 (8 bytes fixed-width)
/// - Last sequence key: "{prefix}Head"
/// - Event value: MessagePack envelope [eventType:string, payload:binary]
///
/// Features:
/// - Atomic batch append per stream
/// - Deterministic replay by Sequence ordering
/// - Stream isolation by TruthStreamIdentity
/// - Efficient range queries via lexicographic key ordering
/// </summary>
public sealed class PlateTopologyEventStore : ITopologyEventStore, IPlateTopologySnapshotStore, IDisposable
{
    private const string EventPrefix = "E:";
    private const string HeadSuffix = "Head";
    private const string SnapshotPrefix = "Snap:";
    private readonly Db _db;
    private readonly object _lock = new();

    /// <summary>
    /// Opens or creates a RocksDB event store at the specified path.
    /// </summary>
    /// <param name="dbPath">Path to the RocksDB database directory.</param>
    public PlateTopologyEventStore(string dbPath)
    {
        ArgumentNullException.ThrowIfNull(dbPath);
        _db = Db.Open(dbPath);
    }

    /// <summary>
    /// Appends a batch of events to the specified stream atomically.
    ///
    /// Uses RocksDB WriteBatch to ensure atomicity: either all events succeed
    /// or none are persisted. Validates that all events match the stream identity
    /// and have monotonically increasing Sequence numbers.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// If events don't match stream identity or sequences are not monotonic.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// If stream identity is not valid.
    /// </exception>
    public Task AppendAsync(
        TruthStreamIdentity stream,
        IEnumerable<IPlateTopologyEvent> events,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(events);

        // Validate stream identity per RFC-V2-0001 review recommendation
        if (!stream.IsValid())
        {
            throw new InvalidOperationException(
                $"TruthStreamIdentity is not valid: {stream}. " +
                "Ensure VariantId, BranchId, Model are non-empty, LLevel >= 0, and Domain is well-formed.");
        }

        var eventsList = events.ToList();
        if (eventsList.Count == 0)
            return Task.CompletedTask;

        // Validate all events belong to the same stream
        foreach (var evt in eventsList)
        {
            if (evt.StreamIdentity != stream)
            {
                throw new ArgumentException(
                    $"Event {evt.EventId} stream identity {evt.StreamIdentity} does not match expected {stream}",
                    nameof(events));
            }
        }

        // Validate sequences are monotonically increasing
        for (int i = 1; i < eventsList.Count; i++)
        {
            if (eventsList[i].Sequence <= eventsList[i - 1].Sequence)
            {
                throw new ArgumentException(
                    $"Events must have monotonically increasing Sequence numbers. Event {i} has Sequence {eventsList[i].Sequence} after {eventsList[i - 1].Sequence}",
                    nameof(events));
            }
        }

        // Build batch atomically
        var prefix = BuildStreamPrefix(stream);
        using var batch = new WriteBatch();

        var previousHash = GetPreviousHashForAppend(prefix);

        foreach (var evt in eventsList)
        {
            var eventKey = BuildEventKey(prefix, evt.Sequence);
            var eventBytes = MessagePackEventSerializer.Serialize((IPlateTopologyEvent)evt);

            var schemaVersion = MessagePackEventRecordSerializer.SchemaVersionV1;
            var tick = evt.Sequence;
            var hash = MessagePackEventRecordSerializer.ComputeHashV1(schemaVersion, tick, previousHash, eventBytes);
            var recordBytes = MessagePackEventRecordSerializer.SerializeRecord(schemaVersion, tick, previousHash, hash, eventBytes);

            batch.Put(eventKey, recordBytes);
            previousHash = hash;
        }

        // Update last sequence
        var headKey = BuildHeadKey(prefix);
        var lastSequence = eventsList[^1].Sequence;
        Span<byte> headBytes = stackalloc byte[8];
        BinaryPrimitives.WriteInt64BigEndian(headBytes, lastSequence);
        batch.Put(headKey, headBytes);

        // Write batch atomically
        lock (_lock)
        {
            _db.Write(batch);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Reads events from a stream starting from a specific Sequence number.
    ///
    /// Returns events in ascending Sequence order for deterministic replay.
    /// Uses range scan starting from the first key >= fromSequenceInclusive.
    /// Stream isolation is enforced via key prefix.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// If stream identity is not valid.
    /// </exception>
    public async IAsyncEnumerable<IPlateTopologyEvent> ReadAsync(
        TruthStreamIdentity stream,
        long fromSequenceInclusive,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (fromSequenceInclusive < 0)
            throw new ArgumentOutOfRangeException(nameof(fromSequenceInclusive), "Sequence must be non-negative");

        // Validate stream identity per RFC-V2-0001 review recommendation
        if (!stream.IsValid())
        {
            throw new InvalidOperationException(
                $"TruthStreamIdentity is not valid: {stream}. " +
                "Ensure VariantId, BranchId, Model are non-empty, LLevel >= 0, and Domain is well-formed.");
        }

        var prefix = BuildStreamPrefix(stream);
        var firstKey = BuildEventKey(prefix, fromSequenceInclusive);

        var expectedPreviousHash = GetExpectedPreviousHashForRead(prefix, fromSequenceInclusive);

        await Task.Yield(); // Ensure async pattern

        var bytesToYield = new List<byte[]>();
        lock (_lock)
        {
            using var iterator = _db.CreateIterator();
            iterator.Seek(firstKey);

            while (iterator.Valid && HasStreamPrefix(iterator.Key, prefix))
            {
                bytesToYield.Add(iterator.Value.ToArray());
                iterator.Next();
            }
        }

        foreach (var eventBytes in bytesToYield)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (MessagePackEventRecordSerializer.TryDeserializeRecord(eventBytes, out var record))
            {
                if (!record.PreviousHash.AsSpan().SequenceEqual(expectedPreviousHash))
                {
                    throw new InvalidOperationException("EventRecord previousHash mismatch");
                }

                var evt = DeserializeAndValidateRecord(record);
                expectedPreviousHash = record.Hash;
                yield return evt;
            }
            else
            {
                expectedPreviousHash = MessagePackEventRecordSerializer.GetZeroHash();
                yield return MessagePackEventSerializer.Deserialize(eventBytes);
            }
        }
    }

    /// <summary>
    /// Gets the highest Sequence number for a stream.
    ///
    /// Returns null if the stream is empty or does not exist.
    /// Reads from the Head metadata key which stores the last sequence number
    /// as a big-endian uint64.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// If stream identity is not valid.
    /// </exception>
    public Task<long?> GetLastSequenceAsync(
        TruthStreamIdentity stream,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);

        // Validate stream identity per RFC-V2-0001 review recommendation
        if (!stream.IsValid())
        {
            throw new InvalidOperationException(
                $"TruthStreamIdentity is not valid: {stream}. " +
                "Ensure VariantId, BranchId, Model are non-empty, LLevel >= 0, and Domain is well-formed.");
        }

        var prefix = BuildStreamPrefix(stream);
        var headKey = BuildHeadKey(prefix);

        Span<byte> buffer = stackalloc byte[8];
        int written;

        lock (_lock)
        {
            if (!_db.TryGet(headKey, buffer, out written))
            {
                return Task.FromResult<long?>(null);
            }
        }

        if (written != 8)
        {
            throw new InvalidOperationException($"Head value must be 8 bytes, got {written}");
        }

        // Decode big-endian uint64 to long
        var value = BinaryPrimitives.ReadInt64BigEndian(buffer);
        return Task.FromResult<long?>(value);
    }

    public Task SaveSnapshotAsync(PlateTopologySnapshot snapshot, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        if (!snapshot.Key.Stream.IsValid())
        {
            throw new InvalidOperationException(
                $"TruthStreamIdentity is not valid: {snapshot.Key.Stream}. " +
                "Ensure VariantId, BranchId, Model are non-empty, LLevel >= 0, and Domain is well-formed.");
        }

        if (snapshot.Key.Tick < 0)
            throw new ArgumentOutOfRangeException(nameof(snapshot), "Snapshot tick must be non-negative");

        var prefix = BuildStreamPrefix(snapshot.Key.Stream);
        var key = BuildSnapshotKey(prefix, snapshot.Key.Tick);
        var bytes = MessagePackPlateTopologySnapshotSerializer.Serialize(snapshot);

        lock (_lock)
        {
            _db.Put(key, bytes);
        }

        return Task.CompletedTask;
    }

    public Task<PlateTopologySnapshot?> GetSnapshotAsync(PlateTopologyMaterializationKey key, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(key);

        if (!key.Stream.IsValid())
        {
            throw new InvalidOperationException(
                $"TruthStreamIdentity is not valid: {key.Stream}. " +
                "Ensure VariantId, BranchId, Model are non-empty, LLevel >= 0, and Domain is well-formed.");
        }

        if (key.Tick < 0)
            return Task.FromResult<PlateTopologySnapshot?>(null);

        var prefix = BuildStreamPrefix(key.Stream);
        var snapshotKey = BuildSnapshotKey(prefix, key.Tick);

        byte[]? bytes;
        lock (_lock)
        {
            try
            {
                bytes = _db.Get(snapshotKey);
            }
            catch
            {
                bytes = null;
            }
        }

        if (bytes == null || bytes.Length == 0)
            return Task.FromResult<PlateTopologySnapshot?>(null);

        var snapshot = MessagePackPlateTopologySnapshotSerializer.Deserialize(bytes);
        return Task.FromResult<PlateTopologySnapshot?>(snapshot);
    }

    /// <summary>
    /// Builds the stream prefix key component.
    ///
    /// Format: "S:{variant}:{branch}:L{l}:{domain}:M{m}:"
    /// </summary>
    private static byte[] BuildStreamPrefix(TruthStreamIdentity stream)
    {
        return Encoding.UTF8.GetBytes($"S:{stream.VariantId}:{stream.BranchId}:L{stream.LLevel}:{stream.Domain}:M{stream.Model}:");
    }

    private byte[] GetPreviousHashForAppend(byte[] prefix)
    {
        var headKey = BuildHeadKey(prefix);

        Span<byte> headBuffer = stackalloc byte[8];
        int headWritten;

        lock (_lock)
        {
            if (!_db.TryGet(headKey, headBuffer, out headWritten))
            {
                return MessagePackEventRecordSerializer.GetZeroHash();
            }
        }

        if (headWritten != 8)
        {
            throw new InvalidOperationException($"Head value must be 8 bytes, got {headWritten}");
        }

        var lastSequence = BinaryPrimitives.ReadInt64BigEndian(headBuffer);
        var lastEventKey = BuildEventKey(prefix, lastSequence);

        byte[] lastValue;
        lock (_lock)
        {
            lastValue = _db.Get(lastEventKey);
        }

        if (MessagePackEventRecordSerializer.TryDeserializeRecord(lastValue, out var lastRecord))
        {
            return lastRecord.Hash;
        }

        return MessagePackEventRecordSerializer.GetZeroHash();
    }

    private byte[] GetExpectedPreviousHashForRead(byte[] prefix, long fromSequenceInclusive)
    {
        if (fromSequenceInclusive <= 0)
        {
            return MessagePackEventRecordSerializer.GetZeroHash();
        }

        var previousEventKey = BuildEventKey(prefix, fromSequenceInclusive - 1);

        byte[] previousValue;
        lock (_lock)
        {
            previousValue = _db.Get(previousEventKey);
        }

        if (MessagePackEventRecordSerializer.TryDeserializeRecord(previousValue, out var previousRecord))
        {
            return previousRecord.Hash;
        }

        return MessagePackEventRecordSerializer.GetZeroHash();
    }

    private static IPlateTopologyEvent DeserializeAndValidateRecord(MessagePackEventRecordSerializer.EventRecordV1 record)
    {
        if (record.SchemaVersion != MessagePackEventRecordSerializer.SchemaVersionV1)
        {
            throw new InvalidOperationException($"Unsupported schemaVersion: {record.SchemaVersion}");
        }

        var expectedHash = MessagePackEventRecordSerializer.ComputeHashV1(
            record.SchemaVersion,
            record.Tick,
            record.PreviousHash,
            record.EventBytes);

        if (!expectedHash.AsSpan().SequenceEqual(record.Hash))
        {
            throw new InvalidOperationException("EventRecord hash mismatch");
        }

        var evt = MessagePackEventSerializer.Deserialize(record.EventBytes);
        if (evt.Sequence != record.Tick)
        {
            throw new InvalidOperationException("EventRecord tick does not match event payload sequence");
        }

        return evt;
    }

    /// <summary>
    /// Builds an event key from stream prefix and Sequence number.
    ///
    /// Format: "{prefix}E:{seq}" where seq is big-endian uint64 (8 bytes).
    /// Big-endian encoding ensures lexicographic ordering matches numeric ordering.
    /// </summary>
    private static byte[] BuildEventKey(byte[] prefix, long sequence)
    {
        var key = new byte[prefix.Length + EventPrefix.Length + 8];

        // Copy prefix
        Buffer.BlockCopy(prefix, 0, key, 0, prefix.Length);

        // Copy "E:" suffix
        Encoding.UTF8.GetBytes(EventPrefix, 0, EventPrefix.Length, key, prefix.Length);

        // Encode sequence as big-endian uint64
        var offset = prefix.Length + EventPrefix.Length;
        BinaryPrimitives.WriteUInt64BigEndian(key.AsSpan(offset), (ulong)sequence);

        return key;
    }

    /// <summary>
    /// Builds the Head metadata key for a stream.
    ///
    /// Format: "{prefix}Head"
    /// </summary>
    private static byte[] BuildHeadKey(byte[] prefix)
    {
        var key = new byte[prefix.Length + HeadSuffix.Length];
        Buffer.BlockCopy(prefix, 0, key, 0, prefix.Length);
        Encoding.UTF8.GetBytes(HeadSuffix, 0, HeadSuffix.Length, key, prefix.Length);
        return key;
    }

    private static byte[] BuildSnapshotKey(byte[] prefix, long tick)
    {
        var key = new byte[prefix.Length + SnapshotPrefix.Length + 8];

        Buffer.BlockCopy(prefix, 0, key, 0, prefix.Length);
        Encoding.UTF8.GetBytes(SnapshotPrefix, 0, SnapshotPrefix.Length, key, prefix.Length);

        var offset = prefix.Length + SnapshotPrefix.Length;
        BinaryPrimitives.WriteUInt64BigEndian(key.AsSpan(offset), (ulong)tick);

        return key;
    }

    /// <summary>
    /// Checks if a key starts with the given stream prefix.
    ///
    /// Used for range scan termination to ensure we don't read into another stream.
    /// </summary>
    private static bool HasStreamPrefix(ReadOnlySpan<byte> key, byte[] prefix)
    {
        if (key.Length < prefix.Length)
            return false;

        if (!key[..prefix.Length].SequenceEqual(prefix))
            return false;

        var requiredLength = prefix.Length + EventPrefix.Length;
        if (key.Length < requiredLength)
            return false;

        return key[prefix.Length] == (byte)'E' && key[prefix.Length + 1] == (byte)':';
    }

    /// <summary>
    /// Disposes the RocksDB store and releases resources.
    /// </summary>
    public void Dispose()
    {
        _db.Dispose();
    }
}
