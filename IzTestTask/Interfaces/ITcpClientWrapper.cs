namespace IzTestTask.Interfaces;

public interface ITcpClientWrapper : IDisposable
{
    bool Connected { get; }

    Task ConnectAsync(string ipAddress, int port);
    Stream GetStream();
    void Close();
}