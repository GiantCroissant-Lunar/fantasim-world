# EventStore MVP Implementation Plan

## Overview

Implement a minimal viable event store following the DB-first doctrine with RocksDB and MessagePack canonical encoding.

**Scope**:
- RocksDB persistence (via modern-rocksdb) with proper column families
- MessagePack canonical encoding for events
- Sequence key encoding (uint64 big-endian)
- Hash-chain validation
- Basic stream operations (append, read, stream metadata)

**Out of Scope**:
- Snapshots (deferred to future spec)
- Read models (deferred to future spec)
- Specific domain events (deferred to topology spec)

## Precondition Checklist

Before starting implementation, ensure:
- [ ] modern-rocksdb is available at `D:\lunar-snake\personal-work\plate-projects\modern-rocksdb`
- [ ] .NET 8.0 SDK is installed
- [ ] Task (taskfile) is installed

---

## Phase 1: Spec-Kit Setup

### 1.1 Initialize spec-kit in main worktree

**Command**:
```powershell
task spec:init
```

**Expected Output**:
- `.specify/` directory created in main worktree
- `memory/constitution.md` initialized

**Acceptance**: `.specify/memory/constitution.md` exists

### 1.2 Create eventstore-mvp worktree

**Command** (in main worktree):
```powershell
task spec:new -- eventstore-mvp
```

**Expected Output**:
- Branch `spec/eventstore-mvp` created
- Worktree `../fantasim-world--eventstore-mvp` created
- `specs/eventstore-mvp/` directory created in worktree

**Acceptance**:
- `git worktree list` shows new worktree
- `../fantasim-world--eventstore-mvp/specs/eventstore-mvp/` exists

### 1.3 Enter worktree

**Command**:
```powershell
cd ../fantasim-world--eventstore-mvp
```

**Acceptance**: Current directory is `../fantasim-world--eventstore-mvp`

---

## Phase 2: Specification

### 2.1 Run specify phase

**Command**:
```powershell
task spec:specify FEATURE=eventstore-mvp
```

**Expected Artifacts**:
- `specs/eventstore-mvp/spec.md` with:
  - Problem statement
  - Success criteria
  - Slice tags: `Persistence`
  - Constraints: DB-first, RocksDB, MessagePack

**Acceptance**:
- `specs/eventstore-mvp/spec.md` exists
- Contains slice tags and constraints

### 2.2 Verify spec alignment

**Manual Check**:
- Spec references RFC-V2-0004 and RFC-V2-0005
- Spec mentions column families: `events`, `stream_meta`, `snapshots`, `read_models`
- Spec describes stream prefix format
- Spec describes sequence key encoding (uint64 big-endian)

---

## Phase 3: Planning

### 3.1 Run plan phase

**Command**:
```powershell
task spec:plan FEATURE=eventstore-mvp
```

**Expected Artifacts**:
- `specs/eventstore-mvp/plan.md` with:
  - Technical approach
  - Architecture decisions
  - Dependencies on modern-rocksdb and MessagePack-CSharp
  - Key design (stream prefix, sequence encoding)
  - MessagePack schema design

**Acceptance**:
- `specs/eventstore-mvp/plan.md` exists
- References ADR-0005, ADR-0006, RFC-V2-0004, RFC-V2-0005
- Describes .NET project structure

### 3.2 Plan structure verification

**Manual Check**:
- Plan specifies project layout (e.g., `src/Fantasim.Persistence/`)
- Plan defines event envelope interface
- Plan defines MessagePack encoding rules
- Plan includes testing strategy

---

## Phase 4: Tasks Generation

### 4.1 Run tasks phase

**Command**:
```powershell
task spec:tasks FEATURE=eventstore-mvp
```

**Expected Artifacts**:
- `specs/eventstore-mvp/tasks.md` with numbered, agent-separable tasks

**Acceptance**:
- `specs/eventstore-mvp/tasks.md` exists
- Tasks are independently completable
- Each task has clear success criteria

---

## Phase 5: Implementation

**Note**: This phase will be executed in build mode. The following is the task breakdown.

### 5.1 Initialize .NET solution and project structure

**Command**:
```powershell
dotnet new sln -n Fantasim
dotnet new classlib -n Fantasim.Persistence -o src/Fantasim.Persistence
dotnet sln add src/Fantasim.Persistence/Fantasim.Persistence.csproj
dotnet new xunit -n Fantasim.Persistence.Tests -o tests/Fantasim.Persistence.Tests
dotnet sln add tests/Fantasim.Persistence.Tests/Fantasim.Persistence.Tests.csproj
dotnet add tests/Fantasim.Persistence.Tests/Fantasim.Persistence.Tests.csproj reference src/Fantasim.Persistence/Fantasim.Persistence.csproj
```

**Expected Artifacts**:
- `Fantasim.sln`
- `src/Fantasim.Persistence/Fantasim.Persistence.csproj`
- `tests/Fantasim.Persistence.Tests/Fantasim.Persistence.Tests.csproj`

**Acceptance**:
- `dotnet build` succeeds
- Projects reference each other correctly

### 5.2 Add package dependencies

**Commands**:
```powershell
# Add modern-rocksdb (local reference)
dotnet add src/Fantasim.Persistence/Fantasim.Persistence.csproj reference ../modern-rocksdb/dotnet/src/RocksDb.Managed/RocksDb.Managed.csproj
dotnet add src/Fantasim.Persistence/Fantasim.Persistence.csproj reference ../modern-rocksdb/dotnet/src/RocksDb.Abstractions/RocksDb.Abstractions.csproj
dotnet add src/Fantasim.Persistence/Fantasim.Persistence.csproj reference ../modern-rocksdb/dotnet/src/RocksDb.Providers/RocksDb.Providers.csproj

# Add MessagePack-CSharp
dotnet add src/Fantasim.Persistence/Fantasim.Persistence.csproj package MessagePack
dotnet add tests/Fantasim.Persistence.Tests/Fantasim.Persistence.Tests.csproj package MessagePack
dotnet add tests/Fantasim.Persistence.Tests/Fantasim.Persistence.Tests.csproj package FluentAssertions
```

**Expected Artifacts**:
- Updated `.csproj` files with package references

**Acceptance**:
- `dotnet restore` succeeds
- `dotnet build` succeeds

### 5.3 Define core types and interfaces

**Files to create**:

