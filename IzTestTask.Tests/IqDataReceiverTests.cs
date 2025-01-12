using System.Net.Sockets;
using IzTestTask.Core;

namespace IzTestTask.Tests;

public class IqDataReceiverTests : IDisposable
{
    private const string TestFilePath = "test_iq_data.bin";
    private readonly IqDataReceiver _receiver = new(TestFilePath);

    [Fact]
    public async Task StartListening_ShouldCreateFile()
    {
        // Arrange
        using var cts = new CancellationTokenSource(100); // Short timeout for test

        // Act
        var listeningTask = _receiver.StartListeningAsync(cts.Token);
        await Task.Delay(50); // Give some time for the file to be created

        // Assert
        Assert.True(File.Exists(TestFilePath));
    }

    [Theory]
    [InlineData(60000)]  // Default port
    [InlineData(60001)]  // Different port to test port binding
    public void Constructor_ShouldCreateUdpListener_OnSpecifiedPort(int port)
    {
        // Act & Assert
        Assert.Throws<SocketException>(() =>
        {
            using var receiver1 = new IqDataReceiver(TestFilePath, port);
            using var receiver2 = new IqDataReceiver(TestFilePath, port);
            // Second receiver should fail to bind to the same port
        });
    }

    public void Dispose()
    {
        _receiver.Dispose();
        if (File.Exists(TestFilePath))
        {
            File.Delete(TestFilePath);
        }
    }
}