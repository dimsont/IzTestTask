using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using IzTestTask.Constants;
using IzTestTask.Core;
using IzTestTask.Models;

namespace IzTestTask.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
public class NetSdrBenchmarks
{
    private NetSdrClient _client = null!;
    private NetSdrMessage _message = null!;
    private byte[] _mockIqData = null!;
    private IqDataReceiver _receiver = null!;
    private const string TestOutputPath = "benchmark_iq_data.bin";

    [GlobalSetup]
    public void Setup()
    {
        var tcpClientWrapper = new TcpClientWrapper();
        _client = new NetSdrClient(tcpClientWrapper);
        _message = new NetSdrMessage
        {
            ControlItemCode = ProtocolConstants.ControlItems.ReceiverFrequency,
            Data = new byte[] { 0x00, 0x00, 0x00, 0x00 }
        };

        // Create mock IQ data (1MB of samples)
        _mockIqData = new byte[1024 * 1024];
        Random.Shared.NextBytes(_mockIqData);

        _receiver = new IqDataReceiver(TestOutputPath);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _client.Dispose();
        _receiver.Dispose();
        if (File.Exists(TestOutputPath))
        {
            File.Delete(TestOutputPath);
        }
    }

    [Benchmark]
    public byte[] MessageEncoding()
    {
        return _message.ToByteArray();
    }

    [Benchmark]
    public async Task NetworkOperations()
    {
        await _client.ConnectAsync("127.0.0.1").ConfigureAwait(false);
        await _client.SetFrequencyAsync(144_000_000).ConfigureAwait(false);
        await _client.DisconnectAsync().ConfigureAwait(false);
    }

    [Benchmark]
    public async Task IqDataProcessing()
    {
        // Simulate processing 1MB of IQ data
        using var ms = new MemoryStream(_mockIqData);
        var buffer = new byte[ProtocolConstants.IqData.SampleSize];
        var processed = 0;

        while (processed < _mockIqData.Length)
        {
            await ms.ReadAsync(buffer).ConfigureAwait(false);
            // Process I/Q sample (2 bytes each)
            var iSample = BitConverter.ToInt16(buffer, 0);
            var qSample = BitConverter.ToInt16(buffer, 2);
            processed += ProtocolConstants.IqData.SampleSize;
        }
    }

    [Benchmark]
    [Arguments(1024)]
    [Arguments(4096)]
    [Arguments(8192)]
    public async Task IqDataWriting(int bufferSize)
    {
        // Benchmark different buffer sizes for writing IQ data
        using var ms = new MemoryStream();
        var buffer = new byte[bufferSize];
        var remaining = _mockIqData.Length;
        var position = 0;

        while (remaining > 0)
        {
            var count = Math.Min(bufferSize, remaining);
            Array.Copy(_mockIqData, position, buffer, 0, count);
            await ms.WriteAsync(buffer.AsMemory(0, count)).ConfigureAwait(false);
            position += count;
            remaining -= count;
        }
    }
}