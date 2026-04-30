namespace OnChat.Protocol.Codecs.Impl;

public class Int32Codec : ICodec<int>
{
    public void Encode(BinaryWriter writer, int value) => writer.Write(value);

    public int Decode(BinaryReader reader) => reader.ReadInt32();
}