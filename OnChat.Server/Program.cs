using OnChat;
using OnChat.Protocol.Codecs;
using OnChat.Shared;
using Serilog;

Log.Logger = new LoggerConfiguration().WriteTo.Console(
    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}"
).CreateLogger();

Server server = new();

MessagePacket packet = new()
                       {
                           UserId = "druzhe",
                           Message = "hello"
                       };

MemoryStream ms = new();
BinaryWriter msW = new(ms);
BinaryReader msR = new(ms);

ICodec codec = server.Protocol.GetCodec(typeof(MessagePacket));
codec.Encode(msW, packet);
ms.Seek(0, SeekOrigin.Begin);

MessagePacket packetRead = (MessagePacket) codec.Decode(msR);

Log.Information($"{packetRead.UserId} {packetRead.Message}");

//await server.Start();