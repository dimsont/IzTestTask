using System.Net.Sockets;
using IzTestTask.Constants;
using IzTestTask.Exceptions;
using IzTestTask.Interfaces;

namespace IzTestTask.Core;

/// <summary>
/// Handles receiving and storing IQ data from the NetSDR receiver.
/// </summary>
public class IqDataReceiver : IIqDataReceiver
{
    private readonly UdpClient _udpClient;
    private readonly FileStream _fileStream;

    /// <summary>
    /// Initializes a new instance of the IQ data receiver.
    /// </summary>
    /// <param name="outputPath">Path where IQ data will be stored.</param>
    /// <param name="port">UDP port to listen on (default: 60000).</param>
    public IqDataReceiver(string outputPath, int port = ProtocolConstants.Ports.DefaultUdp)
    {
        _udpClient = new UdpClient(port);
        _fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
    }

    /// <summary>
    /// Starts listening for IQ data from the receiver.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the listening operation.</param>
    /// <exception cref="NetSdrException">Thrown when received data is malformed.</exception>
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

    /// <summary>
    /// Releases all resources used by the IQ data receiver.
    /// </summary>
    public void Dispose()
    {
        _udpClient.Dispose();
        _fileStream.Dispose();
        GC.SuppressFinalize(this);
    }
}