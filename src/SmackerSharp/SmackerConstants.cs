namespace SmackerSharp;

/// <summary>
/// Constants matching the public SmackerSharp C API.
/// </summary>
public static class SmackerConstants
{
    /// <summary>Mask for audio track 0.</summary>
    public const byte AudioTrack0 = 0x01;
    /// <summary>Mask for audio track 1.</summary>
    public const byte AudioTrack1 = 0x02;
    /// <summary>Mask for audio track 2.</summary>
    public const byte AudioTrack2 = 0x04;
    /// <summary>Mask for audio track 3.</summary>
    public const byte AudioTrack3 = 0x08;
    /// <summary>Mask for audio track 4.</summary>
    public const byte AudioTrack4 = 0x10;
    /// <summary>Mask for audio track 5.</summary>
    public const byte AudioTrack5 = 0x20;
    /// <summary>Mask for audio track 6.</summary>
    public const byte AudioTrack6 = 0x40;
    /// <summary>Mask for the video track.</summary>
    public const byte VideoTrack = 0x80;
}
