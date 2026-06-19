using System.IO;

namespace SmackerSharp.Internal;

internal sealed class Huffman16
{
    private const uint Branch = 0x80000000;
    private const uint Cache = 0x40000000;
    private const uint LeafMask = 0x3FFFFFFF;

    private uint[] _tree = Array.Empty<uint>();
    private readonly ushort[] _cache = new ushort[3];

    public int Size { get; private set; }

    public void Build(ref BitReader reader, uint allocationSize)
    {
        int initialBit = reader.ReadBit();
        Size = 0;

        if (initialBit != 0)
        {
            Huffman8 low8 = new();
            low8.Build(ref reader);

            Huffman8 high8 = new();
            high8.Build(ref reader);

            for (int i = 0; i < _cache.Length; i++)
            {
                int low = reader.ReadByte();
                int high = reader.ReadByte();
                _cache[i] = (ushort)(low | (high << 8));
            }

            if (allocationSize < 12 || allocationSize % 4 != 0)
            {
                throw new InvalidDataException($"Invalid Huffman16 allocation size {allocationSize}.");
            }

            int limit = checked((int)((allocationSize - 12) / 4));
            _tree = new uint[limit];
            BuildRecursive(ref reader, low8, high8, limit);

            if (Size != limit)
            {
                throw new InvalidDataException("Huffman16 tree did not fill the expected node count.");
            }
        }
        else
        {
            _tree = new uint[] { 0 };
        }

        int finalBit = reader.ReadBit();
        if (finalBit != 0)
        {
            throw new InvalidDataException("Huffman16 tree ended with an invalid final bit.");
        }
    }

    public void ResetCache()
    {
        Array.Clear(_cache);
    }

    public int Lookup(ref BitReader reader)
    {
        int index = 0;
        while ((_tree[index] & Branch) != 0)
        {
            int bit = reader.ReadBit();
            index = bit != 0 ? checked((int)(_tree[index] & LeafMask)) : index + 1;
        }

        uint value = _tree[index];
        if ((value & Cache) != 0)
        {
            value = _cache[value & LeafMask];
        }

        if (_cache[0] != value)
        {
            _cache[2] = _cache[1];
            _cache[1] = _cache[0];
            _cache[0] = (ushort)value;
        }

        return (int)value;
    }

    private void BuildRecursive(ref BitReader reader, Huffman8 low8, Huffman8 high8, int limit)
    {
        if (Size >= limit)
        {
            throw new InvalidDataException("Huffman16 tree exceeded the expected node count.");
        }

        int bit = reader.ReadBit();
        if (bit != 0)
        {
            int branchIndex = Size++;
            BuildRecursive(ref reader, low8, high8, limit);
            _tree[branchIndex] = Branch | (uint)Size;
            BuildRecursive(ref reader, low8, high8, limit);
            return;
        }

        int low = low8.Lookup(ref reader);
        int high = high8.Lookup(ref reader);
        uint value = (uint)(low | (high << 8));

        if (value == _cache[0])
        {
            value = Cache;
        }
        else if (value == _cache[1])
        {
            value = Cache | 1;
        }
        else if (value == _cache[2])
        {
            value = Cache | 2;
        }

        _tree[Size++] = value;
    }
}
