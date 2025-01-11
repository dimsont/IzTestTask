using BenchmarkDotNet.Running;
using IzTestTask;
using IzTestTask.Benchmarks;

public class Program
{
    public static async Task Main(string[] args)
    {
        if (args.Length > 0 && args[0].Equals("benchmark", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("Running benchmarks...");
            BenchmarkRunner.Run<NetSdrBenchmarks>();
        }
        else
        {
            Console.WriteLine("Starting NetSDR interaction demo...");

            var receiver = new IqDataReceiver();
            var client = new NetSdrClient();

            try
            {
                var cts = new CancellationTokenSource();
                string ipAddress = "127.0.0.1";
                string iqOutputPath = "iq_samples.bin";

                await client.ConnectAsync(ipAddress);
                await client.SetFrequencyAsync(144000000); // Set frequency to 144 MHz
                await client.StartReceiverAsync();

                Console.WriteLine("Receiver started. Press Enter to stop.");
                var listeningTask = receiver.StartListeningAsync(iqOutputPath, cts.Token);

                Console.ReadLine();
                cts.Cancel();

                await listeningTask;
                await client.StopReceiverAsync();
                await client.DisconnectAsync();

                Console.WriteLine("Operation completed successfully.");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"An error occurred: {ex.Message}");
            }
        }
    }
}
