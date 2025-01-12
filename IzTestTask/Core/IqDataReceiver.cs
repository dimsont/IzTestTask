using IzTestTask.Constants;
using IzTestTask.Exceptions;
using IzTestTask.Interfaces;
using System.Net.Sockets;

namespace IzTestTask.Core;

public class IqDataReceiver(string outputPath, int port = ProtocolConstants.Ports.DefaultUdp) : IIqDataReceiver
{
    private readonly UdpClient _udpClient = new(port);
    private readonly FileStream _fileStream = new(outputPath, FileMode.Create, FileAccess.Write);

    public async Task StartListeningAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var result = await _udpClient.ReceiveAsync(cancellationToken).ConfigureAwait(false);

                if (result.Buffer.Length % ProtocolConstants.IqData.SampleSize != 0)
                {
                    throw new NetSdrException("Received malformed IQ data");
                }

                await _fileStream.WriteAsync(result.Buffer, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // Normal cancellation, no need to handle
        }
    }

    public void Dispose()
    {
        _udpClient.Dispose();
        _fileStream.Dispose();
        GC.SuppressFinalize(this);
    }
}