#### `src/Fantasim.Persistence/Events/EventEnvelope.cs`
```csharp
using MessagePack;

namespace Fantasim.Persistence.Events;

[MessagePackObject]
public sealed record EventEnvelope
{
    [Key(0)] public string VariantId { get; init; } = string.Empty;
    [Key(1)] public string BranchId { get; init; } = string.Empty;
    [Key(2)] public int LLevel { get; init; }
    [Key(3)] public string Domain { get; init; } = string.Empty;
    [Key(4)] public string Model { get; init; } = string.Empty;
    [Key(5)] public long Tick { get; init; }
    [Key(6)] public string EventType { get; init; } = string.Empty;
    [Key(7)] public ReadOnlyMemory<byte> Payload { get; init; }
    [Key(8)] public ReadOnlyMemory<byte> PreviousHash { get; init; }
    [Key(9)] public ReadOnlyMemory<byte> Hash { get; init; }
}
```

#### `src/Fantasim.Persistence/Events/EventId.cs`
```csharp
namespace Fantasim.Persistence.Events;

public readonly record struct EventId(
    string VariantId,
    string BranchId,
    int LLevel,
    string Domain,
    string Model,
    ulong Sequence);
```

#### `src/Fantasim.Persistence/Storage/IEventStore.cs`
```csharp
namespace Fantasim.Persistence.Storage;

public interface IEventStore
{
    ValueTask<ulong> AppendAsync(EventEnvelope envelope, CancellationToken cancellationToken = default);
    ValueTask<EventEnvelope?> GetAsync(EventId eventId, CancellationToken cancellationToken = default);
    IAsyncEnumerable<EventEnvelope> ReadStreamAsync(
        string variantId,
        string branchId,
        int lLevel,
        string domain,
        string model,
        ulong afterSequence = 0,
        CancellationToken cancellationToken = default);
}
```

**Acceptance**:
- Types compile
- MessagePack attributes present on `EventEnvelope`

### 5.4 Implement MessagePack canonical encoding tests

**File**: `tests/Fantasim.Persistence.Tests/Encoding/MessagePackCanonicalEncodingTests.cs`

**Tests to implement**:

#### 5.4.1 Test: Explicit integer keys enforce determinism
```csharp
[Fact]
public void EventEnvelope_MessagePack_UsesExplicitIntegerKeys()
{
    var envelope = new EventEnvelope
    {
        VariantId = "test-variant",
        BranchId = "main",
        LLevel = 0,
        Domain = "test-domain",
        Model = "test-model",
        Tick = 100,
        EventType = "TestEvent",
        Payload = new byte[] { 1, 2, 3 },
        PreviousHash = new byte[] { 0, 0, 0, 0 },
        Hash = new byte[] { 1, 1, 1, 1 }
    };

    // Serialize twice and verify bytes are identical
    var bytes1 = MessagePackSerializer.Serialize(envelope);
    var bytes2 = MessagePackSerializer.Serialize(envelope);

    bytes1.Should().BeEquivalentTo(bytes2);

    // Verify no map type is used (MessagePack format byte for map is 0x8X)
    // Fixed arrays use 0x9X for small arrays, 0xDC/0xDD for larger arrays
    bytes1[0].Should().NotBeInRange(0x80, 0x8F); // Not a fixed map
    bytes1[0].Should().NotBe(0xDE); // Not a map16
    bytes1[0].Should().NotBe(0xDF); // Not a map32
}
```

#### 5.4.2 Test: No floats in canonical encoding
```csharp
[Fact]
public void EventEnvelope_HasNoFloatingPointFields()
{
    // This test ensures we don't accidentally introduce nondeterminism
    var envelopeType = typeof(EventEnvelope);
    var properties = envelopeType.GetProperties();

    foreach (var prop in properties)
    {
        prop.PropertyType.Should().NotBe(typeof(float), $"{prop.Name} should not be float");
        prop.PropertyType.Should().NotBe(typeof(double), $"{prop.Name} should not be double");
    }
}
```

**Acceptance**:
- All tests pass
- Encoding is deterministic

### 5.5 Implement sequence key encoding utilities

**File**: `src/Fantasim.Persistence/Storage/KeyEncoding.cs`

```csharp
namespace Fantasim.Persistence.Storage;

public static class KeyEncoding
{
    /// <summary>
    /// Encode a uint64 as big-endian 8-byte sequence for RocksDB keys.
    /// This preserves numeric ordering under lexicographic byte ordering.
    /// </summary>
    public static byte[] EncodeSequenceKey(ulong sequence)
    {
        var buffer = new byte[8];
        buffer[0] = (byte)((sequence >> 56) & 0xFF);
        buffer[1] = (byte)((sequence >> 48) & 0xFF);
        buffer[2] = (byte)((sequence >> 40) & 0xFF);
        buffer[3] = (byte)((sequence >> 32) & 0xFF);
        buffer[4] = (byte)((sequence >> 24) & 0xFF);
        buffer[5] = (byte)((sequence >> 16) & 0xFF);
        buffer[6] = (byte)((sequence >> 8) & 0xFF);
        buffer[7] = (byte)(sequence & 0xFF);
        return buffer;
    }

    /// <summary>
    /// Decode a uint64 from big-endian 8-byte sequence.
    /// </summary>
    public static ulong DecodeSequenceKey(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length != 8)
            throw new ArgumentException("Sequence key must be 8 bytes", nameof(bytes));

        return ((ulong)bytes[0] << 56) |
               ((ulong)bytes[1] << 48) |
               ((ulong)bytes[2] << 40) |
               ((ulong)bytes[3] << 32) |
               ((ulong)bytes[4] << 24) |
               ((ulong)bytes[5] << 16) |
               ((ulong)bytes[6] << 8) |
               bytes[7];
    }
}
```

**File**: `tests/Fantasim.Persistence.Tests/Storage/KeyEncodingTests.cs`

