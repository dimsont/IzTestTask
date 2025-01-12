using BenchmarkDotNet.Running;
using IzTestTask.Benchmarks;
using IzTestTask.Enums;
using IzTestTask.Exceptions;
using System.Net.Sockets;
using IzTestTask.Core;

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

        var tcpClientWrapper = new TcpClientWrapper();
        using var client = new NetSdrClient(tcpClientWrapper);
        using var receiver = new IqDataReceiver(iqOutputPath);
        using var cts = new CancellationTokenSource();

        try
        {
            await client.ConnectAsync(ipAddress).ConfigureAwait(false);
            Console.WriteLine($"Connected to receiver at {ipAddress}");

            // Set frequency to 144 MHz
            await client.SetFrequencyAsync(144_000_000).ConfigureAwait(false);
            Console.WriteLine("Frequency set to 144 MHz");

            // Start the receiver
            await client.SetReceiverStateAsync(ReceiverStateEnum.Start).ConfigureAwait(false);
            Console.WriteLine("Receiver started");

            Console.WriteLine("Starting IQ data collection. Press Enter to stop...");
            var listeningTask = receiver.StartListeningAsync(cts.Token);

            // Wait for user input
            Console.ReadLine();

            // Stop the data collection
            await cts.CancelAsync();
            await listeningTask.ConfigureAwait(false);
            Console.WriteLine("IQ data collection stopped");

            // Stop the receiver
            await client.SetReceiverStateAsync(ReceiverStateEnum.Stop).ConfigureAwait(false);
            Console.WriteLine("Receiver stopped");

            // Disconnect
            await client.DisconnectAsync().ConfigureAwait(false);
            Console.WriteLine("Disconnected from receiver");

            Console.WriteLine($"IQ samples have been saved to {iqOutputPath}");
        }
        catch (NetSdrException ex)
        {
            await Console.Error.WriteLineAsync($"NetSDR protocol error: {ex.Message}").ConfigureAwait(false);
        }
        catch (SocketException ex)
        {
            await Console.Error.WriteLineAsync($"Network error: {ex.Message}").ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync($"Unexpected error: {ex.Message}").ConfigureAwait(false);
        }
    }
}