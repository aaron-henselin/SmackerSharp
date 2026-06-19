using System.Security.Cryptography;
using SmackerSharp;

if (args.Length < 1)
{
    Console.Error.WriteLine("Usage: SmackerSharp.Dump <file.smk> [--frames all|N[,N...]] [--audio]");
    return 2;
}

string path = args[0];
bool includeAudio = args.Contains("--audio", StringComparer.OrdinalIgnoreCase);
string frameSpec = GetOption(args, "--frames") ?? "0";

using SmackerReader reader = SmackerReader.Open(path);
reader.VideoEnabled = true;
for (int i = 0; i < reader.AudioTracks.Count; i++)
{
    if (includeAudio && reader.AudioTracks[i].Exists)
    {
        reader.SetAudioEnabled(i, true);
    }
}

Console.WriteLine($"file={Path.GetFullPath(path)}");
Console.WriteLine($"version={reader.Version}");
Console.WriteLine($"width={reader.VideoInfo.Width}");
Console.WriteLine($"height={reader.VideoInfo.Height}");
Console.WriteLine($"frames={reader.Info.FrameCount}");
Console.WriteLine($"usf={reader.Info.MicrosecondsPerFrame}");
Console.WriteLine($"yscale={reader.VideoInfo.YScaleMode}");

IEnumerable<uint> frames = ExpandFrames(frameSpec, reader.Info.FrameCount);
foreach (uint frame in frames)
{
    reader.SeekKeyframe(frame);
    while (reader.Info.CurrentFrame < frame)
    {
        SmackerFrameResult result = reader.Next();
        if (result == SmackerFrameResult.Done)
        {
            throw new InvalidOperationException($"Could not reach frame {frame}.");
        }
    }

    Console.WriteLine($"frame={frame}");
    Console.WriteLine($"palette_sha256={Hash(reader.PaletteRgb)}");
    Console.WriteLine($"palette_fnv1a64={Fnv1A64(reader.PaletteRgb):x16}");
    Console.WriteLine($"video_sha256={Hash(reader.VideoFrame8)}");
    Console.WriteLine($"video_fnv1a64={Fnv1A64(reader.VideoFrame8):x16}");

    if (includeAudio)
    {
        for (int track = 0; track < reader.AudioTracks.Count; track++)
        {
            if (!reader.AudioTracks[track].Exists)
            {
                continue;
            }

            ReadOnlySpan<byte> audio = reader.GetAudio(track);
            Console.WriteLine($"audio{track}_bytes={audio.Length}");
            Console.WriteLine($"audio{track}_sha256={Hash(audio)}");
            Console.WriteLine($"audio{track}_fnv1a64={Fnv1A64(audio):x16}");
        }
    }
}

return 0;

static string? GetOption(string[] args, string name)
{
    for (int i = 1; i < args.Length - 1; i++)
    {
        if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
        {
            return args[i + 1];
        }
    }

    return null;
}

static IEnumerable<uint> ExpandFrames(string spec, uint frameCount)
{
    if (string.Equals(spec, "all", StringComparison.OrdinalIgnoreCase))
    {
        for (uint i = 0; i < frameCount; i++)
        {
            yield return i;
        }

        yield break;
    }

    foreach (string part in spec.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
    {
        if (!uint.TryParse(part, out uint frame))
        {
            throw new ArgumentException($"Invalid frame number '{part}'.", nameof(spec));
        }

        if (frame >= frameCount)
        {
            throw new ArgumentOutOfRangeException(nameof(spec), frame, "Frame is outside the file.");
        }

        yield return frame;
    }
}

static string Hash(ReadOnlySpan<byte> data)
{
    byte[] hash = SHA256.HashData(data);
    return Convert.ToHexString(hash).ToLowerInvariant();
}

static ulong Fnv1A64(ReadOnlySpan<byte> data)
{
    const ulong offsetBasis = 14695981039346656037;
    const ulong prime = 1099511628211;

    ulong hash = offsetBasis;
    foreach (byte value in data)
    {
        hash ^= value;
        hash *= prime;
    }

    return hash;
}
