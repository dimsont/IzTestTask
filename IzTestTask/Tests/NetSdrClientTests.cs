using System.Net.Sockets;
using System.Text;
using Moq;
using Xunit;

namespace IzTestTask.Tests
{
    public class NetSdrClientTests
    {
        private Mock<TcpClient> _tcpClientMock;
        private Mock<NetworkStream> _networkStreamMock;
        private NetSdrClient _client;

        public NetSdrClientTests()
        {
            _tcpClientMock = new Mock<TcpClient>();
            _networkStreamMock = new Mock<NetworkStream>();
            _client = new NetSdrClient();
        }

        private void SetupNetworkStream(string response)
        {
            var responseData = Encoding.ASCII.GetBytes(response);

            // Setup ReadAsync to return the mock response
            _networkStreamMock.Setup(stream => stream.ReadAsync(It.IsAny<byte[]>(), 0, It.IsAny<int>(), default))
                              .Callback<byte[], int, int, CancellationToken>((buffer, offset, count, token) =>
                              {
                                  Array.Copy(responseData, 0, buffer, offset, responseData.Length);
                              })
                              .ReturnsAsync(responseData.Length);

            // Mock WriteAsync (no operation needed for testing)
            _networkStreamMock.Setup(stream => stream.WriteAsync(It.IsAny<byte[]>(), 0, It.IsAny<int>(), default))
                              .Returns(Task.CompletedTask);

            // Mock the TcpClient's GetStream method to return our mock NetworkStream
            _tcpClientMock.Setup(client => client.GetStream()).Returns(_networkStreamMock.Object);
        }

        [Fact]
        public async Task ConnectAsync_ShouldEstablishConnection()
        {
            // Arrange
            _tcpClientMock.Setup(client => client.ConnectAsync(It.IsAny<string>(), It.IsAny<int>()))
                          .Returns(Task.CompletedTask);

            // Inject mock TcpClient into NetSdrClient
            await _client.ConnectAsync("127.0.0.1");

            // Assert
            _tcpClientMock.Verify(client => client.ConnectAsync("127.0.0.1", 50000), Times.Once);
        }

        [Fact]
        public async Task DisconnectAsync_ShouldSendDisconnectCommand()
        {
            // Arrange
            SetupNetworkStream("ACK\n");
            await _client.ConnectAsync("127.0.0.1");

            // Act
            await _client.DisconnectAsync();

            // Assert
            _networkStreamMock.Verify(stream =>
                stream.WriteAsync(It.Is<byte[]>(data => Encoding.ASCII.GetString(data).Contains("DISCONNECT")), 0, It.IsAny<int>(), default),
                Times.Once);
        }

        [Fact]
        public async Task StartReceiverAsync_ShouldSendStartCommand()
        {
            // Arrange
            SetupNetworkStream("ACK\n");
            await _client.ConnectAsync("127.0.0.1");

            // Act
            await _client.StartReceiverAsync();

            // Assert
            _networkStreamMock.Verify(stream =>
                stream.WriteAsync(It.Is<byte[]>(data => Encoding.ASCII.GetString(data).Contains("RECEIVER START")), 0, It.IsAny<int>(), default),
                Times.Once);
        }

        [Fact]
        public async Task SetFrequencyAsync_ShouldSendFrequencyCommand()
        {
            // Arrange
            SetupNetworkStream("ACK\n");
            await _client.ConnectAsync("127.0.0.1");

            // Act
            await _client.SetFrequencyAsync(123456);

            // Assert
            _networkStreamMock.Verify(stream =>
                stream.WriteAsync(It.Is<byte[]>(data => Encoding.ASCII.GetString(data).Contains("SET FREQ 123456")), 0, It.IsAny<int>(), default),
                Times.Once);
        }

        [Fact]
        public async Task Commands_ShouldHandleNAKResponse()
        {
            // Arrange
            SetupNetworkStream("NAK\n");
            await _client.ConnectAsync("127.0.0.1");

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await _client.StartReceiverAsync());
        }
    }
}