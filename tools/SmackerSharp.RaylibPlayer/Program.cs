using System.Numerics;
using SmackerSharp;
using Raylib_cs;

const string LogPath = @"C:\tmp\SmackerSharp-raylib-player.log";

try
{
    Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);
    File.WriteAllText(LogPath, $"Starting {DateTimeOffset.Now:u}{Environment.NewLine}");

    if (args.Length != 1)
    {
        Console.Error.WriteLine("Usage: SmackerSharp.RaylibPlayer <file.smk>");
        return 2;
    }

    string path = args[0];
    if (!File.Exists(path))
    {
        Console.Error.WriteLine($"File not found: {path}");
        return 2;
    }

    using SmackerReader smk = SmackerReader.Open(path);
    smk.VideoEnabled = true;

    int width = checked((int)smk.VideoInfo.Width);
    int height = checked((int)smk.VideoInfo.Height);
    uint frameCount = smk.Info.FrameCount;
    double secondsPerFrame = smk.Info.MicrosecondsPerFrame / 1_000_000.0;
    if (secondsPerFrame <= 0)
    {
        secondsPerFrame = 0.1;
    }

    File.AppendAllText(LogPath, $"Opened {path} {width}x{height} frames={smk.Info.FrameCount} usf={smk.Info.MicrosecondsPerFrame}{Environment.NewLine}");

    Raylib.SetConfigFlags(ConfigFlags.ResizableWindow);
    Raylib.InitWindow(width, height, $"{Path.GetFileName(path)} - SmackerSharp");
    Raylib.SetTargetFPS(60);

    Image image = Raylib.GenImageColor(width, height, Color.Black);
    Texture2D texture = Raylib.LoadTextureFromImage(image);
    Raylib.UnloadImage(image);

    byte[] rgba = new byte[checked(width * height * 4)];

    SmackerFrameResult frameResult = smk.First();
    UploadFrame(smk, rgba, texture);
    double accumulator = 0;
    bool paused = false;
    string? playbackError = null;
    string overlayText = $"1/{frameCount}";

    while (!Raylib.WindowShouldClose())
    {
        if (Raylib.IsKeyPressed(KeyboardKey.Space))
        {
            paused = !paused;
        }

        if (Raylib.IsKeyPressed(KeyboardKey.R))
        {
            frameResult = smk.First();
            UploadFrame(smk, rgba, texture);
            accumulator = 0;
            paused = false;
            playbackError = null;
            overlayText = $"1/{frameCount}";
        }

        if (!paused && playbackError is null)
        {
            accumulator += Raylib.GetFrameTime();
            if (accumulator > secondsPerFrame * 2)
            {
                accumulator = secondsPerFrame;
            }

            if (accumulator >= secondsPerFrame)
            {
                accumulator -= secondsPerFrame;

                try
                {
                    frameResult = smk.Next();
                    if (frameResult == SmackerFrameResult.Done)
                    {
                        frameResult = smk.First();
                    }

                    UploadFrame(smk, rgba, texture);
                    overlayText = $"{smk.Info.CurrentFrame + 1}/{frameCount}";
                }
                catch (Exception ex)
                {
                    playbackError = ex.Message;
                    paused = true;
                    File.AppendAllText(LogPath, ex + Environment.NewLine);
                    break;
                }

                if (frameResult == SmackerFrameResult.Last)
                {
                    break;
                }
            }
        }

        Raylib.BeginDrawing();
        Raylib.ClearBackground(Color.Black);
        DrawCentered(texture, width, height);
        Raylib.DrawText(playbackError ?? (paused ? "Paused" : overlayText), 8, 8, 20, Color.RayWhite);
        Raylib.EndDrawing();
    }

    Raylib.UnloadTexture(texture);
    Raylib.CloseWindow();
    File.AppendAllText(LogPath, $"Closed {DateTimeOffset.Now:u}{Environment.NewLine}");
    return 0;
}
catch (Exception ex)
{
    File.AppendAllText(LogPath, ex + Environment.NewLine);
    Console.Error.WriteLine(ex);
    return 1;
}

static unsafe void UploadFrame(SmackerReader smk, byte[] rgba, Texture2D texture)
{
    ReadOnlySpan<byte> frame = smk.VideoFrame8;
    ReadOnlySpan<byte> palette = smk.PaletteRgb;

    for (int i = 0, o = 0; i < frame.Length; i++, o += 4)
    {
        int paletteOffset = frame[i] * 3;
        rgba[o] = palette[paletteOffset];
        rgba[o + 1] = palette[paletteOffset + 1];
        rgba[o + 2] = palette[paletteOffset + 2];
        rgba[o + 3] = 255;
    }

    fixed (byte* pixels = rgba)
    {
        Raylib.UpdateTexture(texture, pixels);
    }
}

static void DrawCentered(Texture2D texture, int sourceWidth, int sourceHeight)
{
    int screenWidth = Raylib.GetScreenWidth();
    int screenHeight = Raylib.GetScreenHeight();
    float scale = MathF.Min(screenWidth / (float)sourceWidth, screenHeight / (float)sourceHeight);
    if (scale <= 0)
    {
        scale = 1;
    }

    float drawWidth = sourceWidth * scale;
    float drawHeight = sourceHeight * scale;
    Rectangle source = new(0, 0, sourceWidth, sourceHeight);
    Rectangle destination = new((screenWidth - drawWidth) / 2, (screenHeight - drawHeight) / 2, drawWidth, drawHeight);
    Raylib.DrawTexturePro(texture, source, destination, Vector2.Zero, 0, Color.White);
}
