using System;

namespace FantaSim.Geosphere.Plate.Topology.Contracts.Persistence;

public interface IOrderedKeyValueStore : IDisposable
{
    IOrderedKeyValueWriteBatch CreateWriteBatch();

    void Write(IOrderedKeyValueWriteBatch batch);

    void Put(byte[] key, byte[] value);

    bool TryGet(byte[] key, out byte[] value);

    bool TryGet(byte[] key, Span<byte> destination, out int bytesWritten);

    IOrderedKeyValueIterator CreateIterator();
}

public interface IOrderedKeyValueWriteBatch : IDisposable
{
    void Put(byte[] key, ReadOnlySpan<byte> value);
}

public interface IOrderedKeyValueIterator : IDisposable
{
    bool Valid { get; }

    ReadOnlyMemory<byte> Key { get; }

    ReadOnlyMemory<byte> Value { get; }

    /// <summary>
    /// Positions the iterator at the first key >= target.
    /// </summary>
    void Seek(byte[] target);

    /// <summary>
    /// Positions the iterator at the last key &lt;= target.
    /// This is the reverse of Seek and is useful for "latest before" queries.
    /// </summary>
    void SeekForPrev(byte[] target);

    void Next();

    /// <summary>
    /// Moves to the previous key.
    /// </summary>
    void Prev();
}
