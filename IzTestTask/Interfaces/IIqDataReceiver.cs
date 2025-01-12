namespace IzTestTask.Interfaces;

public interface IIqDataReceiver : IDisposable
{
    Task StartListeningAsync(CancellationToken cancellationToken);
}