```csharp
public class KeyEncodingTests
{
    [Fact]
    public void EncodeSequenceKey_PreservesOrdering()
    {
        // Test that numeric ordering is preserved under byte ordering
        var seq1 = KeyEncoding.EncodeSequenceKey(1);
        var seq2 = KeyEncoding.EncodeSequenceKey(10);
        var seq3 = KeyEncoding.EncodeSequenceKey(100);
        var seq4 = KeyEncoding.EncodeSequenceKey(1000);

        // Lexicographic compare should match numeric compare
        seq1.AsSpan().SequenceCompareTo(seq2).Should().BeNegative();
        seq2.AsSpan().SequenceCompareTo(seq3).Should().BeNegative();
        seq3.AsSpan().SequenceCompareTo(seq4).Should().BeNegative();
    }

    [Fact]
    public void EncodeDecodeSequenceKey_Roundtrip()
    {
        var testCases = new ulong[] { 0, 1, 100, 1000, 1000000, ulong.MaxValue };

        foreach (var original in testCases)
        {
            var encoded = KeyEncoding.EncodeSequenceKey(original);
            var decoded = KeyEncoding.DecodeSequenceKey(encoded);
            decoded.Should().Be(original);
        }
    }

    [Fact]
    public void EncodeSequenceKey_FixedWidth8Bytes()
    {
        var seq1 = KeyEncoding.EncodeSequenceKey(1);
        var seq2 = KeyEncoding.EncodeSequenceKey(ulong.MaxValue);

        seq1.Length.Should().Be(8);
        seq2.Length.Should().Be(8);
    }
}
```

**Acceptance**:
- All tests pass
- Encoding preserves lexicographic ordering
- Fixed-width 8 bytes

### 5.6 Implement stream key builder

**File**: `src/Fantasim.Persistence/Storage/StreamKeyBuilder.cs`

```csharp
using System.Text;

namespace Fantasim.Persistence.Storage;

public static class StreamKeyBuilder
{
    /// <summary>
    /// Build stream prefix: S:{variant}:{branch}:L{l}:{domain}:M{m}:
    /// </summary>
    public static string BuildStreamPrefix(
        string variantId,
        string branchId,
        int lLevel,
        string domain,
        string model)
    {
        return $"S:{variantId}:{branchId}:L{lLevel}:{domain}:M{model}:";
    }

    /// <summary>
    /// Build event key: S:{...}:E:{seq} (seq is uint64 big-endian 8 bytes)
    /// </summary>
    public static byte[] BuildEventKey(
        string variantId,
        string branchId,
        int lLevel,
        string domain,
        string model,
        ulong sequence)
    {
        var prefix = BuildStreamPrefix(variantId, branchId, lLevel, domain, model);
        var prefixBytes = Encoding.UTF8.GetBytes(prefix);
        var seqBytes = KeyEncoding.EncodeSequenceKey(sequence);

        var keyBytes = new byte[prefixBytes.Length + seqBytes.Length];
        Buffer.BlockCopy(prefixBytes, 0, keyBytes, 0, prefixBytes.Length);
        Buffer.BlockCopy(seqBytes, 0, keyBytes, prefixBytes.Length, seqBytes.Length);

        return keyBytes;
    }

    /// <summary>
    /// Build stream head metadata key: S:{...}:Head
    /// </summary>
    public static string BuildStreamHeadKey(
        string variantId,
        string branchId,
        int lLevel,
        string domain,
        string model)
    {
        return $"{BuildStreamPrefix(variantId, branchId, lLevel, domain, model)}Head";
    }
}
```

**File**: `tests/Fantasim.Persistence.Tests/Storage/StreamKeyBuilderTests.cs`

```csharp
public class StreamKeyBuilderTests
{
    [Fact]
    public void BuildStreamPrefix_Format()
    {
        var prefix = StreamKeyBuilder.BuildStreamPrefix("v1", "main", 0, "test", "m1");
        prefix.Should().Be("S:v1:main:L0:test:Mm1:");
    }

    [Fact]
    public void BuildEventKey_EncodesSequenceProperly()
    {
        var key1 = StreamKeyBuilder.BuildEventKey("v1", "main", 0, "test", "m1", 1);
        var key2 = StreamKeyBuilder.BuildEventKey("v1", "main", 0, "test", "m1", 10);

        // Keys should be lexicographically sortable by sequence
        key1.AsSpan().SequenceCompareTo(key2).Should().BeNegative();
    }

    [Fact]
    public void BuildEventKey_Structure()
    {
        var key = StreamKeyBuilder.BuildEventKey("v1", "main", 0, "test", "m1", 100);

        // Verify structure: UTF-8 prefix + 8-byte sequence
        var prefixBytes = Encoding.UTF8.GetBytes("S:v1:main:L0:test:Mm1:");
        key.Should().HaveCount(prefixBytes.Length + 8);

        // Verify prefix matches
        var keyPrefix = key[..prefixBytes.Length];
        keyPrefix.Should().BeEquivalentTo(prefixBytes);
    }
}
```

**Acceptance**:
- All tests pass
- Keys are lexicographically sortable

### 5.7 Implement hash utilities

**File**: `src/Fantasim.Persistence/Events/HashUtils.cs`

```csharp
using System.Security.Cryptography;
using MessagePack;

namespace Fantasim.Persistence.Events;

public static class HashUtils
{
    /// <summary>
    /// Compute SHA-256 hash of MessagePack-encoded canonical bytes.
    /// Hash preimage includes: envelope fields (excluding hash itself) + payload + previousHash.
    /// </summary>
    public static byte[] ComputeHash(EventEnvelope envelope)
    {
        using var sha256 = SHA256.Create();

        // Create canonical hash preimage
        var preimage = CreateCanonicalHashPreimage(envelope);
        var preimageBytes = MessagePackSerializer.Serialize(preimage);

        return sha256.ComputeHash(preimageBytes);
    }

    /// <summary>
    /// Verify hash-chain linkage: current.previousHash == previous.hash
    /// </summary>
    public static bool VerifyHashChain(EventEnvelope current, EventEnvelope? previous)
    {
        if (previous == null)
            return current.PreviousHash.Length == 0;

        return current.PreviousHash.Span.SequenceEqual(previous.Hash.Span);
    }

    private static HashPreimage CreateCanonicalHashPreimage(EventEnvelope envelope)
    {
        return new HashPreimage
        {
            SchemaVersion = 1,
            VariantId = envelope.VariantId,
            BranchId = envelope.BranchId,
            LLevel = envelope.LLevel,
            Domain = envelope.Domain,
            Model = envelope.Model,
            Tick = envelope.Tick,
            EventType = envelope.EventType,
            Payload = envelope.Payload,
            PreviousHash = envelope.PreviousHash
        };
    }

    [MessagePackObject]
    private sealed record HashPreimage
    {
        [Key(0)] public int SchemaVersion { get; init; }
        [Key(1)] public string VariantId { get; init; } = string.Empty;
        [Key(2)] public string BranchId { get; init; } = string.Empty;
        [Key(3)] public int LLevel { get; init; }
        [Key(4)] public string Domain { get; init; } = string.Empty;
        [Key(5)] public string Model { get; init; } = string.Empty;
        [Key(6)] public long Tick { get; init; }
        [Key(7)] public string EventType { get; init; } = string.Empty;
        [Key(8)] public ReadOnlyMemory<byte> Payload { get; init; }
        [Key(9)] public ReadOnlyMemory<byte> PreviousHash { get; init; }
    }
}
```

