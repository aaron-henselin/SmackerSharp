using SmackerSharp;
using Xunit;

namespace SmackerSharp.Tests;

public sealed class SmackerWriterTests
{
    [Fact]
    public void WriterRoundTripsAllZeroFrameThroughManagedDecoder()
    {
        byte[] encoded = EncodeSingleBlackFrame();

        using SmackerReader reader = SmackerReader.Open(encoded);
        reader.VideoEnabled = true;

        Assert.Equal('2', reader.Version);
        Assert.Equal(4u, reader.VideoInfo.Width);
        Assert.Equal(4u, reader.VideoInfo.Height);
        Assert.Equal(1u, reader.Info.FrameCount);
        Assert.Equal(SmackerFrameResult.Last, reader.First());
        Assert.All(reader.VideoFrame8.ToArray(), value => Assert.Equal(0, value));
        Assert.Equal(256 * 3, reader.PaletteRgb.Length);
    }

    [Fact]
    public void WriterRoundTripsSolidNonZeroFrameThroughManagedDecoder()
    {
        using MemoryStream output = new();
        using SmackerWriter writer = new(output, new SmackerWriterOptions(4, 4, 100000));
        byte[] pixels = Enumerable.Repeat((byte)7, 16).ToArray();

        writer.AddFrame(pixels, BuildPalette());
        writer.Finish();

        using SmackerReader reader = SmackerReader.Open(output.ToArray());
        reader.VideoEnabled = true;

        Assert.Equal(SmackerFrameResult.Last, reader.First());
        Assert.All(reader.VideoFrame8.ToArray(), value => Assert.Equal(7, value));
    }

    [Fact]
    public void WriterRejectsNonSolidPixelsUntilBlockEncoderExists()
    {
        using MemoryStream output = new();
        using SmackerWriter writer = new(output, new SmackerWriterOptions(4, 4, 100000));
        byte[] pixels = new byte[16];
        pixels[1] = 1;

        Assert.Throws<NotSupportedException>(() => writer.AddFrame(pixels, BuildPalette()));
    }

    internal static byte[] EncodeSingleBlackFrame()
    {
        using MemoryStream output = new();
        using SmackerWriter writer = new(output, new SmackerWriterOptions(4, 4, 100000));
        writer.AddFrame(new byte[16], BuildPalette());
        writer.Finish();
        return output.ToArray();
    }

    private static byte[] BuildPalette()
    {
        byte[] palette = new byte[256 * 3];
        for (int i = 0; i < 256; i++)
        {
            palette[i * 3] = 0x00;
            palette[(i * 3) + 1] = 0x04;
            palette[(i * 3) + 2] = 0xFF;
        }

        return palette;
    }
}
