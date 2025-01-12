using System.Net.Sockets;
using IzTestTask.Interfaces;

namespace IzTestTask.Core;

public class TcpClientWrapper : ITcpClientWrapper
{
    private readonly TcpClient _tcpClient = new TcpClient();

    public bool Connected => _tcpClient.Connected;

    public async Task ConnectAsync(string ipAddress, int port)
    {
        await _tcpClient.ConnectAsync(ipAddress, port).ConfigureAwait(false);
    }

    public NetworkStream GetStream()
    {
        return _tcpClient.GetStream();
    }

    public void Close()
    {
        _tcpClient.Close();
    }

    public void Dispose()
    {
        _tcpClient.Dispose();
    }
}