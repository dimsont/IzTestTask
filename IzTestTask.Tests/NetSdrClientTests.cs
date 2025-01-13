using System.Buffers.Binary;
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
    private readonly MemoryStream _memoryStream;
    private readonly NetSdrClient _client;
    private readonly List<byte[]> _sentMessages;
    private bool _isConnected;

    public NetSdrClientTests()
    {
        _tcpClientWrapperMock = new Mock<ITcpClientWrapper>();
        _memoryStream = new MemoryStream();
        _sentMessages = [];
        _isConnected = false;

        // Setup connection state
        _tcpClientWrapperMock.SetupGet(c => c.Connected)
            .Returns(() => _isConnected);

        // Setup connect behavior
        _tcpClientWrapperMock.Setup(c => c.ConnectAsync(It.IsAny<string>(), It.IsAny<int>()))
            .Callback(() => _isConnected = true)
            .Returns(Task.CompletedTask);

        // Setup close behavior
        _tcpClientWrapperMock.Setup(c => c.Close())
            .Callback(() => _isConnected = false);

        // Setup stream mocking with proper response handling
        _tcpClientWrapperMock.Setup(c => c.GetStream())
            .Returns(() =>
            {
                var streamMock = new Mock<Stream>();
                streamMock
                    .Setup(s => s.WriteAsync(It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()))
                    .Callback<ReadOnlyMemory<byte>, CancellationToken>((data, _) =>
                    {
                        if (_isConnected)
                        {
                            _sentMessages.Add(data.ToArray());
                        }
                    })
                    .Returns(ValueTask.CompletedTask);

                streamMock
                    .Setup(s => s.ReadAsync(It.IsAny<Memory<byte>>(), It.IsAny<CancellationToken>()))
                    .Returns<Memory<byte>, CancellationToken>((buffer, _) =>
                    {
                        if (_memoryStream.Position < _memoryStream.Length)
                        {
                            return new ValueTask<int>(_memoryStream.Read(buffer.Span));
                        }
                        return new ValueTask<int>(0);
                    });

                streamMock.Setup(s => s.CanRead).Returns(true);
                streamMock.Setup(s => s.CanWrite).Returns(true);

                return streamMock.Object;
            });

        _client = new NetSdrClient(_tcpClientWrapperMock.Object);
    }

    private void SetupResponse(byte[] response)
    {
        _memoryStream.Position = 0;
        _memoryStream.SetLength(0);
        _memoryStream.Write(response, 0, response.Length);
        _memoryStream.Position = 0;
    }

    [Fact]
    public async Task ConnectAsync_ShouldEstablishConnection()
    {
        // Arrange
        SetupResponse([ProtocolConstants.AckResponse]);
        const string ipAddress = "127.0.0.1";
        const int port = ProtocolConstants.Ports.DefaultTcp;

        // Act
        await _client.ConnectAsync(ipAddress, port);

        // Assert
        _tcpClientWrapperMock.Verify(c => c.ConnectAsync(ipAddress, port), Times.Once);
        Assert.True(_isConnected);
    }

    [Fact]
    public async Task SetReceiverState_Start_ShouldSendCorrectMessage()
    {
        // Arrange
        SetupResponse([ProtocolConstants.AckResponse]);
        await _client.ConnectAsync("127.0.0.1");
        _sentMessages.Clear(); // Clear messages from connect

        // Act
        await _client.SetReceiverStateAsync(ReceiverStateEnum.Start);

        // Assert
        Assert.NotEmpty(_sentMessages);
        var lastMessage = _sentMessages[^1];
        Assert.True(lastMessage.Length >= 6, "Message too short");
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
        _sentMessages.Clear(); // Clear messages from connect
        const uint frequency = 144_000_000;

        // Act
        await _client.SetFrequencyAsync(frequency);

        // Assert
        Assert.NotEmpty(_sentMessages);
        var lastMessage = _sentMessages[^1];
        Assert.True(lastMessage.Length >= 9, "Message too short");
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
        SetupResponse([ProtocolConstants.AckResponse]); // For connect
        await _client.ConnectAsync("127.0.0.1");
        SetupResponse([ProtocolConstants.NakResponse]); // For command

        // Act & Assert
        await Assert.ThrowsAsync<NetSdrException>(() =>
            _client.SetReceiverStateAsync(ReceiverStateEnum.Start));
    }

    [Fact]
    public async Task Commands_WhenNotConnected_ShouldThrowException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<NetSdrException>(() =>
            _client.SetReceiverStateAsync(ReceiverStateEnum.Start));
    }
}