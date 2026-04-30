namespace OnChat.Protocol.Codecs.Impl;

public class ByteCodec : ICodec<byte>
{
    public void Encode(BinaryWriter writer, byte value) => writer.Write(value);

    public byte Decode(BinaryReader reader) => reader.ReadByte();
}