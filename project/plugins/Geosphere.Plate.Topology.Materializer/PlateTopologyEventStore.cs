using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using FantaSim.Geosphere.Plate.Topology.Contracts.Capabilities;
using FantaSim.Geosphere.Plate.Topology.Contracts.Derived;
using FantaSim.Geosphere.Plate.Topology.Contracts.Events;
using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;
using FantaSim.Geosphere.Plate.Topology.Serializers;
using UnifyStorage.Abstractions;

namespace FantaSim.Geosphere.Plate.Topology.Materializer;

/// <summary>
/// Implementation of ITopologyEventStore per RFC-V2-0004 and RFC-V2-0005.
///
/// Key design per persistence RFC:
/// - Stream prefix: "S:{variant}:{branch}:L{l}:{domain}:M{m}:"
/// - Event key: "{prefix}E:{seq}" where seq is big-endian uint64 (8 bytes fixed-width)
/// - Last sequence key: "{prefix}Head"
/// - Snapshot key: "{prefix}Snap:{tick}" where tick is big-endian uint64
/// - Capabilities key: "{prefix}Meta:Caps" ??9-byte bitset
/// - Event value: MessagePack envelope [eventType:string, payload:binary]
///
/// Features:
/// - Atomic batch append per stream
/// - Deterministic replay by Sequence ordering
/// - Stream isolation by TruthStreamIdentity
/// - Efficient range queries via lexicographic key ordering
/// - Stream capability tracking for safe optimizations
/// </summary>
public sealed class PlateTopologyEventStore : ITopologyEventStore, IPlateTopologySnapshotStore, ITruthStreamCapabilities, IDisposable
{
    private const string EventPrefix = "E:";
    private const string HeadSuffix = "Head";
    private const string SnapshotPrefix = "Snap:";
    private const string MetaCapsKey = "Meta:Caps";
    private readonly IKeyValueStore _store;
    private readonly object _lock = new();

    /// <summary>
    /// Per-stream locks for optimistic concurrency control.
    /// Ensures read-modify-write atomicity within a single process.
    /// Key is the stream prefix string for efficient lookup.
    /// </summary>
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _streamLocks = new(StringComparer.Ordinal);

    /// <summary>
    /// Opens or creates an event store at the specified path.
    /// </summary>
    /// <param name="store">Ordered key-value store implementation.</param>
    public PlateTopologyEventStore(IKeyValueStore store)
    {
        ArgumentNullException.ThrowIfNull(store);
        _store = store;
    }

