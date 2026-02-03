using System.Buffers;
using System.Collections;
using Tenray.ZoneTree;
using Tenray.ZoneTree.Core;
using Tenray.ZoneTree.Options;
using Tenray.ZoneTree.Comparers;
using UnifyStorage.Abstractions;

namespace FantaSim.Geosphere.Plate.PluginSmokeHost;

/// <summary>
/// Simple ZoneTree-based IKeyValueStore implementation.
/// Uses Tenray.ZoneTree directly to avoid unify-storage build issues.
/// Compatible with ZoneTree 1.8.x API.
/// </summary>
public sealed class ZoneTreeKeyValueStore : IKeyValueStore
{
    private readonly IZoneTree<byte[], byte[]> _zoneTree;
    private readonly string _dataPath;
    private bool _disposed;

    public ZoneTreeKeyValueStore(string dataPath)
    {
        _dataPath = dataPath;
        Directory.CreateDirectory(dataPath);

        _zoneTree = new ZoneTreeFactory<byte[], byte[]>()
            .SetDataDirectory(dataPath)
            .SetWriteAheadLogDirectory(dataPath)
            .SetIsDeletedDelegate((in byte[] key, in byte[] value) => value.Length == 0)
            .SetMarkValueDeletedDelegate((ref byte[] value) => value = Array.Empty<byte>())
            .SetComparer(new ByteArrayComparer())
            .OpenOrCreate();
    }

    public bool TryGet(ReadOnlySpan<byte> key, Span<byte> buffer, out int written)
    {
        ThrowIfDisposed();

        var keyArray = key.ToArray();
        var found = _zoneTree.TryGet(keyArray, out var value);

        if (!found || value is null)
        {
            written = 0;
            return false;
        }

        if (value.Length > buffer.Length)
        {
            written = value.Length;
            return false;
        }

        value.CopyTo(buffer);
        written = value.Length;
        return true;
    }

    public void Put(ReadOnlySpan<byte> key, ReadOnlySpan<byte> value)
    {
        ThrowIfDisposed();
        _zoneTree.Upsert(key.ToArray(), value.ToArray());
    }

    public void Delete(ReadOnlySpan<byte> key)
    {
        ThrowIfDisposed();
        // ZoneTree 1.8.x uses deletion markers - empty array indicates deletion
        _zoneTree.Upsert(key.ToArray(), Array.Empty<byte>());
    }

    public IKeyValueIterator CreateIterator()
    {
        ThrowIfDisposed();
        return new ZoneTreeIterator(_zoneTree.CreateIterator());
    }

    public IWriteBatch CreateWriteBatch()
    {
        return new WriteBatchImpl();
    }

    public void Write(IWriteBatch batch)
    {
        ThrowIfDisposed();

        if (batch is not WriteBatchImpl writeBatch)
            throw new ArgumentException("Invalid batch type", nameof(batch));

        // Apply operations directly using ZoneTree 1.8.x API
        foreach (var op in writeBatch.Operations)
        {
            if (op.IsDelete)
                _zoneTree.Upsert(op.Key, Array.Empty<byte>());
            else
                _zoneTree.Upsert(op.Key, op.Value!);
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _zoneTree?.Dispose();
            _disposed = true;
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(ZoneTreeKeyValueStore));
    }

    /// <summary>
    /// Byte array comparer for ordered storage using IRefComparer for ZoneTree 1.8.x.
    /// </summary>
    private struct ByteArrayComparer : IRefComparer<byte[]>
    {
        public int Compare(in byte[] x, in byte[] y)
        {
            // Use temporary variables since 'in' parameters can't be used directly with null checks
            var x1 = x;
            var y1 = y;
            if (ReferenceEquals(x1, y1)) return 0;
            if (x1 is null) return -1;
            if (y1 is null) return 1;

            var minLength = Math.Min(x1.Length, y1.Length);
            for (var i = 0; i < minLength; i++)
            {
                var cmp = x1[i].CompareTo(y1[i]);
                if (cmp != 0) return cmp;
            }

            return x1.Length.CompareTo(y1.Length);
        }
    }

    /// <summary>
    /// Iterator wrapper for ZoneTree.
    /// </summary>
    private class ZoneTreeIterator : IKeyValueIterator
    {
        private readonly IZoneTreeIterator<byte[], byte[]> _iterator;
        private bool _disposed;

        public ZoneTreeIterator(IZoneTreeIterator<byte[], byte[]> iterator)
        {
            _iterator = iterator;
        }

        public bool Valid => !_disposed && _iterator.CurrentKey is not null;

        public ReadOnlySpan<byte> Key => _iterator.CurrentKey ?? ReadOnlySpan<byte>.Empty;

        public ReadOnlySpan<byte> Value => _iterator.CurrentValue ?? ReadOnlySpan<byte>.Empty;

        public void Seek(ReadOnlySpan<byte> key)
        {
            _iterator.Refresh();
            _iterator.Seek(key.ToArray());
        }

        public void SeekForPrev(ReadOnlySpan<byte> key)
        {
            // For SeekForPrev, we seek forward first
            _iterator.Refresh();
            var keyArray = key.ToArray();
            _iterator.Seek(keyArray);
            
            if (_iterator.CurrentKey is not null)
            {
                var comparer = new ByteArrayComparer();
                // Compare without 'in' since CurrentKey is a property
                if (comparer.Compare(_iterator.CurrentKey, keyArray) > 0)
                {
                    // We're past the target, but ZoneTree 1.8.x doesn't have MovePrevious
                    // This is a limitation - full SeekForPrev would require a reverse iterator
                }
            }
        }

        public void Next()
        {
            _iterator.Next();
        }

        public void Prev()
        {
            // ZoneTree 1.8.x doesn't support MovePrevious on forward iterators
            throw new NotSupportedException("Prev() is not supported in ZoneTree 1.8.x forward iterator. Use reverse iteration instead.");
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _iterator?.Dispose();
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Write batch implementation.
    /// </summary>
    private class WriteBatchImpl : IWriteBatch
    {
        public List<(byte[] Key, byte[]? Value, bool IsDelete)> Operations { get; } = new();

        public void Put(ReadOnlySpan<byte> key, ReadOnlySpan<byte> value)
        {
            Operations.Add((key.ToArray(), value.ToArray(), false));
        }

        public void Delete(ReadOnlySpan<byte> key)
        {
            Operations.Add((key.ToArray(), null, true));
        }

        public void Clear()
        {
            Operations.Clear();
        }

        public void Dispose()
        {
            Clear();
        }
    }
}
