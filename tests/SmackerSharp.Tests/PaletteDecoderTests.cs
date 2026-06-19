using System.IO;
using SmackerSharp.Internal;
using Xunit;

namespace SmackerSharp.Tests;

public sealed class PaletteDecoderTests
{
    [Fact]
    public void DecodeDirectPaletteExpandsSixBitComponents()
    {
        byte[] encoded = new byte[256 * 3];
        for (int i = 0; i < 256; i++)
        {
            encoded[i * 3] = 0;
            encoded[(i * 3) + 1] = 1;
            encoded[(i * 3) + 2] = 63;
        }

        byte[] palette = new byte[256 * 3];
        PaletteDecoder.Decode(encoded, palette);

        Assert.Equal(0x00, palette[0]);
        Assert.Equal(0x04, palette[1]);
        Assert.Equal(0xFF, palette[2]);
        Assert.Equal(0x00, palette[^3]);
        Assert.Equal(0x04, palette[^2]);
        Assert.Equal(0xFF, palette[^1]);
    }

    [Fact]
    public void DecodeSkipBlocksPreservePreviousPalette()
    {
        byte[] palette = Enumerable.Range(0, 256 * 3).Select(i => (byte)(i & 0xFF)).ToArray();
        byte[] encoded = { 0xFF, 0xFF };

        PaletteDecoder.Decode(encoded, palette);

        Assert.Equal((byte)0, palette[0]);
        Assert.Equal((byte)255, palette[255]);
        Assert.Equal((byte)253, palette[^3]);
        Assert.Equal((byte)254, palette[^2]);
        Assert.Equal((byte)255, palette[^1]);
    }

    [Fact]
    public void DecodeColorShiftCopiesFromPreviousPalette()
    {
        byte[] palette = new byte[256 * 3];
        palette[10 * 3] = 0x11;
        palette[(10 * 3) + 1] = 0x22;
        palette[(10 * 3) + 2] = 0x33;

        byte[] encoded = new byte[1 + 1 + 255 + 255];
        encoded[0] = 0x40;
        encoded[1] = 10;
        encoded[2] = 0xFE;
        encoded[3] = 0xFF;

        PaletteDecoder.Decode(encoded.AsSpan(0, 4), palette);

        Assert.Equal(0x11, palette[0]);
        Assert.Equal(0x22, palette[1]);
        Assert.Equal(0x33, palette[2]);
    }

    [Fact]
    public void DecodeRejectsShortPalette()
    {
        byte[] palette = new byte[256 * 3];

        Assert.Throws<InvalidDataException>(() => PaletteDecoder.Decode(new byte[] { 0, 1 }, palette));
    }

    [Fact]
    public void DecodeRejectsOutOfRangeComponent()
    {
        byte[] palette = new byte[256 * 3];

        Assert.Throws<InvalidDataException>(() => PaletteDecoder.Decode(new byte[] { 0, 0x40, 0 }, palette));
    }
}
