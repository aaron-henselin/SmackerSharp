using System.IO;

namespace SmackerSharp.Internal;

internal static class VideoDecoder
{
    private const int TreeMMap = 0;
    private const int TreeMClr = 1;
    private const int TreeFull = 2;
    private const int TreeType = 3;

    private static ReadOnlySpan<ushort> SizeTable => new ushort[]
    {
        1, 2, 3, 4, 5, 6, 7, 8,
        9, 10, 11, 12, 13, 14, 15, 16,
        17, 18, 19, 20, 21, 22, 23, 24,
        25, 26, 27, 28, 29, 30, 31, 32,
        33, 34, 35, 36, 37, 38, 39, 40,
        41, 42, 43, 44, 45, 46, 47, 48,
        49, 50, 51, 52, 53, 54, 55, 56,
        57, 58, 59, 128, 256, 512, 1024, 2048
    };

    public static void Decode(ReadOnlySpan<byte> data, Huffman16[] trees, byte version, uint width, uint height, Span<byte> frame)
    {
        int w = checked((int)width);
        int h = checked((int)height);
        if (frame.Length < checked(w * h))
        {
            throw new ArgumentException("Frame buffer is smaller than the video dimensions.", nameof(frame));
        }

        if (trees.Length < 4)
        {
            throw new ArgumentException("Four video Huffman trees are required.", nameof(trees));
        }

        foreach (Huffman16 tree in trees)
        {
            tree.ResetCache();
        }

        BitReader reader = new(data);
        int row = 0;
        int col = 0;

        while (row < h)
        {
            int unpack = trees[TreeType].Lookup(ref reader);
            int type = unpack & 0x0003;
            int blockLength = (unpack & 0x00FC) >> 2;
            byte typeData = (byte)((unpack & 0xFF00) >> 8);

            if (type == 1 && version == (byte)'4')
            {
                int bit = reader.ReadBit();
                if (bit != 0)
                {
                    type = 4;
                }
                else if (reader.ReadBit() != 0)
                {
                    type = 5;
                }
            }

            int runLength = SizeTable[blockLength];
            for (int block = 0; block < runLength && row < h; block++)
            {
                int destination = (row * w) + col;
                switch (type)
                {
                    case 0:
                        DecodeMonoBlock(ref reader, trees, frame, destination, w);
                        break;
                    case 1:
                        DecodeFullBlock(ref reader, trees, frame, destination, w);
                        break;
                    case 2:
                        break;
                    case 3:
                        DecodeSolidBlock(frame, destination, w, typeData);
                        break;
                    case 4:
                        DecodeSmk4DoubleBlock(ref reader, trees, frame, destination, w);
                        break;
                    case 5:
                        DecodeSmk4HalfBlock(ref reader, trees, frame, destination, w);
                        break;
                    default:
                        throw new InvalidDataException($"Unsupported Smacker video block type {type}.");
                }

                col += 4;
                if (col >= w)
                {
                    col = 0;
                    row += 4;
                }
            }
        }
    }

    private static void DecodeMonoBlock(ref BitReader reader, Huffman16[] trees, Span<byte> frame, int destination, int width)
    {
        int colors = trees[TreeMClr].Lookup(ref reader);
        byte s1 = (byte)((colors & 0xFF00) >> 8);
        byte s2 = (byte)(colors & 0x00FF);
        int map = trees[TreeMMap].Lookup(ref reader);
        int mask = 0x01;
        int rowDestination = destination;

        for (int y = 0; y < 4; y++)
        {
            for (int x = 0; x < 4; x++)
            {
                frame[rowDestination + x] = (map & mask) != 0 ? s1 : s2;
                mask <<= 1;
            }

            rowDestination += width;
        }
    }

    private static void DecodeFullBlock(ref BitReader reader, Huffman16[] trees, Span<byte> frame, int destination, int width)
    {
        int rowDestination = destination;
        for (int y = 0; y < 4; y++)
        {
            int unpack = trees[TreeFull].Lookup(ref reader);
            frame[rowDestination + 3] = (byte)((unpack & 0xFF00) >> 8);
            frame[rowDestination + 2] = (byte)(unpack & 0x00FF);

            unpack = trees[TreeFull].Lookup(ref reader);
            frame[rowDestination + 1] = (byte)((unpack & 0xFF00) >> 8);
            frame[rowDestination] = (byte)(unpack & 0x00FF);
            rowDestination += width;
        }
    }

    private static void DecodeSolidBlock(Span<byte> frame, int destination, int width, byte value)
    {
        int rowDestination = destination;
        for (int y = 0; y < 4; y++)
        {
            frame.Slice(rowDestination, 4).Fill(value);
            rowDestination += width;
        }
    }

    private static void DecodeSmk4DoubleBlock(ref BitReader reader, Huffman16[] trees, Span<byte> frame, int destination, int width)
    {
        int rowDestination = destination;
        for (int y = 0; y < 2; y++)
        {
            int unpack = trees[TreeFull].Lookup(ref reader);
            for (int repeat = 0; repeat < 2; repeat++)
            {
                frame.Slice(rowDestination + 2, 2).Fill((byte)((unpack & 0xFF00) >> 8));
                frame.Slice(rowDestination, 2).Fill((byte)(unpack & 0x00FF));
                rowDestination += width;
            }
        }
    }

    private static void DecodeSmk4HalfBlock(ref BitReader reader, Huffman16[] trees, Span<byte> frame, int destination, int width)
    {
        int rowDestination = destination;
        for (int y = 0; y < 2; y++)
        {
            int unpack = trees[TreeFull].Lookup(ref reader);
            frame[rowDestination + 3] = (byte)((unpack & 0xFF00) >> 8);
            frame[rowDestination + 2] = (byte)(unpack & 0x00FF);
            frame[rowDestination + width + 3] = (byte)((unpack & 0xFF00) >> 8);
            frame[rowDestination + width + 2] = (byte)(unpack & 0x00FF);

            unpack = trees[TreeFull].Lookup(ref reader);
            frame[rowDestination + 1] = (byte)((unpack & 0xFF00) >> 8);
            frame[rowDestination] = (byte)(unpack & 0x00FF);
            frame[rowDestination + width + 1] = (byte)((unpack & 0xFF00) >> 8);
            frame[rowDestination + width] = (byte)(unpack & 0x00FF);
            rowDestination += width << 1;
        }
    }
}
