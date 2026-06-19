using System.IO;

namespace SmackerSharp.Internal;

internal ref struct BitReader
{
    private readonly ReadOnlySpan<byte> _data;
    private int _byteOffset;
    private int _bitOffset;

    public BitReader(ReadOnlySpan<byte> data)
    {
        _data = data;
        _byteOffset = 0;
        _bitOffset = 0;
    }

    public int ReadBit()
    {
        if (_byteOffset >= _data.Length)
        {
            throw new InvalidDataException("Bitstream exhausted.");
        }

        int value = (_data[_byteOffset] >> _bitOffset) & 1;

        if (_bitOffset >= 7)
        {
            _byteOffset++;
            _bitOffset = 0;
        }
        else
        {
            _bitOffset++;
        }

        return value;
    }

    public int ReadByte()
    {
        if (_byteOffset + (_bitOffset > 0 ? 1 : 0) >= _data.Length)
        {
            throw new InvalidDataException("Bitstream exhausted.");
        }

        if (_bitOffset == 0)
        {
            return _data[_byteOffset++];
        }

        int value = _data[_byteOffset] >> _bitOffset;
        _byteOffset++;
        return value | ((_data[_byteOffset] << (8 - _bitOffset)) & 0xFF);
    }
}
