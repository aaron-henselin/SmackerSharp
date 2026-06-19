using SmackerSharp;
using System.Security.Cryptography;
using Xunit;

namespace SmackerSharp.Tests;

public sealed class SampleFileTests
{
    [Fact]
    public void HelloSmkOpensAndReportsKnownHeaderMetadata()
    {
        string? path = SampleFixtures.HelloSmkPath;
        if (path is null)
        {
            return;
        }

        using SmackerReader reader = SmackerReader.Open(path);

        Assert.Equal(320u, reader.VideoInfo.Width);
        Assert.Equal(240u, reader.VideoInfo.Height);
        Assert.Equal(1018u, reader.Info.FrameCount);
        Assert.Equal(66660.0, reader.Info.MicrosecondsPerFrame);
        Assert.Equal((byte)'2', reader.Container.Version);
        Assert.False(reader.Container.HasRingFrame);
        Assert.Equal(1018, reader.Container.StoredFrameCount);
        Assert.Equal(293302u, reader.Container.HuffmanTreeChunkSize);
        Assert.Equal(21232u, reader.Container.ChunkSizes[0]);
        Assert.False(reader.Container.Keyframes[0]);
        Assert.Equal(0x03, reader.Container.FrameTypes[0]);
    }

    [Fact]
    public void HelloSmkReportsKnownAudioMetadata()
    {
        string? path = SampleFixtures.HelloSmkPath;
        if (path is null)
        {
            return;
        }

        using SmackerReader reader = SmackerReader.Open(path);

        SmackerAudioTrackInfo track0 = reader.AudioTracks[0];
        Assert.True(track0.Exists);
        Assert.Equal(1, track0.Channels);
        Assert.Equal(8, track0.BitDepth);
        Assert.Equal(11025u, track0.Rate);
        Assert.Equal(SmackerAudioCompression.SmackerDpcm, track0.Compression);

        for (int i = 1; i < reader.AudioTracks.Count; i++)
        {
            Assert.False(reader.AudioTracks[i].Exists);
        }
    }

    [Fact]
    public void ConfiguredSampleFilesOpen()
    {
        foreach (string path in SampleFixtures.SampleFiles)
        {
            using SmackerReader reader = SmackerReader.Open(path);

            Assert.True(reader.VideoInfo.Width > 0, $"{path} should report a positive width.");
            Assert.True(reader.VideoInfo.Height > 0, $"{path} should report a positive height.");
            Assert.True(reader.Info.FrameCount > 0, $"{path} should report at least one frame.");
        }
    }

    [Fact]
    public void HelloSmkDecodesFirstVideoFrame()
    {
        string? path = SampleFixtures.HelloSmkPath;
        if (path is null)
        {
            return;
        }

        using SmackerReader reader = SmackerReader.Open(path);
        reader.VideoEnabled = true;

        SmackerFrameResult result = reader.First();

        Assert.Equal(SmackerFrameResult.More, result);
        Assert.Equal(320 * 240, reader.VideoFrame8.Length);
        Assert.Equal(256 * 3, reader.PaletteRgb.Length);
        Assert.Contains(reader.VideoFrame8.ToArray(), value => value != 0);
        Assert.Contains(reader.PaletteRgb.ToArray(), value => value != 0);
    }

    [Fact]
    public void HelloSmkDecodesFirstAudioChunk()
    {
        string? path = SampleFixtures.HelloSmkPath;
        if (path is null)
        {
            return;
        }

        using SmackerReader reader = SmackerReader.Open(path);
        reader.SetAudioEnabled(0, true);

        SmackerFrameResult result = reader.First();
        ReadOnlySpan<byte> audio = reader.GetAudio(0);

        Assert.Equal(SmackerFrameResult.More, result);
        Assert.True(audio.Length > 0);
        Assert.Contains(audio.ToArray(), value => value != 0);
    }

