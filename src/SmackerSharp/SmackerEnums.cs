namespace SmackerSharp;

/// <summary>
/// Specifies the result of advancing to the next frame.
/// </summary>
public enum SmackerFrameResult
{
    /// <summary>
    /// Playback has finished and all frames have been processed.
    /// </summary>
    Done = 0x00,

    /// <summary>
    /// There are more frames available to decode.
    /// </summary>
    More = 0x01,

    /// <summary>
    /// This was the last frame in the video stream.
    /// </summary>
    Last = 0x02
}

/// <summary>
/// Specifies the stream open mode.
/// </summary>
public enum SmackerOpenMode
{
    /// <summary>
    /// Keep the file stream open on disk and stream chunks as needed.
    /// </summary>
    Disk = 0x00,

    /// <summary>
    /// Read the entire file into memory and process from memory.
    /// </summary>
    Memory = 0x01
}

/// <summary>
/// Specifies the vertical scaling mode of the video.
/// </summary>
public enum SmackerYScaleMode
{
    /// <summary>
    /// The video is not scaled.
    /// </summary>
    None = 0x00,

    /// <summary>
    /// The video is interlaced (double height, skipping alternate scan lines).
    /// </summary>
    Interlace = 0x01,

    /// <summary>
    /// The video height is doubled by repeating scan lines.
    /// </summary>
    Double = 0x02
}

/// <summary>
/// Specifies the audio compression format.
/// </summary>
public enum SmackerAudioCompression
{
    /// <summary>
    /// Raw uncompressed PCM audio.
    /// </summary>
    None = 0,

    /// <summary>
    /// Smacker delta PCM (DPCM) Huffman-compressed audio.
    /// </summary>
    SmackerDpcm = 1,

    /// <summary>
    /// Bink (perceptual) audio (unsupported).
    /// </summary>
    Bink = 2
}

/// <summary>
/// A flag mask for identifying which video and audio tracks are active or enabled.
/// </summary>
[Flags]
public enum SmackerTrackMask : byte
{
    /// <summary>
    /// No tracks enabled.
    /// </summary>
    None = 0x00,

    /// <summary>
    /// Audio track 0 enabled.
    /// </summary>
    AudioTrack0 = 0x01,

    /// <summary>
    /// Audio track 1 enabled.
    /// </summary>
    AudioTrack1 = 0x02,

    /// <summary>
    /// Audio track 2 enabled.
    /// </summary>
    AudioTrack2 = 0x04,

    /// <summary>
    /// Audio track 3 enabled.
    /// </summary>
    AudioTrack3 = 0x08,

    /// <summary>
    /// Audio track 4 enabled.
    /// </summary>
    AudioTrack4 = 0x10,

    /// <summary>
    /// Audio track 5 enabled.
    /// </summary>
    AudioTrack5 = 0x20,

    /// <summary>
    /// Audio track 6 enabled.
    /// </summary>
    AudioTrack6 = 0x40,

    /// <summary>
    /// Video track enabled.
    /// </summary>
    Video = 0x80,

    /// <summary>
    /// Enable all audio and video tracks.
    /// </summary>
    All = AudioTrack0 | AudioTrack1 | AudioTrack2 | AudioTrack3 | AudioTrack4 | AudioTrack5 | AudioTrack6 | Video
}
