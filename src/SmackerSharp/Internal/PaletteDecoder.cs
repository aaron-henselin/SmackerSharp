using System.IO;

namespace SmackerSharp.Internal;

internal static class PaletteDecoder
{
    private static ReadOnlySpan<byte> PaletteMap => new byte[]
    {
        0x00, 0x04, 0x08, 0x0C, 0x10, 0x14, 0x18, 0x1C,
        0x20, 0x24, 0x28, 0x2C, 0x30, 0x34, 0x38, 0x3C,
        0x41, 0x45, 0x49, 0x4D, 0x51, 0x55, 0x59, 0x5D,
        0x61, 0x65, 0x69, 0x6D, 0x71, 0x75, 0x79, 0x7D,
        0x82, 0x86, 0x8A, 0x8E, 0x92, 0x96, 0x9A, 0x9E,
        0xA2, 0xA6, 0xAA, 0xAE, 0xB2, 0xB6, 0xBA, 0xBE,
        0xC3, 0xC7, 0xCB, 0xCF, 0xD3, 0xD7, 0xDB, 0xDF,
        0xE3, 0xE7, 0xEB, 0xEF, 0xF3, 0xF7, 0xFB, 0xFF
    };

    public static void Decode(ReadOnlySpan<byte> data, Span<byte> paletteRgb)
    {
        if (paletteRgb.Length != 256 * 3)
        {
            throw new ArgumentException("Palette buffer must contain exactly 768 RGB bytes.", nameof(paletteRgb));
        }

        Span<byte> oldPalette = stackalloc byte[256 * 3];
        paletteRgb.CopyTo(oldPalette);

        int paletteIndex = 0;
        int offset = 0;

        while (paletteIndex < 256 && offset < data.Length)
        {
            byte command = data[offset];
            if ((command & 0x80) != 0)
            {
                int count = (command & 0x7F) + 1;
                offset++;
                EnsurePaletteWrite(paletteIndex, count);
                paletteIndex += count;
            }
            else if ((command & 0x40) != 0)
            {
                if (data.Length - offset < 2)
                {
                    throw new InvalidDataException("Palette color-shift block is truncated.");
                }

                int count = (command & 0x3F) + 1;
                offset++;
                int sourceIndex = data[offset];
                offset++;

                EnsurePaletteWrite(paletteIndex, count);
                EnsurePaletteRead(sourceIndex, count);

                oldPalette.Slice(sourceIndex * 3, count * 3).CopyTo(paletteRgb.Slice(paletteIndex * 3, count * 3));
                paletteIndex += count;
            }
            else
            {
                if (data.Length - offset < 3)
                {
                    throw new InvalidDataException("Palette direct-color block is truncated.");
                }

                for (int component = 0; component < 3; component++)
                {
                    byte value = data[offset++];
                    if (value > 0x3F)
                    {
                        throw new InvalidDataException("Palette component exceeds the Smacker 6-bit range.");
                    }

                    paletteRgb[(paletteIndex * 3) + component] = PaletteMap[value];
                }

                paletteIndex++;
            }
        }

        if (paletteIndex < 256)
        {
            throw new InvalidDataException("Palette chunk did not fill all 256 entries.");
        }
    }

    private static void EnsurePaletteWrite(int start, int count)
    {
        if (start + count > 256)
        {
            throw new InvalidDataException("Palette block writes past the 256-color palette.");
        }
    }

    private static void EnsurePaletteRead(int start, int count)
    {
        if (start + count > 256)
        {
            throw new InvalidDataException("Palette color-shift block reads past the previous palette.");
        }
    }
}