**File**: `tests/Fantasim.Persistence.Tests/Events/HashUtilsTests.cs`

```csharp
public class HashUtilsTests
{
    [Fact]
    public void ComputeHash_Deterministic()
    {
        var envelope = new EventEnvelope
        {
            VariantId = "v1",
            BranchId = "main",
            LLevel = 0,
            Domain = "test",
            Model = "m1",
            Tick = 100,
            EventType = "TestEvent",
            Payload = new byte[] { 1, 2, 3 },
            PreviousHash = new byte[0],
            Hash = new byte[32] // placeholder
        };

        var hash1 = HashUtils.ComputeHash(envelope);
        var hash2 = HashUtils.ComputeHash(envelope);

        hash1.Should().BeEquivalentTo(hash2);
        hash1.Should().HaveCount(32); // SHA-256 output
    }

    [Fact]
    public void VerifyHashChain_GenesisEvent()
    {
        var genesis = new EventEnvelope
        {
            VariantId = "v1",
            BranchId = "main",
            LLevel = 0,
            Domain = "test",
            Model = "m1",
            Tick = 0,
            EventType = "Genesis",
            Payload = new byte[] { },
            PreviousHash = new byte[0],
            Hash = HashUtils.ComputeHash(new EventEnvelope
            {
                VariantId = "v1",
                BranchId = "main",
                LLevel = 0,
                Domain = "test",
                Model = "m1",
                Tick = 0,
                EventType = "Genesis",
                Payload = new byte[] { },
                PreviousHash = new byte[0],
                Hash = new byte[32]
            })
        };

        HashUtils.VerifyHashChain(genesis, null).Should().BeTrue();
    }

    [Fact]
    public void VerifyHashChain_LinkedEvents()
    {
        var event1 = new EventEnvelope
        {
            VariantId = "v1",
            BranchId = "main",
            LLevel = 0,
            Domain = "test",
            Model = "m1",
            Tick = 1,
            EventType = "Event1",
            Payload = new byte[] { 1 },
            PreviousHash = new byte[0],
            Hash = HashUtils.ComputeHash(new EventEnvelope
            {
                VariantId = "v1",
                BranchId = "main",
                LLevel = 0,
                Domain = "test",
                Model = "m1",
                Tick = 1,
                EventType = "Event1",
                Payload = new byte[] { 1 },
                PreviousHash = new byte[0],
                Hash = new byte[32]
            })
        };

        var event2 = new EventEnvelope
        {
            VariantId = "v1",
            BranchId = "main",
            LLevel = 0,
            Domain = "test",
            Model = "m1",
            Tick = 2,
            EventType = "Event2",
            Payload = new byte[] { 2 },
            PreviousHash = event1.Hash,
            Hash = HashUtils.ComputeHash(new EventEnvelope
            {
                VariantId = "v1",
                BranchId = "main",
                LLevel = 0,
                Domain = "test",
                Model = "m1",
                Tick = 2,
                EventType = "Event2",
                Payload = new byte[] { 2 },
                PreviousHash = event1.Hash,
                Hash = new byte[32]
            })
        };

        HashUtils.VerifyHashChain(event2, event1).Should().BeTrue();
    }

    [Fact]
    public void VerifyHashChain_BrokenChain()
    {
        var event1 = new EventEnvelope
        {
            VariantId = "v1",
            BranchId = "main",
            LLevel = 0,
            Domain = "test",
            Model = "m1",
            Tick = 1,
            EventType = "Event1",
            Payload = new byte[] { 1 },
            PreviousHash = new byte[0],
            Hash = new byte[] { 1, 2, 3, /* ... */ }
        };

        var event2 = new EventEnvelope
        {
            VariantId = "v1",
            BranchId = "main",
            LLevel = 0,
            Domain = "test",
            Model = "m1",
            Tick = 2,
            EventType = "Event2",
            Payload = new byte[] { 2 },
            PreviousHash = new byte[] { 9, 9, 9, /* ... */ }, // Wrong hash
            Hash = new byte[] { 4, 5, 6, /* ... */ }
        };

        HashUtils.VerifyHashChain(event2, event1).Should().BeFalse();
    }
}
```

**Acceptance**:
- All tests pass
- Hash computation is deterministic
- Hash-chain validation works correctly

### 5.8 Implement stream metadata

**File**: `src/Fantasim.Persistence/Storage/StreamMetadata.cs`

```csharp
using MessagePack;

namespace Fantasim.Persistence.Storage;

[MessagePackObject]
public sealed record StreamMetadata
{
    [Key(0)] public ulong LastSequence { get; init; }
    [Key(1)] public ReadOnlyMemory<byte> LastHash { get; init; }
    [Key(2)] public long LastTick { get; init; }

    public static StreamMetadata Empty => new()
    {
        LastSequence = 0,
        LastHash = new byte[0],
        LastTick = -1
    };
}
```

### 5.9 Implement RocksDB EventStore

**File**: `src/Fantasim.Persistence/Storage/RocksDbEventStore.cs`

