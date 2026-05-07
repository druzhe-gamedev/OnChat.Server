using DataModel;
using LinqToDB;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OnChat;
using OnChat.Connection;
using OnChat.Encryption;
using OnChat.Shared.Messages;
using Serilog;

Log.Logger = new LoggerConfiguration().WriteTo.Console(
    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}"
).CreateLogger();

HostApplicationBuilder builder = Host.CreateApplicationBuilder();
builder.Configuration.AddJsonFile("appsettings.json");
builder.Services.AddSingleton<ConnectionsList>();
builder.Services.AddSerilog();
builder.Services.AddSingleton<JwtTokensService>();
builder.Services.AddSingleton(sp => new Server(
        sp.GetService<ConnectionsList>()!,
        sp,
        typeof(SendMessagePacket).Assembly
    )
);

builder.Services.AddScoped(provider =>
{
    var connectionString = builder.Configuration.GetConnectionString("Default");
    var options = new DataOptions<OnChatDb>(new DataOptions().UsePostgreSQL(connectionString));
    // var options = provider.GetRequiredService<DataOptions<OnChatDb>>();
    return new OnChatDb(options);
});

IHost app = builder.Build();
await app.StartAsync();

await app.Services.GetService<Server>()!.Start();