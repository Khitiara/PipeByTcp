// See https://aka.ms/new-console-template for more information

using System.Net;
using System.Net.Sockets;
using PipeByTcp;

Console.WriteLine("Hello, World!");
string? pipeName = null;
IPEndPoint? endPoint = null;
bool listen = false;
CancellationTokenSource cts = new();

Console.CancelKeyPress += (_, args) => {
    if (args.SpecialKey != ConsoleSpecialKey.ControlC) return;
    if (cts.IsCancellationRequested) return;
    args.Cancel = true;
    cts.Cancel();
};

foreach (string arg in args) {
    if (arg.StartsWith("--listen:")) {
        if (!int.TryParse(arg.AsSpan(9), out int port)) {
            PrintUsage();
            return 1;
        }

        endPoint = new IPEndPoint(IPAddress.Any, port);
        listen = true;
    } else if (arg.StartsWith("--connect:")) {
        if (!IPEndPoint.TryParse(args[1].AsSpan(10), out endPoint!)) {
            PrintUsage();
            return 1;
        }

        listen = false;
    } else if (arg.StartsWith("--pipe:")) {
        pipeName = arg[7..];
    } else {
        PrintUsage();
        return 1;
    }
}

if (endPoint == null) {
    PrintUsage();
    return 1;
}

if (string.IsNullOrWhiteSpace(pipeName)) {
    PrintUsage();
    return 1;
}

if (listen) {
    Socket listener = new(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
    while (!cts.IsCancellationRequested) {
        using Socket client = await listener.AcceptAsync(cts.Token);
        await using ClientConnection conn = new(pipeName, client);
        await conn.RunAsync(cts.Token);
    }
} else {
    using Socket client = new(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
    await client.ConnectAsync(endPoint, cts.Token);
    await using ClientConnection conn = new(pipeName, client);
    await conn.RunAsync(cts.Token);
}

return 0;

void PrintUsage()
{
    Console.WriteLine("USAGE {0} [--listen:<port>] [--connect:<address>] --pipe:<name>", args[0]);
}