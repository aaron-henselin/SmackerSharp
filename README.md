# SmackerSharp

**SmackerSharp** is a .NET/C# port of libsmacker for decoding RAD Smacker video (.smk) files. It is a fully managed, robust C# implementation designed for high-performance playback of retro video formats in modern game engines and tools.

## Features

- **High Performance**: Native C# implementation optimized with modern APIs (`ReadOnlySpan<byte>`, `IMemoryOwner<T>`).
- **Fully Managed Decoder**: Decode 8-bit palettized video frames and audio tracks.
- **Encoder Support**: Support for creating/writing Smacker files.
- **Multi-Track Audio**: Play back up to 7 parallel audio tracks.
- **Cross-Platform**: Targets `.NET 8.0` and can be run on Windows, macOS, and Linux.

---

## Project Structure

- **[src/SmackerSharp](file:///c:/src/libsmacker-csharp/src/SmackerSharp)**: Core decoder and encoder library.
- **[tests/SmackerSharp.Tests](file:///c:/src/libsmacker-csharp/tests/SmackerSharp.Tests)**: Comprehensive unit tests and file compatibility verifications.
- **[tools/SmackerSharp.Dump](file:///c:/src/libsmacker-csharp/tools/SmackerSharp.Dump)**: Command-line diagnostic tool to dump file headers, frames, and audio tracks.
- **[tools/SmackerSharp.RaylibPlayer](file:///c:/src/libsmacker-csharp/tools/SmackerSharp.RaylibPlayer)**: Real-time Smacker player demo implemented using Raylib.

---

## Building and Testing

Ensure you have the [.NET 8.0 SDK](https://dotnet.microsoft.com/download) installed.

### Build the entire solution:
```bash
dotnet build
```

### Run unit tests:
```bash
dotnet test
```

---

## Getting Started

### Decompressing a Smacker Video

Below is a basic example of opening a `.smk` video, reading video frame data, and extracting audio tracks:

```csharp
using System;
using SmackerSharp;

// Open the Smacker video file
using var reader = SmackerReader.Open("path/to/video.smk");

// Enable video and the first audio track
reader.SetEnabled(SmackerTrackMask.Video | SmackerTrackMask.Audio0);

// Advance to the first frame
var result = reader.First();

while (result != SmackerFrameResult.Done)
{
    // Access decoded 8-bit palettized video frame data
    ReadOnlySpan<byte> framePixels = reader.VideoFrame8;

    // Access the current palette (256 RGB entries, 768 bytes total)
    ReadOnlySpan<byte> paletteRgb = reader.PaletteRgb;

    // Retrieve audio bytes decoded for this frame on track 0
    ReadOnlySpan<byte> audioBytes = reader.GetAudio(0);

    // Process/render framePixels and play audioBytes here...

    // Advance to the next frame
    result = reader.Next();
}
```

---

## CLI Tools

### Dump Utility (`SmackerSharp.Dump`)
Dumps details and frame checksums of a Smacker file. Useful for debugging or checking frame integrity:
```bash
dotnet run --project tools/SmackerSharp.Dump/SmackerSharp.Dump.csproj -- <file.smk> [--frames all|N[,N...]] [--audio]
```

### Video Player (`SmackerSharp.RaylibPlayer`)
A real-time player to play back Smacker files with audio using Raylib:
```bash
dotnet run --project tools/SmackerSharp.RaylibPlayer/SmackerSharp.RaylibPlayer.csproj -- <file.smk>
```

---

## Attribution

**SmackerSharp** is based on/derived from the [libsmacker](https://sourceforge.net/projects/libsmacker/) C library, originally created by Greg Kennedy. We thank the original authors and contributors of `libsmacker` for their work on decoding this classic retro video format.

---

## License

This project is licensed under the GNU Lesser General Public License v2.1. See the [COPYING](file:///c:/src/libsmacker-csharp/COPYING) file for full license text.
