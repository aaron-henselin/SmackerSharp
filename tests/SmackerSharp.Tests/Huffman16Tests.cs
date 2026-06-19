using System.IO;
using SmackerSharp.Internal;
using Xunit;

namespace SmackerSharp.Tests;

public sealed class Huffman16Tests
{
    [Fact]
    public void BuildAndLookupSingleLeafTree()
    {
        byte[] treeData = BuildSingleLeafTreeBytes(low: 0x34, high: 0x12);
        BitReader treeReader = new(treeData);
        Huffman16 tree = new();

        tree.Build(ref treeReader, allocationSize: 16);

        BitReader lookupReader = new(Array.Empty<byte>());
        Assert.Equal(0x1234, tree.Lookup(ref lookupReader));
    }

    [Fact]
    public void LookupUpdatesRecentValueCache()
    {
        byte[] treeData = BuildTwoLeafTreeBytes(firstLow: 0x34, firstHigh: 0x12, secondLow: 0x78, secondHigh: 0x56);
        BitReader treeReader = new(treeData);
        Huffman16 tree = new();
        tree.Build(ref treeReader, allocationSize: 24);

        BitReader lookupReader = new(PackBits(0, 1, 0));

        Assert.Equal(0x1234, tree.Lookup(ref lookupReader));
        Assert.Equal(0x5678, tree.Lookup(ref lookupReader));
        Assert.Equal(0x1234, tree.Lookup(ref lookupReader));
    }

    [Fact]
    public void BuildRejectsInvalidAllocationSize()
    {
        byte[] treeData = BuildSingleLeafTreeBytes(low: 0x34, high: 0x12);
        BitReader treeReader = new(treeData);
        Huffman16 tree = new();

        AssertBuildThrowsInvalidData(tree, ref treeReader, allocationSize: 15);
    }

    private static byte[] BuildSingleLeafTreeBytes(byte low, byte high)
    {
        BitPacker packer = new();
        packer.WriteBit(1);
        WriteSingleValueHuffman8(packer, low);
        WriteSingleValueHuffman8(packer, high);
        WriteCacheValues(packer);
        packer.WriteBit(0);
        packer.WriteBit(0);
        return packer.ToArray();
    }

    private static byte[] BuildTwoLeafTreeBytes(byte firstLow, byte firstHigh, byte secondLow, byte secondHigh)
    {
        BitPacker packer = new();
        packer.WriteBit(1);
        WriteTwoValueHuffman8(packer, firstLow, secondLow);
        WriteTwoValueHuffman8(packer, firstHigh, secondHigh);
        WriteCacheValues(packer);
        packer.WriteBit(1);
        packer.WriteBit(0);
        packer.WriteBit(0);
        packer.WriteBit(0);
        packer.WriteBit(0);
        packer.WriteBit(1);
        packer.WriteBit(1);
        packer.WriteBit(0);
        return packer.ToArray();
    }

    private static void WriteSingleValueHuffman8(BitPacker packer, byte value)
    {
        packer.WriteBit(1);
        packer.WriteBit(0);
        packer.WriteByte(value);
        packer.WriteBit(0);
    }

    private static void WriteTwoValueHuffman8(BitPacker packer, byte left, byte right)
    {
        packer.WriteBit(1);
        packer.WriteBit(1);
        packer.WriteBit(0);
        packer.WriteByte(left);
        packer.WriteBit(0);
        packer.WriteByte(right);
        packer.WriteBit(0);
    }

    private static void WriteCacheValues(BitPacker packer)
    {
        packer.WriteByte(0xAA);
        packer.WriteByte(0xAA);
        packer.WriteByte(0xBB);
        packer.WriteByte(0xBB);
        packer.WriteByte(0xCC);
        packer.WriteByte(0xCC);
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

    private static void AssertBuildThrowsInvalidData(Huffman16 tree, ref BitReader reader, uint allocationSize)
    {
        try
        {
            tree.Build(ref reader, allocationSize);
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
