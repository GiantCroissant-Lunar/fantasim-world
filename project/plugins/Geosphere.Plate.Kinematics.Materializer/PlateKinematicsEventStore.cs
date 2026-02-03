using System.Buffers.Binary;
using System.Text;
using FantaSim.Geosphere.Plate.Kinematics.Contracts.Events;
using FantaSim.Geosphere.Plate.Kinematics.Serializers;
using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;
using UnifyStorage.Abstractions;

namespace FantaSim.Geosphere.Plate.Kinematics.Materializer;

public sealed class PlateKinematicsEventStore : IKinematicsEventStore, IDisposable
{
    private const string EventPrefix = "E:";
    private const string HeadSuffix = "Head";

    private readonly IKeyValueStore _store;
    private readonly object _lock = new();
    // Tri-state cache: -1 = false, 0 = unknown, 1 = true
    private volatile int _supportsIterator;
    private volatile int _supportsWriteBatch;

    public PlateKinematicsEventStore(IKeyValueStore store)
    {
        ArgumentNullException.ThrowIfNull(store);
        _store = store;
    }

    public Task AppendAsync(
        TruthStreamIdentity stream,
        IEnumerable<IPlateKinematicsEvent> events,
        CancellationToken cancellationToken)
    {
        return AppendAsync(stream, events, AppendOptions.Default, cancellationToken);
    }

    public Task AppendAsync(
        TruthStreamIdentity stream,
        IEnumerable<IPlateKinematicsEvent> events,
        AppendOptions options,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(events);
        options ??= AppendOptions.Default;

        if (!stream.IsValid())
        {
            throw new InvalidOperationException(
                $"TruthStreamIdentity is not valid: {stream}. " +
                "Ensure VariantId, BranchId, Model are non-empty, LLevel >= 0, and Domain is well-formed.");
        }

        var eventsList = events.ToList();
        if (eventsList.Count == 0)
            return Task.CompletedTask;

        foreach (var evt in eventsList)
        {
            if (evt.StreamIdentity != stream)
            {
                throw new ArgumentException(
                    $"Event {evt.EventId} stream identity {evt.StreamIdentity} does not match expected {stream}",
                    nameof(events));
            }
        }

        for (int i = 1; i < eventsList.Count; i++)
        {
            if (eventsList[i].Sequence <= eventsList[i - 1].Sequence)
            {
                throw new ArgumentException(
                    $"Events must have monotonically increasing Sequence numbers. Event {i} has Sequence {eventsList[i].Sequence} after {eventsList[i - 1].Sequence}",
                    nameof(events));
            }
        }

        ApplyTickMonotonicityPolicy(eventsList, options.TickPolicy);

        var prefix = BuildStreamPrefix(stream);

        IWriteBatch? batch = null;
        if (_supportsWriteBatch != -1)
        {
            try
            {
                batch = _store.CreateWriteBatch();
                _supportsWriteBatch = 1;
            }
            catch (Exception ex) when (ex is NotSupportedException or NotImplementedException)
            {
                _supportsWriteBatch = -1;
            }
        }

        var previousHash = GetPreviousHashForAppend(prefix);

        var headKey = BuildHeadKey(prefix);
        var lastSequence = eventsList[^1].Sequence;
        Span<byte> headBytes = stackalloc byte[8];
        BinaryPrimitives.WriteInt64BigEndian(headBytes, lastSequence);

        if (batch is not null)
        {
            using (batch)
            {
                foreach (var evt in eventsList)
                {
                    var eventKey = BuildEventKey(prefix, evt.Sequence);
                    var eventBytes = MessagePackKinematicsEventSerializer.Serialize(evt);

                    var schemaVersion = MessagePackEventRecordSerializer.SchemaVersionV1;
                    var recordSequence = evt.Sequence;
                    var hash = MessagePackEventRecordSerializer.ComputeHashV1(schemaVersion, recordSequence, previousHash, eventBytes);
                    var recordBytes = MessagePackEventRecordSerializer.SerializeRecord(schemaVersion, recordSequence, previousHash, hash, eventBytes);

                    batch.Put(eventKey, recordBytes);
                    previousHash = hash;
                }

                batch.Put(headKey, headBytes);

                lock (_lock)
                {
                    _store.Write(batch);
                }
            }
        }
        else
        {
            // Fallback for minimal KV backends without write batches (single-writer MVP only).
            lock (_lock)
            {
                foreach (var evt in eventsList)
                {
                    var eventKey = BuildEventKey(prefix, evt.Sequence);
                    var eventBytes = MessagePackKinematicsEventSerializer.Serialize(evt);

                    var schemaVersion = MessagePackEventRecordSerializer.SchemaVersionV1;
                    var recordSequence = evt.Sequence;
                    var hash = MessagePackEventRecordSerializer.ComputeHashV1(schemaVersion, recordSequence, previousHash, eventBytes);
                    var recordBytes = MessagePackEventRecordSerializer.SerializeRecord(schemaVersion, recordSequence, previousHash, hash, eventBytes);

                    _store.Put(eventKey, recordBytes);
                    previousHash = hash;
                }

                _store.Put(headKey, headBytes);
            }
        }

        return Task.CompletedTask;
    }

