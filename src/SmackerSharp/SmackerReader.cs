using System.Buffers;
using System.IO;
using SmackerSharp.Internal;

namespace SmackerSharp;

/// <summary>
/// Provides a fully managed reader and decoder for RAD Smacker (.smk) video files.
/// </summary>
public sealed class SmackerReader : IDisposable
{
    private readonly IMemoryOwner<byte>? _ownedData;
    private readonly SmackerContainer _container;
    private readonly byte[] _paletteRgb;
    private readonly byte[] _videoFrame;
    private readonly byte[][] _audioBuffers;
    private readonly uint[] _audioBufferSizes;
    private readonly bool[] _audioEnabled;
    private uint _currentFrame;
    private bool _disposed;

    private SmackerReader(
        IMemoryOwner<byte>? ownedData,
        SmackerContainer container)
    {
        _ownedData = ownedData;
        _container = container;
        VideoInfo = container.VideoInfo;
        AudioTracks = container.AudioTracks;
        _paletteRgb = new byte[256 * 3];
        _videoFrame = new byte[checked((int)(container.VideoInfo.Width * container.VideoInfo.Height))];
        _audioBuffers = new byte[7][];
        _audioBufferSizes = new uint[7];
        _audioEnabled = new bool[7];
        for (int i = 0; i < _audioBuffers.Length; i++)
        {
            _audioBuffers[i] = new byte[checked((int)container.MaxAudioBufferSizes[i])];
        }
    }

    /// <summary>
    /// Gets the current state and timing info of the Smacker file.
    /// </summary>
    public SmackerInfo Info
    {
        get
        {
            uint currentFrame = _container.Info.FrameCount == 0 ? 0 : _currentFrame % _container.Info.FrameCount;
            return _container.Info with { CurrentFrame = currentFrame };
        }
    }

    /// <summary>
    /// Gets the video dimension and layout properties.
    /// </summary>
    public SmackerVideoInfo VideoInfo { get; }

    /// <summary>
    /// Gets the list of audio track headers present in the Smacker file.
    /// </summary>
    public IReadOnlyList<SmackerAudioTrackInfo> AudioTracks { get; }

    internal SmackerContainer Container => _container;

    /// <summary>
    /// Gets the Smacker file format version identifier.
    /// </summary>
    public char Version => (char)_container.Version;

    /// <summary>
    /// Gets or sets a value indicating whether video frame decoding is enabled.
    /// </summary>
    public bool VideoEnabled { get; set; }

    /// <summary>
    /// Gets the mask representing all available audio tracks in the file.
    /// </summary>
    public SmackerTrackMask AudioTrackMask
    {
        get
        {
            SmackerTrackMask mask = SmackerTrackMask.None;
            for (int i = 0; i < AudioTracks.Count; i++)
            {
                if (AudioTracks[i].Exists)
                {
                    mask |= (SmackerTrackMask)(1 << i);
                }
            }

            return mask;
        }
    }

    /// <summary>
    /// Gets the current frame's palette as 256 RGB byte triples (768 bytes total).
    /// </summary>
    public ReadOnlySpan<byte> PaletteRgb
    {
        get
        {
            ThrowIfDisposed();
            return _paletteRgb;
        }
    }

    /// <summary>
    /// Gets the current frame's decoded 8-bit palettized video pixel buffer.
    /// </summary>
    public ReadOnlySpan<byte> VideoFrame8
    {
        get
        {
            ThrowIfDisposed();
            return _videoFrame;
        }
    }

    /// <summary>
    /// Opens a Smacker file from the specified file path.
    /// </summary>
    /// <param name="path">The path to the Smacker file.</param>
    /// <param name="mode">The streaming mode (Memory or Disk).</param>
    /// <returns>A new <see cref="SmackerReader"/> instance.</returns>
    public static SmackerReader Open(string path, SmackerOpenMode mode = SmackerOpenMode.Memory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (mode == SmackerOpenMode.Disk)
        {
            using FileStream stream = File.OpenRead(path);
            return Open(stream, SmackerOpenMode.Disk);
        }

        byte[] data = File.ReadAllBytes(path);
        return Open(data);
    }

