using System.Net.Sockets;
using IzTestTask.Core;

namespace IzTestTask.Tests;

public class IqDataReceiverTests : IDisposable
{
    private static readonly object _lock = new();
    private static int _nextPort = 60000;
    private static int _fileCounter = 0;
    private readonly string _testFilePath;
    private readonly IqDataReceiver _receiver;

    public IqDataReceiverTests()
    {
        // Create unique file path and port for each test instance
        lock (_lock)
        {
            _testFilePath = $"test_iq_data_{_fileCounter++}.bin";
            _receiver = new IqDataReceiver(_testFilePath, _nextPort++);
        }
    }

    [Fact]
    public async Task StartListening_ShouldCreateFile()
    {
        // Arrange
        using var cts = new CancellationTokenSource(100); // Short timeout for test

        // Act
        var listeningTask = _receiver.StartListeningAsync(cts.Token);
        await Task.Delay(50, cts.Token); // Give some time for the file to be created

        // Assert
        Assert.True(File.Exists(_testFilePath));
    }

    [Fact]
    public void Constructor_ShouldCreateUdpListener_OnSpecifiedPort()
    {
        // Arrange
        int testPort;
        string testFile;
        lock (_lock)
        {
            testPort = _nextPort++;
            testFile = $"test_iq_data_{_fileCounter++}.bin";
        }

        // Act & Assert
        using var receiver1 = new IqDataReceiver(testFile, testPort);
        Assert.ThrowsAny<Exception>(() =>
        {
            using var receiver2 = new IqDataReceiver(testFile + "_2", testPort);
        });
    }

    public void Dispose()
    {
        _receiver.Dispose();
        if (File.Exists(_testFilePath))
        {
            try
            {
                File.Delete(_testFilePath);
            }
            catch (IOException)
            {
                // File might still be in use, can be cleaned up later
            }
        }

        // Clean up any additional test files
        var testFiles = Directory.GetFiles(".", "test_iq_data_*.bin");
        foreach (var file in testFiles)
        {
            try
            {
                File.Delete(file);
            }
            catch (IOException)
            {
                // File might still be in use, can be cleaned up later
            }
        }
    }
}