```csharp
using System.Buffers;
using MessagePack;
using RocksDb.Managed;
using RocksDb.Providers;

namespace Fantasim.Persistence.Storage;

public sealed class RocksDbEventStore : IEventStore, IDisposable
{
    private const string EventsColumnFamily = "events";
    private const string StreamMetaColumnFamily = "stream_meta";

    private readonly RocksDbKeyValueStore _store;
    private readonly ColumnFamily _eventsCf;
    private readonly ColumnFamily _streamMetaCf;
    private readonly SemaphoreSlim _appendLock = new(1, 1);

    private RocksDbEventStore(RocksDbKeyValueStore store, Db db)
    {
        _store = store;
        _eventsCf = db.GetColumnFamily(EventsColumnFamily);
        _streamMetaCf = db.GetColumnFamily(StreamMetaColumnFamily);
    }

    public static RocksDbEventStore Open(string path, DbOptions? options = null)
    {
        var columnFamilies = new[]
        {
            ("default", new DbOptions()),
            (EventsColumnFamily, new DbOptions()),
            (StreamMetaColumnFamily, new DbOptions())
        };

        var db = Db.Open(path, options, columnFamilies);
        var store = RocksDbKeyValueStore.Open(path, options);
        return new RocksDbEventStore(store, db);
    }

    public async ValueTask<ulong> AppendAsync(EventEnvelope envelope, CancellationToken cancellationToken = default)
    {
        await _appendLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // Get current stream metadata
            var streamKey = StreamKeyBuilder.BuildStreamHeadKey(
                envelope.VariantId,
                envelope.BranchId,
                envelope.LLevel,
                envelope.Domain,
                envelope.Model);

            var metadata = GetStreamMetadata(streamKey);

            // Validate previous hash
            if (metadata.LastTick >= 0 && !HashUtils.VerifyHashChain(envelope, null))
            {
                // For non-genesis, verify previous hash matches metadata
                if (!envelope.PreviousHash.Span.SequenceEqual(metadata.LastHash.Span))
                {
                    throw new InvalidOperationException("Previous hash does not match stream head");
                }
            }

            // Compute hash
            var hash = HashUtils.ComputeHash(envelope);
            var hashArray = hash.ToArray();
            var hashedEnvelope = envelope with { Hash = hashArray };

            // Generate next sequence
            var nextSequence = metadata.LastSequence + 1;

            // Store event
            var eventKey = StreamKeyBuilder.BuildEventKey(
                envelope.VariantId,
                envelope.BranchId,
                envelope.LLevel,
                envelope.Domain,
                envelope.Model,
                nextSequence);

            var eventValue = MessagePackSerializer.Serialize(hashedEnvelope);

            using var batch = _store.CreateWriteBatch();
            batch.Put(eventKey, eventValue);

            // Update stream metadata
            var newMetadata = new StreamMetadata
            {
                LastSequence = nextSequence,
                LastHash = hashArray,
                LastTick = envelope.Tick
            };

            var metaValue = MessagePackSerializer.Serialize(newMetadata);
            var metaKeyBytes = System.Text.Encoding.UTF8.GetBytes(streamKey);

            batch.Put(metaKeyBytes, metaValue);

            _store.Write(batch);

            return nextSequence;
        }
        finally
        {
            _appendLock.Release();
        }
    }

    public async ValueTask<EventEnvelope?> GetAsync(EventId eventId, CancellationToken cancellationToken = default)
    {
        var key = StreamKeyBuilder.BuildEventKey(
            eventId.VariantId,
            eventId.BranchId,
            eventId.LLevel,
            eventId.Domain,
            eventId.Model,
            eventId.Sequence);

        return GetEventByKey(key);
    }

    public async IAsyncEnumerable<EventEnvelope> ReadStreamAsync(
        string variantId,
        string branchId,
        int lLevel,
        string domain,
        string model,
        ulong afterSequence = 0,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var prefix = StreamKeyBuilder.BuildStreamPrefix(variantId, branchId, lLevel, domain, model);
        var prefixBytes = System.Text.Encoding.UTF8.GetBytes(prefix);

        using var iterator = _store.CreateIterator();

        if (afterSequence > 0)
        {
            var seekKey = StreamKeyBuilder.BuildEventKey(variantId, branchId, lLevel, domain, model, afterSequence + 1);
            iterator.Seek(seekKey);
        }
        else
        {
            iterator.Seek(prefixBytes);
        }

        while (iterator.Valid && !cancellationToken.IsCancellationRequested)
        {
            var currentKey = iterator.Key;

            // Check if we're still in the same stream
            if (currentKey.Length < prefixBytes.Length ||
                !currentKey[..prefixBytes.Length].SequenceEqual(prefixBytes))
            {
                break;
            }

            var envelope = MessagePackSerializer.Deserialize<EventEnvelope>(iterator.Value);
            if (envelope != null)
            {
                yield return envelope;
            }

            iterator.Next();
        }
    }

    private StreamMetadata GetStreamMetadata(string streamKey)
    {
        var keyBytes = System.Text.Encoding.UTF8.GetBytes(streamKey);
        var buffer = ArrayPool<byte>.Shared.Rent(4096);

        try
        {
            if (_store.TryGet(keyBytes, buffer, out var written) && written > 0)
            {
                return MessagePackSerializer.Deserialize<StreamMetadata>(buffer[..written])
                    ?? StreamMetadata.Empty;
            }

            return StreamMetadata.Empty;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private EventEnvelope? GetEventByKey(byte[] key)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(4096);

        try
        {
            if (_store.TryGet(key, buffer, out var written) && written > 0)
            {
                return MessagePackSerializer.Deserialize<EventEnvelope>(buffer[..written]);
            }

            return null;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    public void Dispose()
    {
        _appendLock.Dispose();
        _store.Dispose();
    }
}
```

**File**: `tests/Fantasim.Persistence.Tests/Storage/RocksDbEventStoreTests.cs`

