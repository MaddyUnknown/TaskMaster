using System.Net;
using System.Net.Sockets;
using System.Text;

namespace TaskMaster.Test.UnitTests.Data;

/// <summary>
/// Minimal loopback HTTP server used to stub TaskMaster API endpoints during
/// library registration. The library's DI extensions call GET /api/auth/config
/// synchronously while registering services; this server answers that request
/// (and any other) with a canned JSON response.
/// </summary>
public sealed class TestApiServer : IDisposable
{
    private readonly TcpListener _listener;
    private readonly CancellationTokenSource _cts = new();
    private readonly byte[] _responseBytes;

    public string BaseUrl { get; }

    public TestApiServer(string responseBody = "{\"isSuccess\":true,\"data\":{\"mode\":\"none\"}}")
    {
        _listener = new TcpListener(IPAddress.Loopback, 0);
        _listener.Start();
        BaseUrl = $"http://127.0.0.1:{((IPEndPoint)_listener.LocalEndpoint).Port}/";

        var head = "HTTP/1.1 200 OK\r\nContent-Type: application/json\r\nConnection: close\r\n";
        var body = responseBody;
        _responseBytes = Encoding.ASCII.GetBytes(
            $"{head}Content-Length: {Encoding.UTF8.GetByteCount(body)}\r\n\r\n{body}");

        _ = Task.Run(() => AcceptLoopAsync(_cts.Token));
    }

    private async Task AcceptLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            TcpClient client;
            try
            {
                client = await _listener.AcceptTcpClientAsync(cancellationToken);
            }
            catch (Exception)
            {
                break;
            }

            _ = Task.Run(() => HandleClientAsync(client), cancellationToken);
        }
    }

    private async Task HandleClientAsync(TcpClient client)
    {
        using (client)
        {
            try
            {
                var stream = client.GetStream();
                var buffer = new byte[8192];
                var received = new StringBuilder();

                while (!received.ToString().Contains("\r\n\r\n"))
                {
                    var read = await stream.ReadAsync(buffer, _cts.Token);
                    if (read == 0) return;
                    received.Append(Encoding.ASCII.GetString(buffer, 0, read));
                }

                await stream.WriteAsync(_responseBytes, _cts.Token);
                await stream.FlushAsync(_cts.Token);
            }
            catch (Exception)
            {
                // Client disconnected or cancelled - nothing to do.
            }
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        try
        {
            _listener.Stop();
        }
        catch
        {
            // Ignore shutdown errors.
        }

        _cts.Dispose();
    }
}
