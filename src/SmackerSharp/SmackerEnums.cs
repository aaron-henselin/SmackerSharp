namespace SmackerSharp;

public enum SmackerFrameResult
{
    Done = 0x00,
    More = 0x01,
    Last = 0x02
}

public enum SmackerOpenMode
{
    Disk = 0x00,
    Memory = 0x01
}

public enum SmackerYScaleMode
{
    None = 0x00,
    Interlace = 0x01,
    Double = 0x02
}

public enum SmackerAudioCompression
{
    None = 0,
    SmackerDpcm = 1,
    Bink = 2
}

[Flags]
public enum SmackerTrackMask : byte
{
    None = 0x00,
    AudioTrack0 = 0x01,
    AudioTrack1 = 0x02,
    AudioTrack2 = 0x04,
    AudioTrack3 = 0x08,
    AudioTrack4 = 0x10,
    AudioTrack5 = 0x20,
    AudioTrack6 = 0x40,
    Video = 0x80,
    All = AudioTrack0 | AudioTrack1 | AudioTrack2 | AudioTrack3 | AudioTrack4 | AudioTrack5 | AudioTrack6 | Video
}
