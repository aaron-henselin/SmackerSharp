namespace SmackerSharp;

/// <summary>
/// Configuration options for the SmackerWriter.
/// </summary>
/// <param name="Width">The target width of the video in pixels.</param>
/// <param name="Height">The target height of the video in pixels.</param>
/// <param name="MicrosecondsPerFrame">The frame duration in microseconds.</param>
/// <param name="Version">The Smacker container version to target ('2' or '4').</param>
public sealed record SmackerWriterOptions(
    uint Width,
    uint Height,
    double MicrosecondsPerFrame,
    char Version = '2');
