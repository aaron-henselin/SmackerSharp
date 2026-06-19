using SmackerSharp.Internal;
using Xunit;

namespace SmackerSharp.Tests;

public sealed class AudioDecoderTests
{
    [Fact]
    public void DecodeRawCopiesPcmBytes()
    {
        byte[] destination = new byte[8];
        SmackerAudioTrackInfo info = new(true, 1, 8, 11025, SmackerAudioCompression.None);

        int size = AudioDecoder.Decode(new byte[] { 1, 2, 3 }, info, destination);

        Assert.Equal(3, size);
        Assert.Equal(new byte[] { 1, 2, 3 }, destination[..3]);
    }

    [Fact]
    public void DecodeDpcm8BitMonoAppliesSignedDeltas()
    {
        BitPacker packer = new();
        packer.WriteUInt32(4);
        packer.WriteBit(1);
        packer.WriteBit(0);
        packer.WriteBit(0);
        WriteSingleValueHuffman8(packer, 1);
        packer.WriteByte(10);

        byte[] destination = new byte[4];
        SmackerAudioTrackInfo info = new(true, 1, 8, 11025, SmackerAudioCompression.SmackerDpcm);

        int size = AudioDecoder.Decode(packer.ToArray(), info, destination);

        Assert.Equal(4, size);
        Assert.Equal(new byte[] { 10, 11, 12, 13 }, destination);
    }

    private static void WriteSingleValueHuffman8(BitPacker packer, byte value)
    {
        packer.WriteBit(1);
        packer.WriteBit(0);
        packer.WriteByte(value);
        packer.WriteBit(0);
    }

    private sealed class BitPacker
    {
        private readonly List<byte> _bytes = new();
        private int _bitOffset;

        public void WriteUInt32(uint value)
        {
            WriteByte((byte)value);
            WriteByte((byte)(value >> 8));
            WriteByte((byte)(value >> 16));
            WriteByte((byte)(value >> 24));
        }

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
