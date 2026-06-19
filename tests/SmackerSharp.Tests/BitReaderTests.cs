using System.IO;
using SmackerSharp.Internal;
using Xunit;

namespace SmackerSharp.Tests;

public sealed class BitReaderTests
{
    [Fact]
    public void ReadBitUsesLeastSignificantBitFirst()
    {
        BitReader reader = new(new byte[] { 0b1001_0110 });

        Assert.Equal(0, reader.ReadBit());
        Assert.Equal(1, reader.ReadBit());
        Assert.Equal(1, reader.ReadBit());
        Assert.Equal(0, reader.ReadBit());
        Assert.Equal(1, reader.ReadBit());
        Assert.Equal(0, reader.ReadBit());
        Assert.Equal(0, reader.ReadBit());
        Assert.Equal(1, reader.ReadBit());
    }

    [Fact]
    public void ReadByteReadsAlignedBytes()
    {
        BitReader reader = new(new byte[] { 0x12, 0x34 });

        Assert.Equal(0x12, reader.ReadByte());
        Assert.Equal(0x34, reader.ReadByte());
    }

    [Fact]
    public void ReadByteMatchesSmackerSharpUnalignedRead()
    {
        BitReader reader = new(new byte[] { 0b1010_1101, 0b0110_0011 });

        Assert.Equal(1, reader.ReadBit());
        Assert.Equal(0, reader.ReadBit());
        Assert.Equal(0b1110_1011, reader.ReadByte());
    }

    [Fact]
    public void ReadBitThrowsWhenExhausted()
    {
        BitReader reader = new(new byte[] { 0x00 });

        for (int i = 0; i < 8; i++)
        {
            reader.ReadBit();
        }

        AssertReadBitThrowsInvalidData(ref reader);
    }

    [Fact]
    public void ReadByteThrowsWhenUnalignedReadWouldPassEnd()
    {
        BitReader reader = new(new byte[] { 0xFF });
        reader.ReadBit();

        AssertReadByteThrowsInvalidData(ref reader);
    }

    private static void AssertReadBitThrowsInvalidData(ref BitReader reader)
    {
        try
        {
            reader.ReadBit();
        }
        catch (InvalidDataException)
        {
            return;
        }

        throw new InvalidOperationException($"Expected {nameof(InvalidDataException)}.");
    }

    private static void AssertReadByteThrowsInvalidData(ref BitReader reader)
    {
        try
        {
            reader.ReadByte();
        }
        catch (InvalidDataException)
        {
            return;
        }

        throw new InvalidOperationException($"Expected {nameof(InvalidDataException)}.");
    }
}
