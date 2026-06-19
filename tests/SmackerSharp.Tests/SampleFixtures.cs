namespace SmackerSharp.Tests;

public static class SampleFixtures
{
    private const string DefaultHelloSmkPath = @"C:\Program Files (x86)\GOG Galaxy\Games\Dark Reign\dark\movies\HELLO.SMK";
    private const string DefaultRoundTripMp4Path = @"";
    private const string WingetFfmpegPath = @"";

    public static string? HelloSmkPath
    {
        get
        {
            string? configured = Environment.GetEnvironmentVariable("SmackerSharp_HELLO_SMK");
            if (!string.IsNullOrWhiteSpace(configured))
            {
                return configured;
            }

            return File.Exists(DefaultHelloSmkPath) ? DefaultHelloSmkPath : null;
        }
    }

    public static IReadOnlyList<string> SampleFiles
    {
        get
        {
            string? sampleDir = Environment.GetEnvironmentVariable("SmackerSharp_SAMPLE_DIR");
            if (string.IsNullOrWhiteSpace(sampleDir) || !Directory.Exists(sampleDir))
            {
                string? hello = HelloSmkPath;
                return hello is null ? Array.Empty<string>() : new[] { hello };
            }

            return Directory.EnumerateFiles(sampleDir, "*.smk", SearchOption.TopDirectoryOnly)
                .Concat(Directory.EnumerateFiles(sampleDir, "*.SMK", SearchOption.TopDirectoryOnly))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Order(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
    }

    public static string? RoundTripMp4Path
    {
        get
        {
            string? configured = Environment.GetEnvironmentVariable("SmackerSharp_ROUNDTRIP_MP4");
            if (!string.IsNullOrWhiteSpace(configured))
            {
                return configured;
            }

            return File.Exists(DefaultRoundTripMp4Path) ? DefaultRoundTripMp4Path : null;
        }
    }

    public static string? FfmpegPath
    {
        get
        {
            string? configured = Environment.GetEnvironmentVariable("SmackerSharp_FFMPEG");
            if (!string.IsNullOrWhiteSpace(configured))
            {
                return configured;
            }

            string? pathValue = Environment.GetEnvironmentVariable("PATH");
            if (!string.IsNullOrWhiteSpace(pathValue))
            {
                foreach (string directory in pathValue.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
                {
                    string candidate = Path.Combine(directory, "ffmpeg.exe");
                    if (File.Exists(candidate))
                    {
                        return candidate;
                    }
                }
            }

            return File.Exists(WingetFfmpegPath) ? WingetFfmpegPath : null;
        }
    }
}
