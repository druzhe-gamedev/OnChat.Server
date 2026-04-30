using System.Net.Sockets;
using System.Text;
using Serilog;

namespace OnChat;

public class ChatRecipient(TcpClient client) : IAsyncDisposable
{
    private readonly NetworkStream _stream = client.GetStream();
    private readonly ILogger _logger = Log.Logger.ForContext<ChatRecipient>();

    public async Task ReadLoop()
    {
        while (true)
        {
            if (!_stream.CanRead)
                continue;

            
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _stream.DisposeAsync();
    }
}