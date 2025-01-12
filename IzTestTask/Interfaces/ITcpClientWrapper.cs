using System.Net.Sockets;

namespace IzTestTask.Interfaces;

public interface ITcpClientWrapper : IDisposable
{
    bool Connected { get; }

    Task ConnectAsync(string ipAddress, int port);
    NetworkStream GetStream();
    void Close();
}