using OnChat.Shared;

namespace OnChat.Protocol;

public class ProtocolBuffer(MemoryStream stream) : IDisposable
{
    public BinaryReader Reader { get; } = new(stream);
    public BinaryWriter Writer { get; } = new(stream);
    public MemoryStream Stream => stream;
    private bool _disposed;
    
    public static async Task<ProtocolBuffer> CreateFromReader(BinaryReader reader)
    {
        (byte first, byte second) = (reader.ReadByte(), reader.ReadByte());
        
        if (first != PacketConstants.Signature[0] || second != PacketConstants.Signature[1])
            throw new BadPacketHeaderSignatureException("Bad packet header signature");

        // length + packet type (byte)
        int length = reader.ReadInt32() + 1;
        byte[] payload = new byte[length];
        await reader.BaseStream.ReadExactlyAsync(payload, 0, length);
        
        MemoryStream ms = new(payload);
        ms.Seek(0, SeekOrigin.Begin);
        
        ProtocolBuffer buffer = new(ms);

        return buffer;
    }

    public void Dispose(bool disposing)
    {
        if (!disposing || _disposed) return;
        
        stream.Dispose();
        Reader.Dispose();
        Writer.Dispose();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}

internal class BadPacketHeaderSignatureException(string? message) : Exception(message);