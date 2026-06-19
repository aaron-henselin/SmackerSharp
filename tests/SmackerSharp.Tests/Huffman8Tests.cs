using System.IO;
using SmackerSharp.Internal;
using Xunit;

namespace SmackerSharp.Tests;

public sealed class Huffman8Tests
{
    [Fact]
    public void BuildAndLookupSimpleBranchTree()
    {
        byte[] treeData = BuildSimpleTreeBytes(0x12, 0x34);
        BitReader treeReader = new(treeData);
        Huffman8 tree = new();
        tree.Build(ref treeReader);

        BitReader leftLookup = new(PackBits(0));
        BitReader rightLookup = new(PackBits(1));

        Assert.Equal(0x12, tree.Lookup(ref leftLookup));
        Assert.Equal(0x34, tree.Lookup(ref rightLookup));
    }

    [Fact]
    public void BuildSupportsAbsentTree()
    {
        BitReader treeReader = new(PackBits(0, 0));
        Huffman8 tree = new();

        tree.Build(ref treeReader);

        BitReader lookup = new(PackBits(1));
        Assert.Equal(0, tree.Lookup(ref lookup));
    }

    [Fact]
    public void BuildRejectsInvalidFinalBit()
    {
        BitReader treeReader = new(PackBits(0, 1));
        Huffman8 tree = new();

        AssertBuildThrowsInvalidData(tree, ref treeReader);
    }

    private static byte[] BuildSimpleTreeBytes(byte left, byte right)
    {
        BitPacker packer = new();
        packer.WriteBit(1);
        packer.WriteBit(1);
        packer.WriteBit(0);
        packer.WriteByte(left);
        packer.WriteBit(0);
        packer.WriteByte(right);
        packer.WriteBit(0);
        return packer.ToArray();
    }

    private static byte[] PackBits(params int[] bits)
    {
        BitPacker packer = new();
        foreach (int bit in bits)
        {
            packer.WriteBit(bit);
        }

        return packer.ToArray();
    }

    private static void AssertBuildThrowsInvalidData(Huffman8 tree, ref BitReader reader)
    {
        try
        {
            tree.Build(ref reader);
        }
        catch (InvalidDataException)
        {
            return;
        }

        throw new InvalidOperationException($"Expected {nameof(InvalidDataException)}.");
    }

    private sealed class BitPacker
    {
        private readonly List<byte> _bytes = new();
        private int _bitOffset;

        public void WriteBit(int bit)
        {
            if (_bitOffset == 0)
            {
                _bytes.Add(0);
            }

            if (bit != 0)
            {
                _bytes[^1] |= (byte)(1 << _bitOffset);
            }

            _bitOffset = (_bitOffset + 1) & 7;
        }

        public void WriteByte(byte value)
        {
            for (int i = 0; i < 8; i++)
            {
                WriteBit((value >> i) & 1);
            }
        }

        public byte[] ToArray() => _bytes.ToArray();
    }
}
