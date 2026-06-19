namespace SmackerSharp;

using SmackerSharp.Internal;

internal sealed class SmackerContainer
{
    public SmackerContainer(
        byte version,
        SmackerInfo info,
        SmackerVideoInfo videoInfo,
        SmackerAudioTrackInfo[] audioTracks,
        uint[] maxAudioBufferSizes,
        uint[] treeSizes,
        uint huffmanTreeChunkSize,
        bool hasRingFrame,
        uint[] chunkSizes,
        bool[] keyframes,
        byte[] frameTypes,
        ReadOnlyMemory<byte> huffmanTreeChunk,
        Huffman16[] videoTrees,
        ReadOnlyMemory<byte>[] frameChunks)
    {
        Version = version;
        Info = info;
        VideoInfo = videoInfo;
        AudioTracks = audioTracks;
        MaxAudioBufferSizes = maxAudioBufferSizes;
        TreeSizes = treeSizes;
        HuffmanTreeChunkSize = huffmanTreeChunkSize;
        HasRingFrame = hasRingFrame;
        ChunkSizes = chunkSizes;
        Keyframes = keyframes;
        FrameTypes = frameTypes;
        HuffmanTreeChunk = huffmanTreeChunk;
        VideoTrees = videoTrees;
        FrameChunks = frameChunks;
    }

    public byte Version { get; }

    public SmackerInfo Info { get; }

    public SmackerVideoInfo VideoInfo { get; }

    public SmackerAudioTrackInfo[] AudioTracks { get; }

    public uint[] MaxAudioBufferSizes { get; }

    public uint[] TreeSizes { get; }

    public uint HuffmanTreeChunkSize { get; }

    public bool HasRingFrame { get; }

    public uint[] ChunkSizes { get; }

    public bool[] Keyframes { get; }

    public byte[] FrameTypes { get; }

    public ReadOnlyMemory<byte> HuffmanTreeChunk { get; }

    public Huffman16[] VideoTrees { get; }

    public ReadOnlyMemory<byte>[] FrameChunks { get; }

    public int StoredFrameCount => ChunkSizes.Length;
}
