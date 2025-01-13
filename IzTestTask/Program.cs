using BenchmarkDotNet.Running;
using IzTestTask.Benchmarks;
using IzTestTask.Enums;
using IzTestTask.Exceptions;
using System.Net.Sockets;
using IzTestTask.Core;
using IzTestTask.Constants;

namespace IzTestTask;

public class Program
{
    public static async Task Main(string[] args)
    {
        if (args.Length > 0 && args[0].Equals("benchmark", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("Running benchmarks...");
            BenchmarkRunner.Run<NetSdrBenchmarks>();
            return;
        }

        Console.WriteLine("Starting NetSDR interaction demo...");

        const string ipAddress = "127.0.0.1";
        const string iqOutputPath = "iq_samples.bin";

        using var tcpClientWrapper = new TcpClientWrapper();
        using var client = new NetSdrClient(tcpClientWrapper);
        using var receiver = new IqDataReceiver(iqOutputPath);
        using var cts = new CancellationTokenSource();

        try
        {
            await client.ConnectAsync(ipAddress, ProtocolConstants.Ports.DefaultTcp).ConfigureAwait(false);
            Console.WriteLine($"Connected to receiver at {ipAddress}");

            // Set frequency to 144 MHz
            const uint frequency = 144_000_000;
            await client.SetFrequencyAsync(frequency).ConfigureAwait(false);
            Console.WriteLine($"Frequency set to {frequency / 1_000_000} MHz");

            // Start the receiver
            await client.SetReceiverStateAsync(ReceiverStateEnum.Start).ConfigureAwait(false);
            Console.WriteLine("Receiver started");

            Console.WriteLine("\nStarting IQ data collection...");
            Console.WriteLine("Press Enter to stop recording.");
            var listeningTask = receiver.StartListeningAsync(cts.Token);

            Console.ReadLine();

            // Stop the data collection
            try
            {
                await cts.CancelAsync();
                await listeningTask.ConfigureAwait(false);
                Console.WriteLine("IQ data collection stopped");
            }
            catch (OperationCanceledException)
            {
                // Expected cancellation
            }

            // Stop the receiver
            await client.SetReceiverStateAsync(ReceiverStateEnum.Stop).ConfigureAwait(false);
            Console.WriteLine("Receiver stopped");

            // Disconnect
            await client.DisconnectAsync().ConfigureAwait(false);
            Console.WriteLine("Disconnected from receiver");

            if (File.Exists(iqOutputPath))
            {
                var fileInfo = new FileInfo(iqOutputPath);
                Console.WriteLine($"\nIQ samples have been saved to {iqOutputPath}");
                Console.WriteLine($"File size: {fileInfo.Length / 1024.0:F2} KB");
            }
        }
        catch (NetSdrException ex)
        {
            await Console.Error.WriteLineAsync($"NetSDR protocol error: {ex.Message}").ConfigureAwait(false);
            Environment.ExitCode = 1;
        }
        catch (SocketException ex)
        {
            await Console.Error.WriteLineAsync($"Network error: {ex.Message}").ConfigureAwait(false);
            Environment.ExitCode = 1;
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync($"Unexpected error: {ex.Message}").ConfigureAwait(false);
            Environment.ExitCode = 1;
        }
    }
}