    /// <summary>
    /// Opens a Smacker file from the specified stream.
    /// </summary>
    /// <param name="stream">The source stream.</param>
    /// <param name="mode">The streaming mode (Memory or Disk).</param>
    /// <returns>A new <see cref="SmackerReader"/> instance.</returns>
    public static SmackerReader Open(Stream stream, SmackerOpenMode mode = SmackerOpenMode.Memory)
    {
        ArgumentNullException.ThrowIfNull(stream);

        if (!stream.CanRead)
        {
            throw new ArgumentException("The stream must be readable.", nameof(stream));
        }

        using MemoryStream memory = new();
        stream.CopyTo(memory);
        byte[] data = memory.ToArray();
        return Open(data);
    }

    /// <summary>
    /// Opens a Smacker file from a read-only memory buffer.
    /// </summary>
    /// <param name="data">The buffer containing the entire Smacker file.</param>
    /// <returns>A new <see cref="SmackerReader"/> instance.</returns>
    public static SmackerReader Open(ReadOnlyMemory<byte> data)
    {
        SmackerContainer container = SmackerParser.Parse(data);
        return new SmackerReader(null, container);
    }

    /// <summary>
    /// Enables or disables decoding of a specific audio track.
    /// </summary>
    /// <param name="track">The audio track index (0 to 6).</param>
    /// <param name="enabled">True to enable decoding; false to disable.</param>
    public void SetAudioEnabled(int track, bool enabled)
    {
        ThrowIfDisposed();

        if ((uint)track >= 7)
        {
            throw new ArgumentOutOfRangeException(nameof(track), track, "Audio track must be between 0 and 6.");
        }

        _audioEnabled[track] = enabled;
    }

    /// <summary>
    /// Checks whether decoding is enabled for a specific audio track.
    /// </summary>
    /// <param name="track">The audio track index (0 to 6).</param>
    /// <returns>True if decoding is enabled; otherwise, false.</returns>
    public bool IsAudioEnabled(int track)
    {
        ThrowIfDisposed();

        if ((uint)track >= 7)
        {
            throw new ArgumentOutOfRangeException(nameof(track), track, "Audio track must be between 0 and 6.");
        }

        return _audioEnabled[track];
    }

    /// <summary>
    /// Enables or disables multiple tracks (video and audio) at once using a mask.
    /// </summary>
    /// <param name="mask">The track mask combination to enable.</param>
    public void SetEnabled(SmackerTrackMask mask)
    {
        ThrowIfDisposed();

        VideoEnabled = (mask & SmackerTrackMask.Video) != 0;
        for (int i = 0; i < 7; i++)
        {
            _audioEnabled[i] = AudioTracks[i].Exists && (((byte)mask & (1 << i)) != 0);
        }
    }

    /// <summary>
    /// Retrieves the decoded audio sample bytes for the current frame on a specific track.
    /// </summary>
    /// <param name="track">The audio track index (0 to 6).</param>
    /// <returns>A span containing the decoded PCM audio bytes.</returns>
    public ReadOnlySpan<byte> GetAudio(int track)
    {
        ThrowIfDisposed();

        if ((uint)track >= 7)
        {
            throw new ArgumentOutOfRangeException(nameof(track), track, "Audio track must be between 0 and 6.");
        }

        return _audioBuffers[track].AsSpan(0, checked((int)_audioBufferSizes[track]));
    }

    /// <summary>
    /// Rewinds the reader to the first frame and decodes it.
    /// </summary>
    /// <returns>A value indicating whether more frames remain in the stream.</returns>
    public SmackerFrameResult First()
    {
        ThrowIfDisposed();
        _currentFrame = 0;
        RenderCurrentFrame();
        return Info.FrameCount <= 1 ? SmackerFrameResult.Last : SmackerFrameResult.More;
    }

