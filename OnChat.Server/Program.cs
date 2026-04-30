using OnChat;
using Serilog;

Log.Logger = new LoggerConfiguration().WriteTo.Console(
    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}"
).CreateLogger();

Server server = new();
await server.Start();