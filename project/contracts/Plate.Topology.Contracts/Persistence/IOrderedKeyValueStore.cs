using System;

namespace Plate.Topology.Contracts.Persistence;

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

    void Seek(byte[] target);

    void Next();
}