    /// <summary>
    /// Advances the reader to the next frame and decodes it.
    /// </summary>
    /// <returns>The result of the advance operation (Done, More, or Last).</returns>
    public SmackerFrameResult Next()
    {
        ThrowIfDisposed();
        if (_currentFrame + 1 < Info.FrameCount + (_container.HasRingFrame ? 1u : 0u))
        {
            _currentFrame++;
            RenderCurrentFrame();
            return _currentFrame + 1 == Info.FrameCount + (_container.HasRingFrame ? 1u : 0u)
                ? SmackerFrameResult.Last
                : SmackerFrameResult.More;
        }

        if (_container.HasRingFrame)
        {
            _currentFrame = 1;
            RenderCurrentFrame();
            return _currentFrame + 1 == Info.FrameCount + 1 ? SmackerFrameResult.Last : SmackerFrameResult.More;
        }

        return SmackerFrameResult.Done;
    }

    /// <summary>
    /// Seeks to the nearest keyframe before or at the specified frame index, and decodes it.
    /// </summary>
    /// <param name="frame">The target 0-based frame index.</param>
    public void SeekKeyframe(uint frame)
    {
        ThrowIfDisposed();

        if (frame >= Info.FrameCount)
        {
            throw new ArgumentOutOfRangeException(nameof(frame), frame, "Frame must be inside the stream.");
        }

        _currentFrame = frame;
        while (_currentFrame > 0 && !_container.Keyframes[_currentFrame])
        {
            _currentFrame--;
        }

        RenderCurrentFrame();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _ownedData?.Dispose();
        _disposed = true;
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    private void RenderCurrentFrame()
    {
        ReadOnlySpan<byte> chunk = _container.FrameChunks[checked((int)_currentFrame)].Span;
        int offset = 0;
        byte frameType = _container.FrameTypes[checked((int)_currentFrame)];

        if ((frameType & 0x01) != 0)
        {
            if (chunk.Length == 0)
            {
                throw new InvalidDataException("Frame is missing palette chunk data.");
            }

            int paletteChunkSize = 4 * chunk[offset];
            if (paletteChunkSize <= 0 || paletteChunkSize > chunk.Length - offset)
            {
                throw new InvalidDataException("Frame palette chunk size is invalid.");
            }

            if (VideoEnabled)
            {
                PaletteDecoder.Decode(chunk.Slice(offset + 1, paletteChunkSize - 1), _paletteRgb);
            }

            offset += paletteChunkSize;
        }

        for (int track = 0; track < 7; track++)
        {
            if ((frameType & (0x02 << track)) == 0)
            {
                _audioBufferSizes[track] = 0;
                continue;
            }

            if (chunk.Length - offset < 4)
            {
                throw new InvalidDataException($"Frame is missing audio chunk size for track {track}.");
            }

            uint audioChunkSize = (uint)(chunk[offset] |
                (chunk[offset + 1] << 8) |
                (chunk[offset + 2] << 16) |
                (chunk[offset + 3] << 24));

            if (audioChunkSize < 4 || audioChunkSize > chunk.Length - offset)
            {
                throw new InvalidDataException($"Frame audio chunk size is invalid for track {track}.");
            }

            int audioPayloadOffset = offset + 4;
            int audioPayloadSize = checked((int)audioChunkSize) - 4;
            if (_audioEnabled[track])
            {
                _audioBufferSizes[track] = (uint)AudioDecoder.Decode(
                    chunk.Slice(audioPayloadOffset, audioPayloadSize),
                    AudioTracks[track],
                    _audioBuffers[track]);
            }

            offset += checked((int)audioChunkSize);
        }

        if (VideoEnabled)
        {
            VideoDecoder.Decode(
                chunk[offset..],
                _container.VideoTrees,
                _container.Version,
                VideoInfo.Width,
                VideoInfo.Height,
                _videoFrame);
        }
    }
}
