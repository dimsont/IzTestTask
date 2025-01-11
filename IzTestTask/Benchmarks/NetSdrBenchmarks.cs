using BenchmarkDotNet.Attributes;

namespace IzTestTask.Benchmarks
{
    [MemoryDiagnoser]
    public class NetSdrBenchmarks
    {
        private NetSdrClient _client = null!; // Initialized in GlobalSetup

        [GlobalSetup]
        public void Setup()
        {
            _client = new NetSdrClient();
        }

        [Benchmark]
        public async Task ConnectBenchmark()
        {
            await _client.ConnectAsync("127.0.0.1");
            await _client.DisconnectAsync();
        }

        [Benchmark]
        public async Task SetFrequencyBenchmark()
        {
            await _client.ConnectAsync("127.0.0.1");
            await _client.SetFrequencyAsync(1000000); // Example frequency: 1 MHz
            await _client.DisconnectAsync();
        }
    }
}
