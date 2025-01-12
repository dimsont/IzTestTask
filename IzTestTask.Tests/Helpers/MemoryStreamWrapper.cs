using System.Net.Sockets;

namespace IzTestTask.Tests.Helpers;

public class MemoryStreamWrapper(MemoryStream innerStream, List<byte[]> sentMessages)
    : NetworkStream(new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp), FileAccess.ReadWrite)
{
    public override async Task WriteAsync(byte[] buffer, int offset, int size, CancellationToken cancellationToken)
    {
        var data = new byte[size];
        Array.Copy(buffer, offset, data, 0, size);
        sentMessages.Add(data);
        await innerStream.WriteAsync(buffer, offset, size, cancellationToken);
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int size, CancellationToken cancellationToken)
    {
        return await innerStream.ReadAsync(buffer, offset, size, cancellationToken);
    }
}
