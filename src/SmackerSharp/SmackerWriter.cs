using System.Buffers.Binary;
using System.IO;

namespace SmackerSharp;

public sealed class SmackerWriter : IDisposable
{
    private const int AudioTrackCount = 7;
    private const int VideoTreeCount = 4;
    private readonly Stream _output;
    private readonly SmackerWriterOptions _options;
    private readonly List<PendingFrame> _frames = new();
    private bool _finished;
    private bool _disposed;

    public SmackerWriter(Stream output, SmackerWriterOptions options)
    {
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(options);

        if (!output.CanWrite)
        {
            throw new ArgumentException("The stream must be writable.", nameof(output));
        }

        if (options.Version is not ('2' or '4'))
        {
            throw new ArgumentOutOfRangeException(nameof(options), options.Version, "Smacker version must be '2' or '4'.");
        }

        if (options.Width == 0 || options.Height == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Width and height must be positive.");
        }

        if ((options.Width % 4) != 0 || (options.Height % 4) != 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "The initial writer supports dimensions divisible by 4.");
        }

        if (options.MicrosecondsPerFrame <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Microseconds per frame must be positive.");
        }

        _output = output;
        _options = options;
    }

    public void AddFrame(
        ReadOnlySpan<byte> indexedPixels,
        ReadOnlySpan<byte> paletteRgb,
        IReadOnlyList<ReadOnlyMemory<byte>>? audioChunks = null)
    {
        ThrowIfDisposed();
        if (_finished)
        {
            throw new InvalidOperationException("Cannot add frames after Finish has been called.");
        }

        int pixelCount = checked((int)(_options.Width * _options.Height));
        if (indexedPixels.Length != pixelCount)
        {
            throw new ArgumentException("Indexed pixel buffer does not match writer dimensions.", nameof(indexedPixels));
        }

        if (paletteRgb.Length != 256 * 3)
        {
            throw new ArgumentException("Palette must contain exactly 768 RGB bytes.", nameof(paletteRgb));
        }

        if (audioChunks is { Count: > 0 })
        {
            throw new NotSupportedException("The initial Smacker writer does not encode audio.");
        }

        byte solidColorIndex = indexedPixels[0];
        for (int i = 1; i < indexedPixels.Length; i++)
        {
            if (indexedPixels[i] != solidColorIndex)
            {
                throw new NotSupportedException("The initial Smacker writer only supports solid-color indexed video frames.");
            }
        }

        _frames.Add(new PendingFrame(solidColorIndex, paletteRgb.ToArray()));
    }

    public void Finish()
    {
        ThrowIfDisposed();
        if (_finished)
        {
            return;
        }

        if (_frames.Count == 0)
        {
            throw new InvalidOperationException("At least one frame is required.");
        }

        WriteHeaderAndFrames();
        _finished = true;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
    }

    private static byte[] BuildFrameChunk(ReadOnlySpan<byte> paletteRgb, string typeCode)
    {
        BitWriter videoBits = new();
        WriteBits(videoBits, typeCode);
        byte[] videoData = videoBits.ToArray();
        int unpaddedLength = 772 + videoData.Length;
        int paddedLength = (unpaddedLength + 3) & ~3;
        byte[] chunk = new byte[paddedLength];
        chunk[0] = 193;

        for (int i = 0; i < paletteRgb.Length; i++)
        {
            byte value = paletteRgb[i];
            chunk[i + 1] = value switch
            {
                0x00 => 0,
                0x04 => 1,
                0x08 => 2,
                0x0C => 3,
                0x10 => 4,
                0x14 => 5,
                0x18 => 6,
                0x1C => 7,
                0x20 => 8,
                0x24 => 9,
                0x28 => 10,
                0x2C => 11,
                0x30 => 12,
                0x34 => 13,
                0x38 => 14,
                0x3C => 15,
                0x41 => 16,
                0x45 => 17,
                0x49 => 18,
                0x4D => 19,
                0x51 => 20,
                0x55 => 21,
                0x59 => 22,
                0x5D => 23,
                0x61 => 24,
                0x65 => 25,
                0x69 => 26,
                0x6D => 27,
                0x71 => 28,
                0x75 => 29,
                0x79 => 30,
                0x7D => 31,
                0x82 => 32,
                0x86 => 33,
                0x8A => 34,
                0x8E => 35,
                0x92 => 36,
                0x96 => 37,
                0x9A => 38,
                0x9E => 39,
                0xA2 => 40,
                0xA6 => 41,
                0xAA => 42,
                0xAE => 43,
                0xB2 => 44,
                0xB6 => 45,
                0xBA => 46,
                0xBE => 47,
                0xC3 => 48,
                0xC7 => 49,
                0xCB => 50,
                0xCF => 51,
                0xD3 => 52,
                0xD7 => 53,
                0xDB => 54,
                0xDF => 55,
                0xE3 => 56,
                0xE7 => 57,
                0xEB => 58,
                0xEF => 59,
                0xF3 => 60,
                0xF7 => 61,
                0xFB => 62,
                0xFF => 63,
                _ => throw new ArgumentException("Palette contains a value that cannot be represented by Smacker's 6-bit palette map.", nameof(paletteRgb))
            };
        }

        videoData.CopyTo(chunk.AsSpan(772));
        return chunk;
    }

    private void WriteHeaderAndFrames()
    {
        Span<byte> buffer = stackalloc byte[4];

        _output.WriteByte((byte)'S');
        _output.WriteByte((byte)'M');
        _output.WriteByte((byte)'K');
        _output.WriteByte((byte)_options.Version);
        WriteUInt32(buffer, _options.Width);
        WriteUInt32(buffer, _options.Height);
        WriteUInt32(buffer, checked((uint)_frames.Count));
        WriteUInt32(buffer, EncodeFrameTiming(_options.MicrosecondsPerFrame));
        WriteUInt32(buffer, 0);

        for (int i = 0; i < AudioTrackCount; i++)
        {
            WriteUInt32(buffer, 0);
        }

        byte[] solidColorIndices = _frames.Select(frame => frame.SolidColorIndex).Distinct().Order().ToArray();
        Dictionary<byte, string> typeCodes = BuildCodes(solidColorIndices);
        List<byte[]> frameChunks = _frames
            .Select(frame => BuildFrameChunk(frame.PaletteRgb.Span, typeCodes[frame.SolidColorIndex]))
            .ToList();

        byte[] huffmanTreeChunk = BuildHuffmanTreeChunk(solidColorIndices);
        WriteUInt32(buffer, checked((uint)huffmanTreeChunk.Length));
        for (int i = 0; i < VideoTreeCount - 1; i++)
        {
            WriteUInt32(buffer, 0);
        }

        WriteUInt32(buffer, checked((uint)(12 + ((solidColorIndices.Length * 2) - 1) * 4)));

        for (int i = 0; i < AudioTrackCount; i++)
        {
            WriteUInt32(buffer, 0);
        }

        WriteUInt32(buffer, 0);

        foreach (byte[] chunk in frameChunks)
        {
            WriteUInt32(buffer, checked((uint)chunk.Length) | 0x01u);
        }

        for (int i = 0; i < frameChunks.Count; i++)
        {
            _output.WriteByte(0x01);
        }

        _output.Write(huffmanTreeChunk);

        foreach (byte[] chunk in frameChunks)
        {
            _output.Write(chunk);
        }
    }

    private void WriteUInt32(Span<byte> buffer, uint value)
    {
        BinaryPrimitives.WriteUInt32LittleEndian(buffer, value);
        _output.Write(buffer);
    }

    private static uint EncodeFrameTiming(double microsecondsPerFrame)
    {
        if (microsecondsPerFrame % 1000 == 0)
        {
            return checked((uint)(microsecondsPerFrame / 1000));
        }

        return unchecked((uint)checked((int)(microsecondsPerFrame / -10)));
    }

    private static byte[] BuildHuffmanTreeChunk(byte[] solidColorIndices)
    {
        BitWriter writer = new();
        WriteAbsentHuffman16(writer);
        WriteAbsentHuffman16(writer);
        WriteAbsentHuffman16(writer);
        WriteTypeHuffman16(writer, solidColorIndices);
        return writer.ToArray();
    }

    private static void WriteAbsentHuffman16(BitWriter writer)
    {
        writer.WriteBit(0);
        writer.WriteBit(0);
    }

    private static void WriteTypeHuffman16(BitWriter writer, byte[] solidColorIndices)
    {
        writer.WriteBit(1);
        WriteSingleLeafHuffman8(writer, 0x03);
        WriteHuffman8(writer, solidColorIndices);
        WriteCacheValues(writer);
        WriteHuffman16ValueNode(writer, solidColorIndices, BuildCodes(solidColorIndices));
        writer.WriteBit(0);
    }

    private static void WriteSingleLeafHuffman8(BitWriter writer, byte value)
    {
        writer.WriteBit(1);
        writer.WriteBit(0);
        writer.WriteByte(value);
        writer.WriteBit(0);
    }

    private static void WriteCacheValues(BitWriter writer)
    {
        writer.WriteByte(0xAA);
        writer.WriteByte(0xAA);
        writer.WriteByte(0xBB);
        writer.WriteByte(0xBB);
        writer.WriteByte(0xCC);
        writer.WriteByte(0xCC);
    }

    private static void WriteHuffman8(BitWriter writer, byte[] values)
    {
        writer.WriteBit(1);
        WriteHuffman8Node(writer, values);
        writer.WriteBit(0);
    }

    private static void WriteHuffman8Node(BitWriter writer, byte[] values)
    {
        if (values.Length == 1)
        {
            writer.WriteBit(0);
            writer.WriteByte(values[0]);
            return;
        }

        int leftCount = values.Length / 2;
        writer.WriteBit(1);
        WriteHuffman8Node(writer, values[..leftCount]);
        WriteHuffman8Node(writer, values[leftCount..]);
    }

    private static void WriteHuffman16ValueNode(BitWriter writer, byte[] values, Dictionary<byte, string> highByteCodes)
    {
        if (values.Length == 1)
        {
            writer.WriteBit(0);
            WriteBits(writer, highByteCodes[values[0]]);
            return;
        }

        int leftCount = values.Length / 2;
        writer.WriteBit(1);
        WriteHuffman16ValueNode(writer, values[..leftCount], highByteCodes);
        WriteHuffman16ValueNode(writer, values[leftCount..], highByteCodes);
    }

    private static Dictionary<byte, string> BuildCodes(byte[] values)
    {
        Dictionary<byte, string> codes = new();
        BuildCodes(values, "", codes);
        return codes;
    }

    private static void BuildCodes(byte[] values, string prefix, Dictionary<byte, string> codes)
    {
        if (values.Length == 1)
        {
            codes.Add(values[0], prefix);
            return;
        }

        int leftCount = values.Length / 2;
        BuildCodes(values[..leftCount], prefix + "0", codes);
        BuildCodes(values[leftCount..], prefix + "1", codes);
    }

    private static void WriteBits(BitWriter writer, string bits)
    {
        foreach (char bit in bits)
        {
            writer.WriteBit(bit == '1' ? 1 : 0);
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    private sealed record PendingFrame(byte SolidColorIndex, ReadOnlyMemory<byte> PaletteRgb);

    private sealed class BitWriter
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
