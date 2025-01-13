using System.Net.Sockets;

namespace IzTestTask.Tests.Helpers;

public class MemoryStreamWrapper(Socket socket, List<byte[]> sentMessages)
    : NetworkStream(socket ?? CreateDummySocket(), FileAccess.ReadWrite)
{
    private readonly MemoryStream _innerStream = new();

    public List<byte[]> SentMessages { get; } = sentMessages ?? throw new ArgumentNullException(nameof(sentMessages));

    public override async Task WriteAsync(byte[] buffer, int offset, int size, CancellationToken cancellationToken)
    {
        var data = new byte[size];
        Array.Copy(buffer, offset, data, 0, size);
        SentMessages.Add(data);
        await _innerStream.WriteAsync(buffer, offset, size, cancellationToken);
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int size, CancellationToken cancellationToken)
    {
        return await _innerStream.ReadAsync(buffer, offset, size, cancellationToken);
    }

    private static Socket CreateDummySocket()
    {
        var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        socket.Bind(new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, 0)); // Bind to a dummy endpoint.
        return socket;
    }
}