    /// <summary>
    /// Appends a batch of events to the specified stream atomically.
    ///
    /// Uses WriteBatch to ensure atomicity: either all events succeed
    /// or none are persisted. Validates that all events match the stream identity
    /// and have monotonically increasing Sequence numbers.
    ///
    /// Note: This overload uses default options (tick policy = Allow).
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
        return AppendAsync(stream, events, AppendOptions.Default, cancellationToken);
    }

    /// <summary>
    /// Appends a batch of events to the specified stream atomically with custom options.
    ///
    /// Uses WriteBatch to ensure atomicity: either all events succeed
    /// or none are persisted. Validates that all events match the stream identity
    /// and have monotonically increasing Sequence numbers.
    ///
    /// Tick monotonicity policy is controlled via options:
    /// - Allow: tick can decrease without any action (default)
    /// - Warn: tick decrease logs a warning but allows append
    /// - Reject: tick decrease throws InvalidOperationException
    /// </summary>
    /// <exception cref="ArgumentException">
    /// If events don't match stream identity or sequences are not monotonic.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// If stream identity is not valid, or if tick decreases and policy is Reject.
    /// </exception>
    /// <exception cref="ConcurrencyConflictException">
    /// If <see cref="AppendOptions.ExpectedHead"/> is set and the actual head doesn't match.
    /// </exception>
    public async Task AppendAsync(
        TruthStreamIdentity stream,
        IEnumerable<IPlateTopologyEvent> events,
        AppendOptions options,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(events);
        options ??= AppendOptions.Default;

        // Validate stream identity per RFC-V2-0001 review recommendation
        if (!stream.IsValid())
        {
            throw new InvalidOperationException(
                $"TruthStreamIdentity is not valid: {stream}. " +
                "Ensure VariantId, BranchId, Model are non-empty, LLevel >= 0, and Domain is well-formed.");
        }

        var eventsList = events.ToList();
        if (eventsList.Count == 0)
            return;

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

        // Apply tick monotonicity policy
        ApplyTickMonotonicityPolicy(eventsList, options.TickPolicy);

        // Build batch atomically
        var prefix = BuildStreamPrefix(stream);
        var prefixString = Encoding.UTF8.GetString(prefix);

        // Acquire per-stream lock for optimistic concurrency
        // This ensures read-check-write atomicity within a single process
        var streamLock = _streamLocks.GetOrAdd(prefixString, _ => new SemaphoreSlim(1, 1));
        await streamLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // Check optimistic concurrency precondition if specified
            if (options.ExpectedHead.HasValue)
            {
                var expected = options.ExpectedHead.Value;
                var actual = GetHeadInternal(prefix);

                if (expected.Sequence != actual.Sequence ||
                    !expected.Hash.AsSpan().SequenceEqual(actual.Hash))
                {
                    throw new ConcurrencyConflictException(stream, expected, actual);
                }
            }

            using var batch = _store.CreateWriteBatch();

            // Check if this is a genesis append (brand new stream)
            var isGenesisAppend = !StreamExists(prefix);

            var previousHash = GetPreviousHashForAppend(prefix);

            byte[] lastHash = previousHash;
            long lastTick = 0;

            foreach (var evt in eventsList)
            {
                var eventKey = BuildEventKey(prefix, evt.Sequence);
                var eventBytes = MessagePackEventSerializer.Serialize((IPlateTopologyEvent)evt);

                var schemaVersion = MessagePackEventRecordSerializer.SchemaVersionV1;
                // RFC-V2-0005: tick is hash-critical and must use the event's Tick, not Sequence
                var tick = evt.Tick.Value;
                var hash = MessagePackEventRecordSerializer.ComputeHashV1(schemaVersion, tick, previousHash, eventBytes);
                var recordBytes = MessagePackEventRecordSerializer.SerializeRecord(schemaVersion, tick, previousHash, hash, eventBytes);

                batch.Put(eventKey, recordBytes);
                previousHash = hash;
                lastHash = hash;
                lastTick = tick;
            }

            // Update head metadata per RFC-V2-0004: {lastSeq, lastHash, lastTick}
            var headKey = BuildHeadKey(prefix);
            var lastSequence = eventsList[^1].Sequence;
            var headBytes = HeadRecordSerializer.Serialize(lastSequence, lastHash, lastTick);
            batch.Put(headKey, headBytes);

            // For genesis appends with Reject policy, mark stream as tick-monotone
            // This is the ONLY safe time to set this flag - at stream creation with strict policy
            if (isGenesisAppend && options.TickPolicy == TickMonotonicityPolicy.Reject)
            {
                SetCapabilitiesInBatch(batch, prefix, StreamCapabilities.GenesisWithRejectPolicy);
            }

            // Write batch atomically
            lock (_lock)
            {
                _store.Write(batch);
            }
        }
        finally
        {
            streamLock.Release();
        }
    }

    /// <summary>
    /// Applies the tick monotonicity policy to a list of events.
    /// </summary>
    private static void ApplyTickMonotonicityPolicy(
        List<IPlateTopologyEvent> events,
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
                        // Log warning - implementation should use a proper logging framework
                        // For now, use Debug.WriteLine which is visible in tests
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
            using var iterator = _store.CreateIterator();
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

        byte[]? headBytes;
        lock (_lock)
        {
            headBytes = ReadBytes(headKey);
        }

        if (headBytes == null || headBytes.Length == 0)
        {
            return Task.FromResult<long?>(null);
        }

        if (HeadRecordSerializer.IsLegacyFormat(headBytes))
        {
            return Task.FromResult<long?>(HeadRecordSerializer.ReadLegacySequence(headBytes));
        }

        if (HeadRecordSerializer.TryDeserialize(headBytes, out var lastSeq, out _, out _))
        {
            return Task.FromResult<long?>(lastSeq);
        }

        throw new InvalidOperationException("Head value is not a recognized head record format.");
    }

    /// <summary>
    /// Gets the current head state (sequence + hash) of a stream.
    ///
    /// Use this method to obtain the precondition for optimistic concurrency control.
    /// The returned <see cref="StreamHead"/> can be converted to a <see cref="HeadPrecondition"/>
    /// via <see cref="StreamHead.ToPrecondition"/> and passed to <see cref="AppendOptions.ExpectedHead"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// If stream identity is not valid.
    /// </exception>
    public Task<StreamHead> GetHeadAsync(
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
        return Task.FromResult(GetHeadInternal(prefix));
    }

    /// <summary>
    /// Internal helper to get stream head without validation.
    /// Used by both GetHeadAsync and AppendAsync precondition check.
    /// </summary>
    private StreamHead GetHeadInternal(byte[] prefix)
    {
        var headKey = BuildHeadKey(prefix);

        byte[]? headBytes;
        lock (_lock)
        {
            headBytes = ReadBytes(headKey);
        }

        if (headBytes == null || headBytes.Length == 0)
        {
            return StreamHead.Empty;
        }

        if (!HeadRecordSerializer.IsLegacyFormat(headBytes) &&
            HeadRecordSerializer.TryDeserialize(headBytes, out var seq, out var hash, out var tick))
        {
            return new StreamHead(seq, hash, tick);
        }

        if (!HeadRecordSerializer.IsLegacyFormat(headBytes))
        {
            throw new InvalidOperationException("Head value is not a recognized head record format.");
        }

        var lastSequence = HeadRecordSerializer.ReadLegacySequence(headBytes);

        var lastEventKey = BuildEventKey(prefix, lastSequence);

        byte[]? lastValue;
        lock (_lock)
        {
            lastValue = ReadBytes(lastEventKey);
        }

        if (lastValue != null && MessagePackEventRecordSerializer.TryDeserializeRecord(lastValue, out var lastRecord))
        {
            return new StreamHead(lastSequence, lastRecord.Hash, lastRecord.Tick);
        }

        // Stream has head key but no event - shouldn't happen but return empty
        return StreamHead.Empty;
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
            _store.Put(key, bytes);
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
                bytes = ReadBytes(snapshotKey);
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

    /// <inheritdoc />
    public Task<PlateTopologySnapshot?> GetLatestSnapshotBeforeAsync(
        TruthStreamIdentity stream,
        long targetTick,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);

        if (!stream.IsValid())
        {
            throw new InvalidOperationException(
                $"TruthStreamIdentity is not valid: {stream}. " +
                "Ensure VariantId, BranchId, Model are non-empty, LLevel >= 0, and Domain is well-formed.");
        }

        if (targetTick < 0)
            return Task.FromResult<PlateTopologySnapshot?>(null);

        var prefix = BuildStreamPrefix(stream);
        var snapshotPrefix = BuildSnapshotPrefix(prefix);
        var targetKey = BuildSnapshotKey(prefix, targetTick);

        byte[]? bytes = null;

        lock (_lock)
        {
            using var iterator = _store.CreateIterator();
            iterator.SeekForPrev(targetKey);

            if (!iterator.Valid)
                return Task.FromResult<PlateTopologySnapshot?>(null);

            var foundKey = iterator.Key;

            // Check if the found key starts with our snapshot prefix
            // (ensures we don't accidentally read a snapshot from another stream)
            if (!foundKey.StartsWith(snapshotPrefix))
                return Task.FromResult<PlateTopologySnapshot?>(null);

            bytes = iterator.Value.ToArray();
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

        byte[]? headBytes;
        lock (_lock)
        {
            headBytes = ReadBytes(headKey);
        }

        if (headBytes == null || headBytes.Length == 0)
        {
            return MessagePackEventRecordSerializer.GetZeroHash();
        }

        if (!HeadRecordSerializer.IsLegacyFormat(headBytes) &&
            HeadRecordSerializer.TryDeserialize(headBytes, out _, out var lastHash, out _))
        {
            return lastHash;
        }

        if (!HeadRecordSerializer.IsLegacyFormat(headBytes))
        {
            throw new InvalidOperationException("Head value is not a recognized head record format.");
        }

        var lastSequence = HeadRecordSerializer.ReadLegacySequence(headBytes);
        var lastEventKey = BuildEventKey(prefix, lastSequence);

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
        {
            return MessagePackEventRecordSerializer.GetZeroHash();
        }

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
        if (evt.Tick.Value != record.Tick)
        {
            throw new InvalidOperationException("EventRecord tick does not match event payload tick");
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
    /// Builds the snapshot key prefix (without tick suffix).
    /// Used to verify SeekForPrev results belong to the same stream.
    /// </summary>
    private static byte[] BuildSnapshotPrefix(byte[] streamPrefix)
    {
        var key = new byte[streamPrefix.Length + SnapshotPrefix.Length];
        Buffer.BlockCopy(streamPrefix, 0, key, 0, streamPrefix.Length);
        Encoding.UTF8.GetBytes(SnapshotPrefix, 0, SnapshotPrefix.Length, key, streamPrefix.Length);
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
    /// Builds the metadata capabilities key for a stream.
    ///
    /// Format: "{prefix}Meta:Caps"
    /// </summary>
    private static byte[] BuildMetaCapsKey(byte[] prefix)
    {
        var key = new byte[prefix.Length + MetaCapsKey.Length];
        Buffer.BlockCopy(prefix, 0, key, 0, prefix.Length);
        Encoding.UTF8.GetBytes(MetaCapsKey, 0, MetaCapsKey.Length, key, prefix.Length);
        return key;
    }

    #region ITruthStreamCapabilities

    /// <inheritdoc />
    public ValueTask<bool> IsTickMonotoneFromGenesisAsync(
        TruthStreamIdentity stream,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        if (!stream.IsValid())
            return ValueTask.FromResult(false);

        var prefix = BuildStreamPrefix(stream);
        var capsKey = BuildMetaCapsKey(prefix);

        lock (_lock)
        {
            var capsBytes = ReadBytes(capsKey);
            if (capsBytes == null || capsBytes.Length < StreamCapabilities.StorageSize)
                return ValueTask.FromResult(false);

            var caps = StreamCapabilities.FromBytes(capsBytes);

            // Guard: If monotone flag is set but RejectFromGenesis isn't, something is wrong
            // This protects against corrupted metadata or future misuse
            if (caps.IsTickMonotoneFromGenesis && !caps.IsTickPolicyRejectFromGenesis)
            {
                Trace.WriteLineIf(
                    DiagnosticSwitches.CapabilityValidation.TraceWarning,
                    $"[EventStore] Stream {stream} has TickMonotoneFromGenesis=true but TickPolicyRejectFromGenesis=false. " +
                    "This is inconsistent - ignoring monotone flag for safety.");
                return ValueTask.FromResult(false);
            }

            return ValueTask.FromResult(caps.IsTickMonotoneFromGenesis);
        }
    }

    /// <summary>
    /// Gets the full capabilities for a stream.
    /// </summary>
    /// <param name="stream">The truth stream identity.</param>
    /// <returns>Stream capabilities, or None if not set.</returns>
    public StreamCapabilities GetCapabilities(TruthStreamIdentity stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        if (!stream.IsValid())
            return StreamCapabilities.None;

        var prefix = BuildStreamPrefix(stream);
        var capsKey = BuildMetaCapsKey(prefix);

        lock (_lock)
        {
            var capsBytes = ReadBytes(capsKey);
            if (capsBytes != null)
            {
                return StreamCapabilities.FromBytes(capsBytes);
            }
            return StreamCapabilities.None;
        }
    }

    /// <summary>
    /// Sets the capabilities for a stream atomically in a write batch.
    ///
    /// This is typically called during the first append to a new stream
    /// when using TickMonotonicityPolicy.Reject.
    /// </summary>
    private void SetCapabilitiesInBatch(IWriteBatch batch, byte[] prefix, StreamCapabilities caps)
    {
        var capsKey = BuildMetaCapsKey(prefix);
        var capsBytes = caps.ToBytes();
        batch.Put(capsKey, capsBytes);
    }

    /// <summary>
    /// Checks if a stream exists (has any events).
    /// </summary>
    private bool StreamExists(byte[] prefix)
    {
        var headKey = BuildHeadKey(prefix);
        lock (_lock)
        {
            Span<byte> buffer = stackalloc byte[0];
            int written;
            return _store.TryGet(headKey, buffer, out written);
        }
    }

    private byte[]? ReadBytes(byte[] key)
    {
        // First try with a small stack buffer to get size
        Span<byte> initialBuffer = stackalloc byte[1];
        if (_store.TryGet(key, initialBuffer, out var written))
        {
            // Value fit in 1 byte (or was empty)
            return initialBuffer.Slice(0, written).ToArray();
        }

        if (written > 0)
        {
            var result = new byte[written];
            if (_store.TryGet(key, result, out var finalWritten))
            {
                return result;
            }
            // Should not happen if store is consistent under lock
            throw new InvalidOperationException("Store state changed during read");
        }

        return null;
    }

    #endregion

    /// <summary>
    /// Disposes the store and releases resources.
    /// </summary>
    public void Dispose()
    {
        _store.Dispose();
    }
}
