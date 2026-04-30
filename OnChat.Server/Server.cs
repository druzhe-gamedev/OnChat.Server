using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using OnChat.Protocol;
using OnChat.Protocol.Connection;
using Serilog;

namespace OnChat;

public class Server : IDisposable
{
    private TcpListener _listener;
    private readonly CancellationTokenSource _cts = new();
    private readonly ConcurrentDictionary<long, ChatConnection> _clients = new ();
    private readonly ILogger _logger = Log.Logger.ForContext<Server>();
    public readonly ChatProtocol Protocol = new();
    
    private bool _disposed;

    public async Task Start()
    {
        try
        {
            _listener = new(IPAddress.Parse("127.0.0.1"), 7596);
            _listener.Start();
        
            _logger.Information($"Start server on {_listener.LocalEndpoint}");

            await AcceptLoop();
        }
        finally
        {
            _listener.Stop();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private async Task AcceptLoop()
    {
        while(!_cts.Token.IsCancellationRequested)
        {
            if (!_listener.Pending()) continue;
                
            TcpClient client = await _listener.AcceptTcpClientAsync();
            _ = HandleConnection(client);
        }
    }

    private async Task HandleConnection(TcpClient client)
    {
        try
        {
            ChatConnection connection = new(client, this);

            if (!_clients.TryAdd(_clients.Count + 1, connection))
            {
                _logger.Error("Couldn't add client");
                return;
            }
            
            _logger.Information($"Connect client {client.Client.RemoteEndPoint}");

            Task readTask = connection.ReadLoop();
            
            await Task.WhenAll(readTask);
        }
        catch (Exception e)
        {
            _logger.Error(e.Message);
        }
        finally
        {
            client.Close();
        }
    }

    private void Dispose(bool disposing)
    {
        if (_disposed) return;
        
        if(disposing)
            _listener.Dispose();
            
        _disposed = true;
    }
}