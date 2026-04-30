namespace OnChat.Protocol.Codecs;

public interface ICodec<TValue>
{
    void Encode(BinaryWriter writer, TValue value);
    TValue Decode(BinaryReader reader);
}