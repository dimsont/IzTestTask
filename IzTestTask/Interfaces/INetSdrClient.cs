using IzTestTask.Constants;
using IzTestTask.Enums;

namespace IzTestTask.Interfaces;

public interface INetSdrClient : IDisposable
{
    Task ConnectAsync(string ipAddress, int port = ProtocolConstants.Ports.DefaultTcp);
    Task DisconnectAsync();
    Task SetReceiverStateAsync(ReceiverStateEnum state);
    Task SetFrequencyAsync(uint frequency);
}