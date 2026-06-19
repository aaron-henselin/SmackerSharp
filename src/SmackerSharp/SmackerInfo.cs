namespace SmackerSharp;

/// <summary>
/// Contains timing and frame counter information for a Smacker video.
/// </summary>
/// <param name="CurrentFrame">The 0-based index of the currently decoded frame.</param>
/// <param name="FrameCount">The total number of frames in the Smacker file.</param>
/// <param name="MicrosecondsPerFrame">The frame duration in microseconds.</param>
public sealed record SmackerInfo(uint CurrentFrame, uint FrameCount, double MicrosecondsPerFrame);

/// <summary>
/// Contains video dimension and scaling properties.
/// </summary>
/// <param name="Width">The width of the video in pixels.</param>
/// <param name="Height">The height of the video in pixels.</param>
/// <param name="YScaleMode">The vertical scaling/interlacing mode of the video.</param>
public sealed record SmackerVideoInfo(uint Width, uint Height, SmackerYScaleMode YScaleMode);

/// <summary>
/// Contains audio track properties retrieved from the Smacker file header.
/// </summary>
/// <param name="Exists">Indicates whether the audio track is present in the file.</param>
/// <param name="Channels">The number of channels (1 for mono, 2 for stereo).</param>
/// <param name="BitDepth">The audio bit depth (8 or 16 bits).</param>
/// <param name="Rate">The sample rate in Hz.</param>
/// <param name="Compression">The audio compression algorithm used (None, SmackerDpcm, or Bink).</param>
public sealed record SmackerAudioTrackInfo(bool Exists, byte Channels, byte BitDepth, uint Rate, SmackerAudioCompression Compression);