```csharp
using System.IO;
using Xunit;

public class RocksDbEventStoreTests : IDisposable
{
    private readonly string _dbPath;
    private readonly RocksDbEventStore _store;

    public RocksDbEventStoreTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"test-rocksdb-{Guid.NewGuid():N}");
        _store = RocksDbEventStore.Open(_dbPath);
    }

    public void Dispose()
    {
        _store.Dispose();
        if (Directory.Exists(_dbPath))
        {
            Directory.Delete(_dbPath, true);
        }
    }

    [Fact]
    public async Task Append_FirstEvent_ReturnsSequence1()
    {
        var envelope = CreateGenesisEvent();

        var sequence = await _store.AppendAsync(envelope);

        sequence.Should().Be(1);
    }

    [Fact]
    public async Task Append_MultipleEvents_IncrementsSequence()
    {
        var event1 = CreateGenesisEvent();
        var event2 = CreateEvent(previousHash: new byte[32], tick: 1);

        var seq1 = await _store.AppendAsync(event1);
        var seq2 = await _store.AppendAsync(event2);

        seq1.Should().Be(1);
        seq2.Should().Be(2);
    }

    [Fact]
    public async Task Get_RetrievesStoredEvent()
    {
        var envelope = CreateGenesisEvent();
        var sequence = await _store.AppendAsync(envelope);

        var retrieved = await _store.GetAsync(new EventId(
            envelope.VariantId,
            envelope.BranchId,
            envelope.LLevel,
            envelope.Domain,
            envelope.Model,
            sequence));

        retrieved.Should().NotBeNull();
        retrieved!.EventType.Should().Be(envelope.EventType);
        retrieved.Hash.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ReadStream_ReturnsAllEventsInOrder()
    {
        var events = new List<EventEnvelope>
        {
            CreateGenesisEvent(),
            CreateEvent(new byte[32], 1),
            CreateEvent(new byte[32], 2)
        };

        foreach (var evt in events)
        {
            await _store.AppendAsync(evt);
        }

        var retrievedEvents = new List<EventEnvelope>();
        await foreach (var evt in _store.ReadStreamAsync("v1", "main", 0, "test", "m1"))
        {
            retrievedEvents.Add(evt);
        }

        retrievedEvents.Should().HaveCount(3);
        retrievedEvents[0].Tick.Should().Be(0);
        retrievedEvents[1].Tick.Should().Be(1);
        retrievedEvents[2].Tick.Should().Be(2);
    }

    [Fact]
    public async Task ReadStream_WithAfterSequence_SkipsEarlierEvents()
    {
        var events = new List<EventEnvelope>
        {
            CreateGenesisEvent(),
            CreateEvent(new byte[32], 1),
            CreateEvent(new byte[32], 2)
        };

        foreach (var evt in events)
        {
            await _store.AppendAsync(evt);
        }

        var retrievedEvents = new List<EventEnvelope>();
        await foreach (var evt in _store.ReadStreamAsync("v1", "main", 0, "test", "m1", afterSequence: 1))
        {
            retrievedEvents.Add(evt);
        }

        retrievedEvents.Should().HaveCount(1);
        retrievedEvents[0].Tick.Should().Be(2);
    }

    private EventEnvelope CreateGenesisEvent()
    {
        return new EventEnvelope
        {
            VariantId = "v1",
            BranchId = "main",
            LLevel = 0,
            Domain = "test",
            Model = "m1",
            Tick = 0,
            EventType = "Genesis",
            Payload = new byte[] { },
            PreviousHash = new byte[0],
            Hash = new byte[32]
        };
    }

    private EventEnvelope CreateEvent(byte[] previousHash, long tick)
    {
        return new EventEnvelope
        {
            VariantId = "v1",
            BranchId = "main",
            LLevel = 0,
            Domain = "test",
            Model = "m1",
            Tick = tick,
            EventType = $"Event{tick}",
            Payload = new byte[] { (byte)tick },
            PreviousHash = previousHash,
            Hash = new byte[32]
        };
    }
}
```

**Acceptance**:
- All tests pass
- Events are append-only
- Sequence numbers increment correctly
- Hash-chain validation works

### 5.10 Run all tests

**Command**:
```powershell
dotnet test
```

**Expected Output**:
- All tests pass
- No warnings or errors

**Acceptance**: All tests pass with 100% success rate

---

## Phase 6: Acceptance Tests (Comprehensive)

### 6.1 Sequence key encoding validation

**File**: `tests/Fantasim.Persistence.Tests/Acceptance/SequenceKeyEncodingTests.cs`

```csharp
public class SequenceKeyEncodingTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(255)]
    [InlineData(256)]
    [InlineData(65535)]
    [InlineData(65536)]
    [InlineData(16777215)]
    [InlineData(16777216)]
    [InlineData(uint.MaxValue)]
    [InlineData(ulong.MaxValue)]
    public void SequenceKey_EncodesAsUint64BigEndian8Bytes(ulong sequence)
    {
        var encoded = KeyEncoding.EncodeSequenceKey(sequence);

        // Fixed width 8 bytes
        encoded.Should().HaveCount(8);

        // Big-endian: MSB at index 0
        var decoded = KeyEncoding.DecodeSequenceKey(encoded);
        decoded.Should().Be(sequence);

        // Verify big-endian byte order
        encoded[0].Should().Be((byte)((sequence >> 56) & 0xFF));
        encoded[1].Should().Be((byte)((sequence >> 48) & 0xFF));
        encoded[2].Should().Be((byte)((sequence >> 40) & 0xFF));
        encoded[3].Should().Be((byte)((sequence >> 32) & 0xFF));
        encoded[4].Should().Be((byte)((sequence >> 24) & 0xFF));
        encoded[5].Should().Be((byte)((sequence >> 16) & 0xFF));
        encoded[6].Should().Be((byte)((sequence >> 8) & 0xFF));
        encoded[7].Should().Be((byte)(sequence & 0xFF));
    }

    [Fact]
    public void SequenceKey_PreservesLexicographicOrdering()
    {
        var sequences = new ulong[] { 0, 1, 10, 100, 1000, 10000, 100000, 1000000 };

        for (int i = 0; i < sequences.Length - 1; i++)
        {
            var key1 = KeyEncoding.EncodeSequenceKey(sequences[i]);
            var key2 = KeyEncoding.EncodeSequenceKey(sequences[i + 1]);

            // Lexicographic compare should match numeric compare
            key1.AsSpan().SequenceCompareTo(key2).Should().BeNegative(
                $"Sequence {sequences[i]} should order before {sequences[i + 1]}");
        }
    }
}
```

### 6.2 MessagePack determinism validation

**File**: `tests/Fantasim.Persistence.Tests/Acceptance/MessagePackDeterminismTests.cs`

