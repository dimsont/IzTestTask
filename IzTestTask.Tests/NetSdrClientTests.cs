using System.Buffers.Binary;
using System.Net.Sockets;
using IzTestTask.Constants;
using IzTestTask.Core;
using IzTestTask.Enums;
using IzTestTask.Exceptions;
using IzTestTask.Interfaces;
using Moq;

namespace IzTestTask.Tests;

public class NetSdrClientTests
{
    private readonly Mock<ITcpClientWrapper> _tcpClientWrapperMock;
    private readonly Mock<NetworkStream> _networkStreamMock;
    private readonly NetSdrClient _client;
    private readonly List<byte[]> _sentMessages;

    public NetSdrClientTests()
    {
        _tcpClientWrapperMock = new Mock<ITcpClientWrapper>();
        _networkStreamMock = new Mock<NetworkStream>(MockBehavior.Strict);
        _sentMessages = new List<byte[]>();

        // Setup mocked NetworkStream behavior
        _networkStreamMock
            .Setup(s => s.WriteAsync(It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()))
            .Callback<ReadOnlyMemory<byte>, CancellationToken>((data, _) =>
            {
                _sentMessages.Add(data.ToArray());
            })
            .Returns(ValueTask.CompletedTask);

        _tcpClientWrapperMock.Setup(c => c.GetStream()).Returns(_networkStreamMock.Object);
        _tcpClientWrapperMock.SetupGet(c => c.Connected).Returns(true);

        _client = new NetSdrClient(_tcpClientWrapperMock.Object);
    }

    private void SetupSuccessResponse()
    {
        var ackResponse = new byte[] { ProtocolConstants.AckResponse };
        _networkStreamMock
            .Setup(s => s.ReadAsync(It.IsAny<Memory<byte>>(), It.IsAny<CancellationToken>()))
            .Returns(async (Memory<byte> buffer, CancellationToken _) =>
            {
                ackResponse.CopyTo(buffer);
                return ackResponse.Length;
            });
    }

    private void SetupNakResponse()
    {
        var nakResponse = new byte[] { ProtocolConstants.NakResponse };
        _networkStreamMock
            .Setup(s => s.ReadAsync(It.IsAny<Memory<byte>>(), It.IsAny<CancellationToken>()))
            .Returns(async (Memory<byte> buffer, CancellationToken _) =>
            {
                nakResponse.CopyTo(buffer);
                return nakResponse.Length;
            });
    }

    [Fact]
    public async Task ConnectAsync_ShouldEstablishConnection()
    {
        // Arrange
        const string ipAddress = "127.0.0.1";
        const int port = ProtocolConstants.Ports.DefaultTcp;

        // Act
        await _client.ConnectAsync(ipAddress, port);

        // Assert
        _tcpClientWrapperMock.Verify(c => c.ConnectAsync(ipAddress, port), Times.Once);
    }

    [Fact]
    public async Task SetReceiverState_Start_ShouldSendCorrectMessage()
    {
        // Arrange
        SetupSuccessResponse();
        await _client.ConnectAsync("127.0.0.1");

        // Act
        await _client.SetReceiverStateAsync(ReceiverStateEnum.Start);

        // Assert
        var lastMessage = _sentMessages.Last();
        Assert.Equal(ProtocolConstants.StartCode, lastMessage[0]);
        Assert.Equal(ProtocolConstants.ControlItems.ReceiverState, lastMessage[3]);
        Assert.Equal((byte)ReceiverStateEnum.Start, lastMessage[5]);
        Assert.Equal(ProtocolConstants.EndCode, lastMessage[^1]);
    }

    [Fact]
    public async Task SetFrequency_ShouldSendCorrectMessage()
    {
        // Arrange
        SetupSuccessResponse();
        await _client.ConnectAsync("127.0.0.1");
        const uint frequency = 144_000_000;

        // Act
        await _client.SetFrequencyAsync(frequency);

        // Assert
        var lastMessage = _sentMessages.Last();
        Assert.Equal(ProtocolConstants.StartCode, lastMessage[0]);
        Assert.Equal(ProtocolConstants.ControlItems.ReceiverFrequency, lastMessage[3]);

        var frequencyBytes = new byte[4];
        Array.Copy(lastMessage, 5, frequencyBytes, 0, 4);
        var sentFrequency = BinaryPrimitives.ReadUInt32LittleEndian(frequencyBytes);
        Assert.Equal(frequency, sentFrequency);
    }

    [Fact]
    public async Task Command_WhenReceivesNak_ShouldThrowException()
    {
        // Arrange
        SetupNakResponse();
        await _client.ConnectAsync("127.0.0.1");

        // Act & Assert
        await Assert.ThrowsAsync<NetSdrException>(() =>
            _client.SetReceiverStateAsync(ReceiverStateEnum.Start));
    }

    [Fact]
    public async Task Commands_WhenNotConnected_ShouldThrowException()
    {
        // Arrange
        _tcpClientWrapperMock.SetupGet(c => c.Connected).Returns(false);

        // Act & Assert
        await Assert.ThrowsAsync<NetSdrException>(() =>
            _client.SetReceiverStateAsync(ReceiverStateEnum.Start));
    }
}
