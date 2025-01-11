using System.Net;
using System.Net.Sockets;
using Moq;
using Xunit;

namespace IzTestTask.Tests
{
    public class IqDataReceiverTests
    {
        private Mock<UdpClient> _udpClientMock;
        private IqDataReceiver _receiver;

        public IqDataReceiverTests()
        {
            _udpClientMock = new Mock<UdpClient>();
            _receiver = new IqDataReceiver();
        }

        [Fact]
        public async Task StartListeningAsync_ShouldWriteReceivedDataToFile()
        {
            // Arrange
            var testFilePath = "test_iq_data.bin";
            var testData = new byte[] { 0x01, 0x02, 0x03, 0x04 };

            // Mock UDP client to return test data
            _udpClientMock.Setup(client => client.ReceiveAsync())
                          .ReturnsAsync(new UdpReceiveResult(testData, new IPEndPoint(IPAddress.Loopback, 60000)));

            // Act
            var cts = new CancellationTokenSource();
            cts.CancelAfter(100); // Stop after a short delay for test purposes
            await _receiver.StartListeningAsync(testFilePath, cts.Token);

            // Assert
            Assert.True(File.Exists(testFilePath));
            var fileData = await File.ReadAllBytesAsync(testFilePath);
            Assert.Equal(testData, fileData);

            // Cleanup
            File.Delete(testFilePath);
        }
    }

}