```csharp
public class MessagePackDeterminismTests
{
    [Fact]
    public void EventEnvelope_NoMapsInCanonicalEncoding()
    {
        var envelope = new EventEnvelope
        {
            VariantId = "v1",
            BranchId = "main",
            LLevel = 0,
            Domain = "test",
            Model = "m1",
            Tick = 100,
            EventType = "TestEvent",
            Payload = new byte[] { 1, 2, 3 },
            PreviousHash = new byte[] { 0, 0, 0, 0 },
            Hash = new byte[] { 1, 1, 1, 1 }
        };

        var bytes = MessagePackSerializer.Serialize(envelope);

        // MessagePack map format bytes:
        // - 0x80-0x8F: fixmap (0-15 items)
        // - 0xDE: map16 (16-65535 items)
        // - 0xDF: map32 (65535+ items)

        bytes[0].Should().NotBeInRange(0x80, 0x8F, "Should not use fixmap");
        bytes[0].Should().NotBe(0xDE, "Should not use map16");
        bytes[0].Should().NotBe(0xDF, "Should not use map32");

        // Should use array format:
        // - 0x90-0x9F: fixarray (0-15 items)
        // - 0xDC: array16 (16-65535 items)
        // - 0xDD: array32 (65535+ items)

        bytes[0].Should().BeOneOf(0x90, 0x9A, 0xDC, 0xDD, "Should use array format");
    }

    [Fact]
    public void EventEnvelope_UsesExplicitIntegerKeys()
    {
        var envelope = new EventEnvelope
        {
            VariantId = "v1",
            BranchId = "main",
            LLevel = 0,
            Domain = "test",
            Model = "m1",
            Tick = 100,
            EventType = "TestEvent",
            Payload = new byte[] { 1, 2, 3 },
            PreviousHash = new byte[] { 0, 0, 0, 0 },
            Hash = new byte[] { 1, 1, 1, 1 }
        };

        var bytes = MessagePackSerializer.Serialize(envelope);

        // Verify using integer keys by checking the array structure
        // In array format, values follow each other directly
        // First byte should be array format with 10 items
        bytes[0].Should().Be(0x9A, "Should use array10 format for 10 fields");
    }

    [Fact]
    public void EventEnvelope_NoFloatingPointFields()
    {
        var envelopeType = typeof(EventEnvelope);
        var properties = envelopeType.GetProperties();

        foreach (var prop in properties)
        {
            prop.PropertyType.Should().NotBe(typeof(float), $"{prop.Name} should not use float");
            prop.PropertyType.Should().NotBe(typeof(double), $"{prop.Name} should not use double");
            prop.PropertyType.Should().NotBe(typeof(decimal), $"{prop.Name} should not use decimal");
        }
    }

    [Fact]
    public void EventEnvelope_DeterministicSerialization()
    {
        var envelope = new EventEnvelope
        {
            VariantId = "v1",
            BranchId = "main",
            LLevel = 0,
            Domain = "test",
            Model = "m1",
            Tick = 100,
            EventType = "TestEvent",
            Payload = new byte[] { 1, 2, 3 },
            PreviousHash = new byte[] { 0, 0, 0, 0 },
            Hash = new byte[] { 1, 1, 1, 1 }
        };

        var serialized1 = new List<byte[]>();
        for (int i = 0; i < 100; i++)
        {
            serialized1.Add(MessagePackSerializer.Serialize(envelope));
        }

        // All serializations should be identical
        foreach (var bytes in serialized1.Skip(1))
        {
            bytes.Should().BeEquivalentTo(serialized1[0]);
        }
    }

    [Fact]
    public void HashPreimage_AlsoUsesCanonicalEncoding()
    {
        // This test uses reflection to verify the private HashPreimage type
        // also follows canonical encoding rules

        var hashUtilsType = typeof(HashUtils);
        var computeHashMethod = hashUtilsType.GetMethod("CreateCanonicalHashPreimage",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        if (computeHashMethod != null)
        {
            var envelope = new EventEnvelope
            {
                VariantId = "v1",
                BranchId = "main",
                LLevel = 0,
                Domain = "test",
                Model = "m1",
                Tick = 100,
                EventType = "TestEvent",
                Payload = new byte[] { 1, 2, 3 },
                PreviousHash = new byte[] { 0, 0, 0, 0 },
                Hash = new byte[] { 1, 1, 1, 1 }
            };

            var preimage = computeHashMethod.Invoke(null, new object[] { envelope });
            var bytes = MessagePackSerializer.Serialize(preimage!);

            // Should use array format, not map
            bytes[0].Should().NotBeInRange(0x80, 0x8F);
            bytes[0].Should().NotBe(0xDE);
            bytes[0].Should().NotBe(0xDF);
        }
    }
}
```

### 6.3 Hash-chain validation

**File**: `tests/Fantasim.Persistence.Tests/Acceptance/HashChainValidationTests.cs`

