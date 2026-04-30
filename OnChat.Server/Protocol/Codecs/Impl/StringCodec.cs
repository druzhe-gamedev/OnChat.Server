namespace OnChat.Protocol.Codecs.Impl;

public class StringCodec : ICodec<string>
{
    public void Encode(BinaryWriter writer, string value) => writer.Write(value);

    public string Decode(BinaryReader reader) => reader.ReadString();
}