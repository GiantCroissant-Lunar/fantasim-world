using System;
using System.Collections.Generic;
using Plate.Topology.Contracts.Persistence;

namespace Plate.Topology.Materializer;

public sealed class InMemoryOrderedKeyValueStore : IOrderedKeyValueStore
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

            if (x.Length < y.Length)
                return -1;
            if (x.Length > y.Length)
                return 1;
            return 0;
        }
    }

    private readonly SortedList<byte[], byte[]> _data = new(ByteSequenceComparer.Instance);

    public IOrderedKeyValueWriteBatch CreateWriteBatch() => new WriteBatchImpl();

    public void Write(IOrderedKeyValueWriteBatch batch)
    {
        ArgumentNullException.ThrowIfNull(batch);

        if (batch is not WriteBatchImpl b)
            throw new ArgumentException("Batch type not supported by this store.", nameof(batch));

        foreach (var op in b.Ops)
        {
            Put(op.Key, op.Value);
        }
    }

    public void Put(byte[] key, byte[] value)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);

        var keyCopy = (byte[])key.Clone();
        var valueCopy = (byte[])value.Clone();

        _data[keyCopy] = valueCopy;
    }

    public bool TryGet(byte[] key, out byte[] value)
    {
        ArgumentNullException.ThrowIfNull(key);

        if (_data.TryGetValue(key, out var found))
        {
            value = (byte[])found.Clone();
            return true;
        }

        value = Array.Empty<byte>();
        return false;
    }

    public bool TryGet(byte[] key, Span<byte> destination, out int bytesWritten)
    {
        ArgumentNullException.ThrowIfNull(key);

        if (!_data.TryGetValue(key, out var found))
        {
            bytesWritten = 0;
            return false;
        }

        if (found.Length > destination.Length)
            throw new ArgumentException("Destination buffer is too small.", nameof(destination));

        found.CopyTo(destination);
        bytesWritten = found.Length;
        return true;
    }

    public IOrderedKeyValueIterator CreateIterator() => new IteratorImpl(_data);

    public void Dispose()
    {
    }

    private sealed class WriteBatchImpl : IOrderedKeyValueWriteBatch
    {
        public readonly List<(byte[] Key, byte[] Value)> Ops = new();

        public void Put(byte[] key, ReadOnlySpan<byte> value)
        {
            ArgumentNullException.ThrowIfNull(key);

            var keyCopy = (byte[])key.Clone();
            var valueCopy = value.ToArray();

            Ops.Add((keyCopy, valueCopy));
        }

        public void Dispose()
        {
            Ops.Clear();
        }
    }

    private sealed class IteratorImpl : IOrderedKeyValueIterator
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

        public ReadOnlyMemory<byte> Key => Valid ? _keys[_index] : ReadOnlyMemory<byte>.Empty;

        public ReadOnlyMemory<byte> Value => Valid ? _values[_index] : ReadOnlyMemory<byte>.Empty;

        public void Seek(byte[] target)
        {
            ArgumentNullException.ThrowIfNull(target);

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

        public void Next()
        {
            if (_index < _keys.Count)
                _index++;
        }

        public void Dispose()
        {
        }
    }
}
