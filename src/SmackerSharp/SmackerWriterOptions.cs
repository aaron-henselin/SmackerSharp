namespace SmackerSharp;

public sealed record SmackerWriterOptions(
    uint Width,
    uint Height,
    double MicrosecondsPerFrame,
    char Version = '2');