    [Fact]
    public void HelloSmkDecodesFirstSeveralVideoFrames()
    {
        string? path = SampleFixtures.HelloSmkPath;
        if (path is null)
        {
            return;
        }

        using SmackerReader reader = SmackerReader.Open(path);
        reader.VideoEnabled = true;

        Assert.Equal(SmackerFrameResult.More, reader.First());
        Assert.Equal(0u, reader.Info.CurrentFrame);

        for (uint expectedFrame = 1; expectedFrame <= 5; expectedFrame++)
        {
            Assert.Equal(SmackerFrameResult.More, reader.Next());
            Assert.Equal(expectedFrame, reader.Info.CurrentFrame);
            Assert.Equal(320 * 240, reader.VideoFrame8.Length);
        }
    }

    [Fact]
    public void HelloSmkIteratesAllFramesWithVideoAndAudioEnabled()
    {
        string? path = SampleFixtures.HelloSmkPath;
        if (path is null)
        {
            return;
        }

        using SmackerReader reader = SmackerReader.Open(path);
        reader.VideoEnabled = true;
        reader.SetAudioEnabled(0, true);

        SmackerFrameResult result = reader.First();
        uint decodedFrames = 1;

        while (result == SmackerFrameResult.More)
        {
            result = reader.Next();
            if (result != SmackerFrameResult.Done)
            {
                decodedFrames++;
            }
        }

        Assert.Equal(SmackerFrameResult.Last, result);
        Assert.Equal(reader.Info.FrameCount, decodedFrames);
        Assert.Equal(reader.Info.FrameCount - 1, reader.Info.CurrentFrame);
        Assert.Equal(SmackerFrameResult.Done, reader.Next());
    }

    [Theory]
    [InlineData(0u,
        "2bc4b273fa98f3b834d02bdab4b9140464e5f81fa568804f82ba11d908fe6408",
        "fd2834c0129789a30248b4565f0696e2ddda871c771c2fa7c534e9c42c714e57",
        "f42a62f142d238420e4c46e347bc92063f96d7fc0831c4396498ebad5332f7d7",
        11760)]
    [InlineData(1u,
        "2bc4b273fa98f3b834d02bdab4b9140464e5f81fa568804f82ba11d908fe6408",
        "ebe2b2618d2d1be54c4995c4cd7a6ca90e19fc71edc76be6981df35fea709669",
        "53c6cf9158fd407b247feed891843a6d57f7732265c98f382dc6631db4631a67",
        736)]
    [InlineData(5u,
        "2bc4b273fa98f3b834d02bdab4b9140464e5f81fa568804f82ba11d908fe6408",
        "25fa7e0fc5623f595e6d642d29e845e24e1a29be314af316f216ed910c73b2e6",
        "db023e8d90b91690bbc91b85adb03787cde07009e768aa4d4f0b99bfbe55fd48",
        736)]
    public void HelloSmkSelectedFrameHashesStayStable(
        uint frame,
        string expectedPaletteHash,
        string expectedVideoHash,
        string expectedAudioHash,
        int expectedAudioBytes)
    {
        string? path = SampleFixtures.HelloSmkPath;
        if (path is null)
        {
            return;
        }

        using SmackerReader reader = SmackerReader.Open(path);
        reader.VideoEnabled = true;
        reader.SetAudioEnabled(0, true);

        reader.SeekKeyframe(frame);
        while (reader.Info.CurrentFrame < frame)
        {
            Assert.NotEqual(SmackerFrameResult.Done, reader.Next());
        }

        ReadOnlySpan<byte> audio = reader.GetAudio(0);

        Assert.Equal(expectedPaletteHash, Sha256(reader.PaletteRgb));
        Assert.Equal(expectedVideoHash, Sha256(reader.VideoFrame8));
        Assert.Equal(expectedAudioBytes, audio.Length);
        Assert.Equal(expectedAudioHash, Sha256(audio));
    }

    private static string Sha256(ReadOnlySpan<byte> data)
    {
        return Convert.ToHexString(SHA256.HashData(data)).ToLowerInvariant();
    }
}
