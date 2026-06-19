using SmackerSharp.Internal;
using Xunit;

namespace SmackerSharp.Tests;

public sealed class VideoDecoderTests
{
    [Fact]
    public void DecodeSolidBlockFillsFourByFourBlock()
    {
        Huffman16[] trees =
        {
            BuildAbsentTree(),
            BuildAbsentTree(),
            BuildAbsentTree(),
            BuildSingleValueTree(0x0703)
        };

        byte[] frame = new byte[16];
        VideoDecoder.Decode(Array.Empty<byte>(), trees, (byte)'2', 4, 4, frame);

        Assert.All(frame, value => Assert.Equal(7, value));
    }

    private static Huffman16 BuildAbsentTree()
    {
        BitReader reader = new(new byte[] { 0 });
        Huffman16 tree = new();
        tree.Build(ref reader, 0);
        return tree;
    }

    private static Huffman16 BuildSingleValueTree(ushort value)
    {
        BitPacker packer = new();
        packer.WriteBit(1);
        WriteSingleValueHuffman8(packer, (byte)(value & 0xFF));
        WriteSingleValueHuffman8(packer, (byte)(value >> 8));
        WriteCacheValues(packer);
        packer.WriteBit(0);
        packer.WriteBit(0);

        BitReader reader = new(packer.ToArray());
        Huffman16 tree = new();
        tree.Build(ref reader, 16);
        return tree;
    }

    private static void WriteSingleValueHuffman8(BitPacker packer, byte value)
    {
        packer.WriteBit(1);
        packer.WriteBit(0);
        packer.WriteByte(value);
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