```csharp
public class HashChainValidationTests : IDisposable
{
    private readonly string _dbPath;
    private readonly RocksDbEventStore _store;

    public HashChainValidationTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"test-hashchain-{Guid.NewGuid():N}");
        _store = RocksDbEventStore.Open(_dbPath);
    }

    public void Dispose()
    {
        _store.Dispose();
        if (Directory.Exists(_dbPath))
        {
            Directory.Delete(_dbPath, true);
        }
    }

    [Fact]
    public async Task HashChain_GenesisEvent_EmptyPreviousHash()
    {
        var genesis = new EventEnvelope
        {
            VariantId = "v1",
            BranchId = "main",
            LLevel = 0,
            Domain = "test",
            Model = "m1",
            Tick = 0,
            EventType = "Genesis",
            Payload = new byte[] { },
            PreviousHash = new byte[0],
            Hash = new byte[32]
        };

        var seq = await _store.AppendAsync(genesis);

        var retrieved = await _store.GetAsync(new EventId(
            genesis.VariantId,
            genesis.BranchId,
            genesis.LLevel,
            genesis.Domain,
            genesis.Model,
            seq));

        retrieved.Should().NotBeNull();
        retrieved!.PreviousHash.Length.Should().Be(0);
        retrieved.Hash.Length.Should().Be(32);
    }

    [Fact]
    public async Task HashChain_SecondEvent_ReferencesPreviousHash()
    {
        var genesis = new EventEnvelope
        {
            VariantId = "v1",
            BranchId = "main",
            LLevel = 0,
            Domain = "test",
            Model = "m1",
            Tick = 0,
            EventType = "Genesis",
            Payload = new byte[] { },
            PreviousHash = new byte[0],
            Hash = new byte[32]
        };

        var seq1 = await _store.AppendAsync(genesis);

        // Get the actual hash from stored genesis
        var storedGenesis = await _store.GetAsync(new EventId(
            genesis.VariantId,
            genesis.BranchId,
            genesis.LLevel,
            genesis.Domain,
            genesis.Model,
            seq1));

        var event2 = new EventEnvelope
        {
            VariantId = "v1",
            BranchId = "main",
            LLevel = 0,
            Domain = "test",
            Model = "m1",
            Tick = 1,
            EventType = "Event2",
            Payload = new byte[] { 1 },
            PreviousHash = storedGenesis!.Hash.ToArray(),
            Hash = new byte[32]
        };

        var seq2 = await _store.AppendAsync(event2);

        var storedEvent2 = await _store.GetAsync(new EventId(
            event2.VariantId,
            event2.BranchId,
            event2.LLevel,
            event2.Domain,
            event2.Model,
            seq2));

        storedEvent2.Should().NotBeNull();
        storedEvent2!.PreviousHash.Span.SequenceEqual(storedGenesis.Hash.Span).Should().BeTrue();
    }

    [Fact]
    public async Task HashChain_BrokenLink_Rejected()
    {
        var genesis = new EventEnvelope
        {
            VariantId = "v1",
            BranchId = "main",
            LLevel = 0,
            Domain = "test",
            Model = "m1",
            Tick = 0,
            EventType = "Genesis",
            Payload = new byte[] { },
            PreviousHash = new byte[0],
            Hash = new byte[32]
        };

        await _store.AppendAsync(genesis);

        // Try to append with wrong previous hash
        var badEvent = new EventEnvelope
        {
            VariantId = "v1",
            BranchId = "main",
            LLevel = 0,
            Domain = "test",
            Model = "m1",
            Tick = 1,
            EventType = "BadEvent",
            Payload = new byte[] { 1 },
            PreviousHash = new byte[32], // Wrong hash (all zeros)
            Hash = new byte[32]
        };

        var act = async () => await _store.AppendAsync(badEvent);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Previous hash does not match stream head*");
    }

    [Fact]
    public async Task HashChain_FullChain_Validation()
    {
        var events = new List<EventEnvelope>();
        var previousHash = ReadOnlyMemory<byte>.Empty;

        for (int i = 0; i < 10; i++)
        {
            var evt = new EventEnvelope
            {
                VariantId = "v1",
                BranchId = "main",
                LLevel = 0,
                Domain = "test",
                Model = "m1",
                Tick = i,
                EventType = $"Event{i}",
                Payload = new byte[] { (byte)i },
                PreviousHash = previousHash,
                Hash = new byte[32]
            };

            var seq = await _store.AppendAsync(evt);
            events.Add(evt);

            // Get stored hash for next event
            var stored = await _store.GetAsync(new EventId(
                evt.VariantId,
                evt.BranchId,
                evt.LLevel,
                evt.Domain,
                evt.Model,
                seq));
            previousHash = stored!.Hash;
        }

        // Read back and validate entire chain
        var retrievedEvents = new List<EventEnvelope>();
        await foreach (var evt in _store.ReadStreamAsync("v1", "main", 0, "test", "m1"))
        {
            retrievedEvents.Add(evt);
        }

        retrievedEvents.Should().HaveCount(10);

        // Validate chain integrity
        for (int i = 1; i < retrievedEvents.Count; i++)
        {
            var current = retrievedEvents[i];
            var previous = retrievedEvents[i - 1];

            HashUtils.VerifyHashChain(current, previous).Should().BeTrue(
                $"Event {i} should link to event {i - 1}");
        }
    }
}
```

**Acceptance**:
- All acceptance tests pass
- Sequence keys are uint64 big-endian 8 bytes
- MessagePack encoding is deterministic (no maps, integer keys, no floats)
- Hash-chain validation enforces previousHash linkage

---

## Phase 7: Documentation and Cleanup

### 7.1 Update README

**Add section** to `README.md`:
```markdown
## Persistence

This project uses a DB-first persistence model with RocksDB and MessagePack:

- **EventStore**: Append-only event log with hash-chain validation
- **Storage**: RocksDB via modern-rocksdb
- **Encoding**: MessagePack canonical encoding (RFC-V2-0005)

### Running Tests

```powershell
dotnet test
```

### Design Decisions

- RocksDB column families separate concerns: events, stream_meta, snapshots, read_models
- Sequence keys use uint64 big-endian 8-byte encoding for lexicographic ordering
- Events use MessagePack with explicit integer keys for determinism
- Hash-chain validation ensures causal integrity
```

### 7.2 Verify build

**Command**:
```powershell
dotnet build
dotnet test
```

**Expected Output**:
- Build succeeds
- All tests pass

---

## Stop/Ask Human Decisions

### Decision 1: Project structure

**Question**: Should the persistence code be organized as:
1. Monolithic `Fantasim.Persistence` project
2. Split into `Fantasim.Core` (types/interfaces) and `Fantasim.Persistence` (implementation)

**Recommendation**: Start with monolithic `Fantasim.Persistence`. Split later if needed.

### Decision 2: Column family names

**Question**: Column family names in RFC-V2-0004 are:
- `events`
- `stream_meta`
- `snapshots`
- `read_models`

**Current plan**: Implements `events` and `stream_meta` only. Confirm this is acceptable for MVP.

**Recommendation**: Yes, snapshots and read_models are out of scope.

### Decision 3: Hash algorithm

**Question**: RFC-V2-0005 doesn't specify the hash algorithm. This plan uses SHA-256.

**Recommendation**: SHA-256 is appropriate. Confirm or specify alternative if needed.

### Decision 4: Stream isolation granularity

**Question**: RFC-V2-0004 specifies isolation by `(VariantId, BranchId, L, Domain, M)`. The plan implements this exactly.

**Question**: Should we add additional isolation options (e.g., per-entity streams)?

**Recommendation**: No, stick to RFC spec for MVP.

### Decision 5: Database path configuration

**Question**: Should database path be:
1. Configurable via app settings
2. Environment variable
3. Hardcoded to `./data/db`

**Recommendation**: Configurable via constructor parameter for MVP.

---

## Completion Criteria

Implementation is complete when:
- [ ] All tests pass (100% success rate)
- [ ] Sequence key encoding tests validate uint64 big-endian 8-byte ordering
- [ ] MessagePack determinism tests validate no maps, integer keys, no floats
- [ ] Hash-chain validation tests enforce previousHash linkage
- [ ] Code follows RFC-V2-0004 and RFC-V2-0005
- [ ] README includes persistence documentation
- [ ] `dotnet build` and `dotnet test` succeed

---

## Post-Implementation

After implementation is complete and verified:

1. **Commit changes** in worktree:
```powershell
git add .
git commit -m "feat: implement eventstore-mvp

- Add RocksDB event store with column families (events, stream_meta)
- Implement MessagePack canonical encoding (RFC-V2-0005)
- Implement uint64 big-endian sequence key encoding
- Add hash-chain validation for events
- Add comprehensive acceptance tests"
```

2. **Push and create PR**:
```powershell
git push -u origin spec/eventstore-mvp
```

3. **After PR merge, cleanup**:
```powershell
# In main worktree
task spec:done -- eventstore-mvp
```
