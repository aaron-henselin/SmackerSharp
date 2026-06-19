using System.Buffers.Binary;
using System.IO;
using SmackerSharp.Internal;

namespace SmackerSharp;

internal static class SmackerParser
{
    private const int AudioTrackCount = 7;
    private const int VideoTreeCount = 4;

    public static SmackerContainer Parse(ReadOnlyMemory<byte> data)
    {
        ReadOnlySpan<byte> span = data.Span;

        if (span.Length < 4)
        {
            throw new InvalidDataException("The stream is too short to contain a Smacker signature.");
        }

        if (span[0] != (byte)'S' || span[1] != (byte)'M' || span[2] != (byte)'K')
        {
            throw new InvalidDataException("The stream does not begin with an SMK signature.");
        }

        byte version = span[3];
        if (version != (byte)'2' && version != (byte)'4')
        {
            throw new InvalidDataException($"Unsupported Smacker version '{(char)version}'.");
        }

        if (span.Length < 104)
        {
            throw new InvalidDataException("The stream is too short to contain the fixed Smacker header.");
        }

        int offset = 4;
        uint width = ReadUInt32(span, ref offset);
        uint height = ReadUInt32(span, ref offset);
        uint frames = ReadUInt32(span, ref offset);
        int frameTiming = unchecked((int)ReadUInt32(span, ref offset));
        uint flags = ReadUInt32(span, ref offset);

        double microsecondsPerFrame = DecodeMicrosecondsPerFrame(frameTiming);

        bool hasRingFrame = (flags & 0x01) != 0;
        SmackerYScaleMode yScaleMode = SmackerYScaleMode.None;
        if ((flags & 0x02) != 0)
        {
            yScaleMode = SmackerYScaleMode.Double;
        }

        if ((flags & 0x04) != 0)
        {
            yScaleMode = SmackerYScaleMode.Interlace;
        }

        uint[] maxAudioBufferSizes = new uint[AudioTrackCount];
        for (int i = 0; i < maxAudioBufferSizes.Length; i++)
        {
            maxAudioBufferSizes[i] = ReadUInt32(span, ref offset);
        }

        uint huffmanTreeChunkSize = ReadUInt32(span, ref offset);

        uint[] treeSizes = new uint[VideoTreeCount];
        for (int i = 0; i < treeSizes.Length; i++)
        {
            treeSizes[i] = ReadUInt32(span, ref offset);
        }

        SmackerAudioTrackInfo[] audioTracks = new SmackerAudioTrackInfo[AudioTrackCount];
        for (int i = 0; i < audioTracks.Length; i++)
        {
            uint audioRateData = ReadUInt32(span, ref offset);
            audioTracks[i] = ParseAudioTrack(audioRateData);
        }

        _ = ReadUInt32(span, ref offset);

        int storedFrameCount = checked((int)(frames + (hasRingFrame ? 1u : 0u)));
        EnsureAvailable(span, offset, checked(storedFrameCount * 4), "frame size table");

        uint[] chunkSizes = new uint[storedFrameCount];
        bool[] keyframes = new bool[storedFrameCount];
        for (int i = 0; i < storedFrameCount; i++)
        {
            uint chunkSizeAndFlags = ReadUInt32(span, ref offset);
            keyframes[i] = (chunkSizeAndFlags & 0x01) != 0;
            chunkSizes[i] = chunkSizeAndFlags & 0xFFFFFFFCu;
        }

        EnsureAvailable(span, offset, storedFrameCount, "frame type table");
        byte[] frameTypes = span.Slice(offset, storedFrameCount).ToArray();
        offset += storedFrameCount;

        EnsureAvailable(span, offset, checked((int)huffmanTreeChunkSize), "Huffman tree chunk");
        ReadOnlyMemory<byte> huffmanTreeChunk = data.Slice(offset, checked((int)huffmanTreeChunkSize));
        offset += checked((int)huffmanTreeChunkSize);

        Huffman16[] videoTrees = BuildVideoTrees(huffmanTreeChunk.Span, treeSizes);

        ReadOnlyMemory<byte>[] frameChunks = new ReadOnlyMemory<byte>[storedFrameCount];
        for (int i = 0; i < frameChunks.Length; i++)
        {
            int chunkSize = checked((int)chunkSizes[i]);
            EnsureAvailable(span, offset, chunkSize, $"frame chunk {i}");
            frameChunks[i] = data.Slice(offset, chunkSize);
            offset += chunkSize;
        }

        SmackerInfo info = new(0, frames, microsecondsPerFrame);
        SmackerVideoInfo videoInfo = new(width, height, yScaleMode);

        return new SmackerContainer(
            version,
            info,
            videoInfo,
            audioTracks,
            maxAudioBufferSizes,
            treeSizes,
            huffmanTreeChunkSize,
            hasRingFrame,
            chunkSizes,
            keyframes,
            frameTypes,
            huffmanTreeChunk,
            videoTrees,
            frameChunks);
    }

    private static Huffman16[] BuildVideoTrees(ReadOnlySpan<byte> huffmanTreeChunk, uint[] treeSizes)
    {
        BitReader reader = new(huffmanTreeChunk);
        Huffman16[] trees = new Huffman16[VideoTreeCount];

        for (int i = 0; i < trees.Length; i++)
        {
            trees[i] = new Huffman16();
            trees[i].Build(ref reader, treeSizes[i]);
        }

        return trees;
    }

    private static SmackerAudioTrackInfo ParseAudioTrack(uint audioRateData)
    {
        if ((audioRateData & 0x40000000u) == 0)
        {
            return new SmackerAudioTrackInfo(false, 0, 0, 0, SmackerAudioCompression.None);
        }

        SmackerAudioCompression compression = SmackerAudioCompression.None;
        if ((audioRateData & 0x80000000u) != 0)
        {
            compression = SmackerAudioCompression.SmackerDpcm;
        }

        if ((audioRateData & 0x0C000000u) != 0)
        {
            compression = SmackerAudioCompression.Bink;
        }

        byte bitDepth = (audioRateData & 0x20000000u) != 0 ? (byte)16 : (byte)8;
        byte channels = (audioRateData & 0x10000000u) != 0 ? (byte)2 : (byte)1;
        uint rate = audioRateData & 0x00FFFFFFu;

        return new SmackerAudioTrackInfo(true, channels, bitDepth, rate, compression);
    }

    private static double DecodeMicrosecondsPerFrame(int frameTiming)
    {
        if (frameTiming > 0)
        {
            return frameTiming * 1000.0;
        }

        if (frameTiming < 0)
        {
            return frameTiming * -10.0;
        }

        return 100000.0;
    }

    private static uint ReadUInt32(ReadOnlySpan<byte> data, ref int offset)
    {
        EnsureAvailable(data, offset, 4, "uint32");
        uint value = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(offset, 4));
        offset += 4;
        return value;
    }

    private static void EnsureAvailable(ReadOnlySpan<byte> data, int offset, int count, string fieldName)
    {
        if (count < 0 || offset < 0 || offset > data.Length || count > data.Length - offset)
        {
            throw new InvalidDataException($"The stream is too short to contain the {fieldName}.");
        }
    }
}
