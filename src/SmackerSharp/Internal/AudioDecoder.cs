using System.Buffers.Binary;
using System.IO;

namespace SmackerSharp.Internal;

internal static class AudioDecoder
{
    public static int Decode(ReadOnlySpan<byte> data, SmackerAudioTrackInfo trackInfo, Span<byte> destination)
    {
        if (!trackInfo.Exists)
        {
            return 0;
        }

        return trackInfo.Compression switch
        {
            SmackerAudioCompression.None => DecodeRaw(data, destination),
            SmackerAudioCompression.SmackerDpcm => DecodeDpcm(data, trackInfo, destination),
            SmackerAudioCompression.Bink => throw new NotSupportedException("Bink/perceptual Smacker audio is not supported."),
            _ => throw new InvalidDataException($"Unknown Smacker audio compression mode {trackInfo.Compression}.")
        };
    }

    private static int DecodeRaw(ReadOnlySpan<byte> data, Span<byte> destination)
    {
        if (data.Length > destination.Length)
        {
            throw new InvalidDataException("Raw audio chunk is larger than the destination buffer.");
        }

        data.CopyTo(destination);
        return data.Length;
    }

    private static int DecodeDpcm(ReadOnlySpan<byte> data, SmackerAudioTrackInfo trackInfo, Span<byte> destination)
    {
        if (data.Length < 4)
        {
            throw new InvalidDataException("Compressed audio chunk is missing the decoded size.");
        }

        int decodedSize = checked((int)BinaryPrimitives.ReadUInt32LittleEndian(data[..4]));
        if (decodedSize > destination.Length)
        {
            throw new InvalidDataException("Decoded audio chunk is larger than the destination buffer.");
        }

        Span<byte> output = destination[..decodedSize];
        BitReader reader = new(data[4..]);

        if (reader.ReadBit() == 0)
        {
            throw new InvalidDataException("Compressed audio chunk has an invalid initial marker bit.");
        }

        int channels = reader.ReadBit() == 1 ? 2 : 1;
        if (channels != trackInfo.Channels)
        {
            throw new InvalidDataException("Compressed audio channel count does not match the track header.");
        }

        int bitDepth = reader.ReadBit() == 1 ? 16 : 8;
        if (bitDepth != trackInfo.BitDepth)
        {
            throw new InvalidDataException("Compressed audio bit depth does not match the track header.");
        }

        Huffman8[] trees = BuildAudioTrees(ref reader, channels, bitDepth);

        if (bitDepth == 8)
        {
            Decode8Bit(ref reader, trees, channels, output);
        }
        else
        {
            Decode16Bit(ref reader, trees, channels, output);
        }

        return decodedSize;
    }

    private static Huffman8[] BuildAudioTrees(ref BitReader reader, int channels, int bitDepth)
    {
        Huffman8[] trees = { new(), new(), new(), new() };
        trees[0].Build(ref reader);

        if (bitDepth == 16)
        {
            trees[1].Build(ref reader);
        }

        if (channels == 2)
        {
            trees[2].Build(ref reader);

            if (bitDepth == 16)
            {
                trees[3].Build(ref reader);
            }
        }

        return trees;
    }

    private static void Decode8Bit(ref BitReader reader, Huffman8[] trees, int channels, Span<byte> output)
    {
        if (output.Length == 0)
        {
            return;
        }

        int j = 1;
        int bytesWritten = 1;

        if (channels == 2)
        {
            EnsureOutputLength(output, 2);
            output[1] = (byte)reader.ReadByte();
            j = 2;
            bytesWritten = 2;
        }

        output[0] = (byte)reader.ReadByte();

        while (bytesWritten < output.Length)
        {
            int delta = unchecked((sbyte)(byte)trees[0].Lookup(ref reader));
            output[j] = unchecked((byte)(output[j - channels] + delta));
            j++;
            bytesWritten++;

            if (channels == 2 && bytesWritten < output.Length)
            {
                delta = unchecked((sbyte)(byte)trees[2].Lookup(ref reader));
                output[j] = unchecked((byte)(output[j - 2] + delta));
                j++;
                bytesWritten++;
            }
        }
    }

    private static void Decode16Bit(ref BitReader reader, Huffman8[] trees, int channels, Span<byte> output)
    {
        if (output.Length == 0)
        {
            return;
        }

        int sampleCount = output.Length / 2;
        EnsureOutputLength(output, channels * 2);
        int sampleIndex = channels == 2 ? 2 : 1;
        int bytesWritten = channels * 2;

        if (channels == 2)
        {
            int low = reader.ReadByte();
            int high = reader.ReadByte();
            WriteInt16(output, 1, unchecked((short)(low | (high << 8))));
        }

        int firstLow = reader.ReadByte();
        int firstHigh = reader.ReadByte();
        WriteInt16(output, 0, unchecked((short)(firstLow | (firstHigh << 8))));

        while (bytesWritten < output.Length && sampleIndex < sampleCount)
        {
            short delta = ReadDelta16(ref reader, trees[0], trees[1]);
            short previous = ReadInt16(output, sampleIndex - channels);
            WriteInt16(output, sampleIndex, unchecked((short)(previous + delta)));
            sampleIndex++;
            bytesWritten += 2;

            if (channels == 2 && bytesWritten < output.Length && sampleIndex < sampleCount)
            {
                delta = ReadDelta16(ref reader, trees[2], trees[3]);
                previous = ReadInt16(output, sampleIndex - 2);
                WriteInt16(output, sampleIndex, unchecked((short)(previous + delta)));
                sampleIndex++;
                bytesWritten += 2;
            }
        }
    }

    private static short ReadDelta16(ref BitReader reader, Huffman8 lowTree, Huffman8 highTree)
    {
        int low = lowTree.Lookup(ref reader);
        int high = highTree.Lookup(ref reader);
        return unchecked((short)(low | (high << 8)));
    }

    private static short ReadInt16(ReadOnlySpan<byte> output, int sampleIndex)
    {
        return BinaryPrimitives.ReadInt16LittleEndian(output.Slice(sampleIndex * 2, 2));
    }

    private static void WriteInt16(Span<byte> output, int sampleIndex, short value)
    {
        BinaryPrimitives.WriteInt16LittleEndian(output.Slice(sampleIndex * 2, 2), value);
    }

    private static void EnsureOutputLength(ReadOnlySpan<byte> output, int minimumLength)
    {
        if (output.Length < minimumLength)
        {
            throw new InvalidDataException("Decoded audio buffer is too small for the track layout.");
        }
    }
}
