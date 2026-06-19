using System.IO;
using SmackerSharp;
using Xunit;

namespace SmackerSharp.Tests;

public sealed class SmackerReaderTests
{
    [Fact]
    public void OpenRejectsInvalidSignature()
    {
        byte[] data = new byte[24];

        InvalidDataException ex = Assert.Throws<InvalidDataException>(() => SmackerReader.Open(data));
        Assert.Contains("SMK", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void OpenReadsMinimalHeader()
    {
        byte[] data = new byte[110];
        data[0] = (byte)'S';
        data[1] = (byte)'M';
        data[2] = (byte)'K';
        data[3] = (byte)'4';
        WriteUInt32(data, 4, 320);
        WriteUInt32(data, 8, 200);
        WriteUInt32(data, 12, 1);
        WriteInt32(data, 16, 33);
        WriteUInt32(data, 52, 1);
        WriteUInt32(data, 104, 1);

        using SmackerReader reader = SmackerReader.Open(data);

        Assert.Equal(320u, reader.VideoInfo.Width);
        Assert.Equal(200u, reader.VideoInfo.Height);
        Assert.Equal(1u, reader.Info.FrameCount);
        Assert.Equal(33_000.0, reader.Info.MicrosecondsPerFrame);
        Assert.Equal(7, reader.AudioTracks.Count);
        Assert.True(reader.Container.Keyframes[0]);
    }

    [Fact]
    public void SetEnabledUpdatesVideoAndExistingAudioTracks()
    {
        byte[] data = new byte[110];
        data[0] = (byte)'S';
        data[1] = (byte)'M';
        data[2] = (byte)'K';
        data[3] = (byte)'4';
        WriteUInt32(data, 4, 320);
        WriteUInt32(data, 8, 200);
        WriteUInt32(data, 12, 1);
        WriteInt32(data, 16, 33);
        WriteUInt32(data, 52, 1);
        WriteUInt32(data, 72, 0x40002B11);
        WriteUInt32(data, 104, 1);

        using SmackerReader reader = SmackerReader.Open(data);

        Assert.Equal(SmackerTrackMask.AudioTrack0, reader.AudioTrackMask);

        reader.SetEnabled(SmackerTrackMask.Video | SmackerTrackMask.AudioTrack0 | SmackerTrackMask.AudioTrack1);

        Assert.True(reader.VideoEnabled);
        Assert.True(reader.IsAudioEnabled(0));
        Assert.False(reader.IsAudioEnabled(1));
    }

    [Fact]
    public void SampleFixtureDiscoveryDoesNotRequireLocalFiles()
    {
        string? helloPath = SampleFixtures.HelloSmkPath;
        if (helloPath is null)
        {
            return;
        }

        Assert.True(File.Exists(helloPath), $"Configured sample file does not exist: {helloPath}");
    }

    private static void WriteUInt32(byte[] data, int offset, uint value)
    {
        data[offset] = (byte)value;
        data[offset + 1] = (byte)(value >> 8);
        data[offset + 2] = (byte)(value >> 16);
        data[offset + 3] = (byte)(value >> 24);
    }

    private static void WriteInt32(byte[] data, int offset, int value)
    {
        WriteUInt32(data, offset, unchecked((uint)value));
    }
}
