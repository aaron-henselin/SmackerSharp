namespace SmackerSharp;

public sealed record SmackerInfo(uint CurrentFrame, uint FrameCount, double MicrosecondsPerFrame);

public sealed record SmackerVideoInfo(uint Width, uint Height, SmackerYScaleMode YScaleMode);

public sealed record SmackerAudioTrackInfo(bool Exists, byte Channels, byte BitDepth, uint Rate, SmackerAudioCompression Compression);
