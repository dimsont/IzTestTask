using System.Buffers.Binary;
using IzTestTask.Constants;
using IzTestTask.Core;
using IzTestTask.Enums;
using IzTestTask.Exceptions;
using IzTestTask.Interfaces;
using IzTestTask.Tests.Helpers;
using Moq;

namespace IzTestTask.Tests;

public class NetSdrClientTests
{
    private readonly Mock<ITcpClientWrapper> _tcpClientWrapperMock;
    private readonly MemoryStream _memoryStream;
    private readonly NetSdrClient _client;
    private readonly List<byte[]> _sentMessages;

    public NetSdrClientTests(MemoryStream memoryStream, Mock<ITcpClientWrapper> tcpClientWrapperMock, NetSdrClient client)
    {
        _memoryStream = memoryStream;
        _tcpClientWrapperMock = new Mock<ITcpClientWrapper>();
        _sentMessages = [];

        // Use the FakeNetworkStream
        var fakeStream = new FakeNetworkStream();
        _tcpClientWrapperMock.Setup(c => c.GetStream()).Returns(fakeStream);
        _tcpClientWrapperMock.SetupGet(c => c.Connected).Returns(true);

        _client = new NetSdrClient(_tcpClientWrapperMock.Object);
    }

    public NetSdrClientTests(List<byte[]> sentMessages, Mock<ITcpClientWrapper> tcpClientWrapperMock, MemoryStream memoryStream, NetSdrClient client)
    {
        _sentMessages = sentMessages;
        _tcpClientWrapperMock = tcpClientWrapperMock;
        _memoryStream = memoryStream;
        _client = client;
    }

    private void SetupResponse(byte[] response)
    {
        _memoryStream.Write(response, 0, response.Length);
        _memoryStream.Position = 0; // Reset position for reading.
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
        SetupResponse([ProtocolConstants.AckResponse]);
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
        SetupResponse([ProtocolConstants.AckResponse]);
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
        SetupResponse([ProtocolConstants.NakResponse]);
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
