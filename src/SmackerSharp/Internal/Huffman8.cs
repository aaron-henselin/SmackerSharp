using System.IO;

namespace SmackerSharp.Internal;

internal sealed class Huffman8
{
    private const ushort Branch = 0x8000;
    private const ushort LeafMask = 0x7FFF;
    private readonly ushort[] _tree = new ushort[511];

    public int Size { get; private set; }

    public void Build(ref BitReader reader)
    {
        int initialBit = reader.ReadBit();
        Size = 0;

        if (initialBit != 0)
        {
            BuildRecursive(ref reader);
        }
        else
        {
            _tree[0] = 0;
        }

        int finalBit = reader.ReadBit();
        if (finalBit != 0)
        {
            throw new InvalidDataException("Huffman8 tree ended with an invalid final bit.");
        }
    }

    public int Lookup(ref BitReader reader)
    {
        int index = 0;
        while ((_tree[index] & Branch) != 0)
        {
            int bit = reader.ReadBit();
            index = bit != 0 ? _tree[index] & LeafMask : index + 1;
        }

        return _tree[index];
    }

    private void BuildRecursive(ref BitReader reader)
    {
        if (Size >= _tree.Length)
        {
            throw new InvalidDataException("Huffman8 tree exceeded the maximum node count.");
        }

        int bit = reader.ReadBit();
        if (bit != 0)
        {
            int branchIndex = Size++;
            BuildRecursive(ref reader);
            _tree[branchIndex] = (ushort)(Branch | Size);
            BuildRecursive(ref reader);
        }
        else
        {
            _tree[Size++] = (ushort)reader.ReadByte();
        }
    }
}
