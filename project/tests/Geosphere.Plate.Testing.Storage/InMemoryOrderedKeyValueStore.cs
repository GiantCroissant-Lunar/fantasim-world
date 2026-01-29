using System;
using System.Collections.Generic;
using UnifyStorage.Abstractions;

namespace FantaSim.Geosphere.Plate.Testing.Storage;

/// <summary>
/// Simple in-memory ordered KV store for tests.
/// Implements <see cref="IKeyValueStore"/> for UnifyStorage-powered stores.
/// </summary>
public sealed class InMemoryOrderedKeyValueStore : IKeyValueStore
{
    private sealed class ByteSequenceComparer : IComparer<byte[]>
    {
        public static readonly ByteSequenceComparer Instance = new();

        public int Compare(byte[]? x, byte[]? y)
        {
            if (ReferenceEquals(x, y))
                return 0;
            if (x is null)
                return -1;
            if (y is null)
                return 1;

            var min = Math.Min(x.Length, y.Length);
            for (var i = 0; i < min; i++)
            {
                var xb = x[i];
                var yb = y[i];
                if (xb < yb)
                    return -1;
                if (xb > yb)
                    return 1;
            }

            return x.Length.CompareTo(y.Length);
        }
    }

    private readonly SortedList<byte[], byte[]> _data = new(ByteSequenceComparer.Instance);

    public IWriteBatch CreateWriteBatch() => new WriteBatchImpl();

    public void Write(IWriteBatch batch)
    {
        ArgumentNullException.ThrowIfNull(batch);

        if (batch is not WriteBatchImpl b)
            throw new ArgumentException("Batch type not supported by this store.", nameof(batch));

        foreach (var op in b.Ops)
        {
            if (op.IsDelete)
                Delete(op.Key);
            else
                Put(op.Key, op.Value!);
        }
    }

    public void Put(ReadOnlySpan<byte> key, ReadOnlySpan<byte> value)
    {
        if (key.IsEmpty) throw new ArgumentException("Key cannot be empty.", nameof(key));

        _data[key.ToArray()] = value.ToArray();
    }

    public void Delete(ReadOnlySpan<byte> key)
    {
        if (key.IsEmpty) throw new ArgumentException("Key cannot be empty.", nameof(key));

        _data.Remove(key.ToArray());
    }

    public bool TryGet(ReadOnlySpan<byte> key, Span<byte> buffer, out int written)
    {
        if (key.IsEmpty) throw new ArgumentException("Key cannot be empty.", nameof(key));

        if (_data.TryGetValue(key.ToArray(), out var found))
        {
            if (found.Length > buffer.Length)
            {
                written = found.Length;
                return false;
            }

            found.CopyTo(buffer);
            written = found.Length;
            return true;
        }

        written = 0;
        return false;
    }

    public bool TryGet(byte[] key, out byte[]? value)
    {
        if (_data.TryGetValue(key, out var v))
        {
            value = v;
            return true;
        }

        value = null;
        return false;
    }

    public IKeyValueIterator CreateIterator() => new IteratorImpl(_data);

    public void Dispose()
    {
    }

    private sealed class WriteBatchImpl : IWriteBatch
    {
        public readonly List<(byte[] Key, byte[]? Value, bool IsDelete)> Ops = new();

        public void Put(ReadOnlySpan<byte> key, ReadOnlySpan<byte> value)
        {
            if (key.IsEmpty) throw new ArgumentException("Key cannot be empty.", nameof(key));

            Ops.Add((key.ToArray(), value.ToArray(), false));
        }

        public void Delete(ReadOnlySpan<byte> key)
        {
            if (key.IsEmpty) throw new ArgumentException("Key cannot be empty.", nameof(key));

            Ops.Add((key.ToArray(), null, true));
        }

        public void Clear() => Ops.Clear();

        public void Dispose() => Ops.Clear();
    }

    private sealed class IteratorImpl : IKeyValueIterator
    {
        private readonly List<byte[]> _keys;
        private readonly List<byte[]> _values;
        private readonly IComparer<byte[]> _comparer;
        private int _index;

        public IteratorImpl(SortedList<byte[], byte[]> data)
        {
            _comparer = data.Comparer;
            _keys = new List<byte[]>(data.Keys);
            _values = new List<byte[]>(data.Values);
            _index = _keys.Count;
        }

        public bool Valid => _index >= 0 && _index < _keys.Count;

        public ReadOnlySpan<byte> Key => Valid ? _keys[_index] : ReadOnlySpan<byte>.Empty;

        public ReadOnlySpan<byte> Value => Valid ? _values[_index] : ReadOnlySpan<byte>.Empty;

        public void Seek(ReadOnlySpan<byte> key)
        {
            if (key.IsEmpty) throw new ArgumentException("Key cannot be empty.", nameof(key));

            var target = key.ToArray();

            var lo = 0;
            var hi = _keys.Count;
            while (lo < hi)
            {
                var mid = lo + ((hi - lo) / 2);
                var cmp = _comparer.Compare(_keys[mid], target);
                if (cmp < 0)
                    lo = mid + 1;
                else
                    hi = mid;
            }

            _index = lo;
        }

        public void SeekForPrev(ReadOnlySpan<byte> key)
        {
            if (key.IsEmpty) throw new ArgumentException("Key cannot be empty.", nameof(key));

            var target = key.ToArray();

            var lo = 0;
            var hi = _keys.Count;
            while (lo < hi)
            {
                var mid = lo + ((hi - lo) / 2);
                var cmp = _comparer.Compare(_keys[mid], target);
                if (cmp <= 0)
                    lo = mid + 1;
                else
                    hi = mid;
            }

            _index = lo - 1;
        }

        public void Next()
        {
            if (_index < _keys.Count)
                _index++;
        }

        public void Prev()
        {
            if (_index >= 0)
                _index--;
        }

        public void Dispose()
        {
        }
    }
}