    private static void ApplyTickMonotonicityPolicy(
        List<IPlateKinematicsEvent> events,
        TickMonotonicityPolicy policy)
    {
        if (policy == TickMonotonicityPolicy.Allow || events.Count < 2)
            return;

        for (int i = 1; i < events.Count; i++)
        {
            if (events[i].Tick.Value < events[i - 1].Tick.Value)
            {
                var message = $"Tick decreased from {events[i - 1].Tick.Value} to {events[i].Tick.Value} " +
                              $"at event index {i} (Sequence {events[i].Sequence})";

                switch (policy)
                {
                    case TickMonotonicityPolicy.Warn:
                        System.Diagnostics.Debug.WriteLine($"[TickMonotonicity.Warn] {message}");
                        break;
                    case TickMonotonicityPolicy.Reject:
                        throw new InvalidOperationException(
                            $"Tick monotonicity violation: {message}. " +
                            "Use TickMonotonicityPolicy.Allow or Warn to permit tick decreases.");
                }
            }
        }
    }

    public async IAsyncEnumerable<IPlateKinematicsEvent> ReadAsync(
        TruthStreamIdentity stream,
        long fromSequenceInclusive,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (fromSequenceInclusive < 0)
            throw new ArgumentOutOfRangeException(nameof(fromSequenceInclusive), "Sequence must be non-negative");

        if (!stream.IsValid())
        {
            throw new InvalidOperationException(
                $"TruthStreamIdentity is not valid: {stream}. " +
                "Ensure VariantId, BranchId, Model are non-empty, LLevel >= 0, and Domain is well-formed.");
        }

        var prefix = BuildStreamPrefix(stream);
        var firstKey = BuildEventKey(prefix, fromSequenceInclusive);

        var expectedPreviousHash = GetExpectedPreviousHashForRead(prefix, fromSequenceInclusive);

        await Task.Yield();

        var bytesToYield = new List<byte[]>();

        var useIterator = _supportsIterator != -1;
        if (useIterator)
        {
            lock (_lock)
            {
                IKeyValueIterator? iterator = null;
                try
                {
                    iterator = _store.CreateIterator();
                    _supportsIterator = 1;
                }
                catch (Exception ex) when (ex is NotSupportedException or NotImplementedException)
                {
                    _supportsIterator = -1;
                }

                if (iterator is not null)
                {
                    using (iterator)
                    {
                        iterator.Seek(firstKey);

                        while (iterator.Valid && HasStreamPrefix(iterator.Key, prefix))
                        {
                            bytesToYield.Add(iterator.Value.ToArray());
                            iterator.Next();
                        }
                    }
                }
            }
        }

        if (_supportsIterator == -1)
        {
            var lastSequence = await GetLastSequenceAsync(stream, cancellationToken).ConfigureAwait(false);
            if (lastSequence.HasValue && lastSequence.Value >= fromSequenceInclusive)
            {
                for (var seq = fromSequenceInclusive; seq <= lastSequence.Value; seq++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var eventKey = BuildEventKey(prefix, seq);
                    byte[]? recordBytes;
                    lock (_lock)
                    {
                        recordBytes = ReadBytes(eventKey);
                    }

                    if (recordBytes == null || recordBytes.Length == 0)
                    {
                        throw new InvalidOperationException(
                            $"Missing kinematics event record at sequence {seq} for stream {stream}. " +
                            "This suggests partial persistence or corruption.");
                    }

                    bytesToYield.Add(recordBytes);
                }
            }
        }

        foreach (var recordBytes in bytesToYield)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (MessagePackEventRecordSerializer.TryDeserializeRecord(recordBytes, out var record))
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
                yield return MessagePackKinematicsEventSerializer.Deserialize(recordBytes);
            }
        }
    }

    public Task<long?> GetLastSequenceAsync(TruthStreamIdentity stream, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);

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
            if (!_store.TryGet(headKey, buffer, out written))
            {
                return Task.FromResult<long?>(null);
            }
        }

        if (written != 8)
            throw new InvalidOperationException($"Head value must be 8 bytes, got {written}");

        var value = BinaryPrimitives.ReadInt64BigEndian(buffer);
        return Task.FromResult<long?>(value);
    }

    private static IPlateKinematicsEvent DeserializeAndValidateRecord(MessagePackEventRecordSerializer.EventRecordV1 record)
    {
        if (record.SchemaVersion != MessagePackEventRecordSerializer.SchemaVersionV1)
            throw new InvalidOperationException($"Unsupported schemaVersion: {record.SchemaVersion}");

        var expectedHash = MessagePackEventRecordSerializer.ComputeHashV1(
            record.SchemaVersion,
            record.Tick,
            record.PreviousHash,
            record.EventBytes);

        if (!expectedHash.AsSpan().SequenceEqual(record.Hash))
            throw new InvalidOperationException("EventRecord hash mismatch");

        var evt = MessagePackKinematicsEventSerializer.Deserialize(record.EventBytes);
        if (evt.Sequence != record.Tick)
            throw new InvalidOperationException("EventRecord tick does not match event payload sequence");

        return evt;
    }

    private byte[] GetPreviousHashForAppend(byte[] prefix)
    {
        var lastSequence = ReadHead(prefix);
        if (lastSequence is null)
            return MessagePackEventRecordSerializer.GetZeroHash();

        var lastEventKey = BuildEventKey(prefix, lastSequence.Value);
        byte[]? lastValue;
        lock (_lock)
        {
            lastValue = ReadBytes(lastEventKey);
        }

        if (lastValue != null && MessagePackEventRecordSerializer.TryDeserializeRecord(lastValue, out var lastRecord))
        {
            return lastRecord.Hash;
        }

        return MessagePackEventRecordSerializer.GetZeroHash();
    }

    private byte[] GetExpectedPreviousHashForRead(byte[] prefix, long fromSequenceInclusive)
    {
        if (fromSequenceInclusive <= 0)
            return MessagePackEventRecordSerializer.GetZeroHash();

        var previousEventKey = BuildEventKey(prefix, fromSequenceInclusive - 1);

        byte[]? previousValue;
        lock (_lock)
        {
            previousValue = ReadBytes(previousEventKey);
        }

        if (previousValue != null && MessagePackEventRecordSerializer.TryDeserializeRecord(previousValue, out var previousRecord))
        {
            return previousRecord.Hash;
        }

        return MessagePackEventRecordSerializer.GetZeroHash();
    }

    private long? ReadHead(byte[] prefix)
    {
        var headKey = BuildHeadKey(prefix);

        Span<byte> buffer = stackalloc byte[8];
        int written;

        lock (_lock)
        {
            if (!_store.TryGet(headKey, buffer, out written))
                return null;
        }

        if (written != 8)
            throw new InvalidOperationException($"Head value must be 8 bytes, got {written}");

        return BinaryPrimitives.ReadInt64BigEndian(buffer);
    }

    private byte[]? ReadBytes(byte[] key)
    {
        Span<byte> initialBuffer = stackalloc byte[1];
        if (_store.TryGet(key, initialBuffer, out var written))
        {
            return initialBuffer.Slice(0, written).ToArray();
        }

        if (written > 0)
        {
            var result = new byte[written];
            if (_store.TryGet(key, result, out _))
            {
                return result;
            }
            throw new InvalidOperationException("Store state changed during read");
        }

        return null;
    }

    private static byte[] BuildStreamPrefix(TruthStreamIdentity stream)
    {
        return Encoding.UTF8.GetBytes($"S:{stream.VariantId}:{stream.BranchId}:L{stream.LLevel}:{stream.Domain}:M{stream.Model}:");
    }

    private static byte[] BuildEventKey(byte[] prefix, long sequence)
    {
        var key = new byte[prefix.Length + EventPrefix.Length + 8];
        Buffer.BlockCopy(prefix, 0, key, 0, prefix.Length);
        Encoding.UTF8.GetBytes(EventPrefix, 0, EventPrefix.Length, key, prefix.Length);
        var offset = prefix.Length + EventPrefix.Length;
        BinaryPrimitives.WriteUInt64BigEndian(key.AsSpan(offset), (ulong)sequence);
        return key;
    }

    private static byte[] BuildHeadKey(byte[] prefix)
    {
        var key = new byte[prefix.Length + HeadSuffix.Length];
        Buffer.BlockCopy(prefix, 0, key, 0, prefix.Length);
        Encoding.UTF8.GetBytes(HeadSuffix, 0, HeadSuffix.Length, key, prefix.Length);
        return key;
    }

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

    public void Dispose() => _store.Dispose();
}
