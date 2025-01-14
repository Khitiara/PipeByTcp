using System.IO.Pipes;
using System.Net.Sockets;

namespace PipeByTcp;

public class ClientConnection : IDisposable, IAsyncDisposable
{
    private readonly Socket _client;
    private readonly NamedPipeServerStream _pipeServ;
    private readonly NetworkStream _stream;

    public ClientConnection(string pipe, Socket client)
    {
        _client = client;
        _pipeServ = new NamedPipeServerStream(pipe, PipeDirection.InOut, 1, PipeTransmissionMode.Byte,
            PipeOptions.Asynchronous);
        _stream = new NetworkStream(_client);
    }

    public async ValueTask RunAsync(CancellationToken cancellationToken)
    {
        await _pipeServ.WaitForConnectionAsync(cancellationToken);
        await Task.WhenAll(ReadAsync(cancellationToken), WriteAsync(cancellationToken));
    }

    private async Task WriteAsync(CancellationToken cancellationToken)
    {
        await _pipeServ.CopyToAsync(_stream, cancellationToken);
    }

    private async Task ReadAsync(CancellationToken cancellationToken)
    {
        await _pipeServ.CopyToAsync(_stream, cancellationToken);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _client.Dispose();
        _pipeServ.Dispose();
        _stream.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        _client.Dispose();
        await _pipeServ.DisposeAsync();
        await _stream.DisposeAsync();
    }
}