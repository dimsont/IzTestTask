using System.Net.Sockets;

internal class IqDataReceiver
{
    private const int UdpPort = 60000;

    public async Task StartListeningAsync(string outputPath, CancellationToken cancellationToken)
    {
        try
        {
            using var udpClient = new UdpClient(UdpPort);
            using var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write);

            Console.WriteLine($"Listening for I/Q data on UDP port {UdpPort}...");

            while (!cancellationToken.IsCancellationRequested)
            {
                var result = await udpClient.ReceiveAsync(cancellationToken);
                await fileStream.WriteAsync(result.Buffer.AsMemory(0, result.Buffer.Length), cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Listening operation canceled.");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error while listening: {ex.Message}");
        }
    }
}


