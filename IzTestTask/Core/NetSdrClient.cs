using IzTestTask.Constants;
using IzTestTask.Exceptions;
using IzTestTask.Interfaces;
using System.Buffers.Binary;
using IzTestTask.Enums;
using IzTestTask.Models;

namespace IzTestTask.Core;

public class NetSdrClient(ITcpClientWrapper tcpClientWrapper) : INetSdrClient
{
    private Stream? _networkStream;

    public async Task ConnectAsync(string ipAddress, int port = ProtocolConstants.Ports.DefaultTcp)
    {
        if (tcpClientWrapper.Connected)
        {
            throw new NetSdrException("Already connected to a receiver.");
        }

        await tcpClientWrapper.ConnectAsync(ipAddress, port).ConfigureAwait(false);
        _networkStream = tcpClientWrapper.GetStream();
    }

    public Task DisconnectAsync()
    {
        if (!tcpClientWrapper.Connected)
        {
            throw new NetSdrException("Not connected to any receiver.");
        }

        tcpClientWrapper.Close();
        _networkStream = null;
        return Task.CompletedTask;
    }

    public async Task SetReceiverStateAsync(ReceiverStateEnum state)
    {
        EnsureConnected();

        var message = new NetSdrMessage
        {
            ControlItemCode = ProtocolConstants.ControlItems.ReceiverState,
            Data = [(byte)state]
        };

        await SendMessageAsync(message).ConfigureAwait(false);
    }

    public async Task SetFrequencyAsync(uint frequency)
    {
        EnsureConnected();

        var frequencyBytes = new byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(frequencyBytes, frequency);

        var message = new NetSdrMessage
        {
            ControlItemCode = ProtocolConstants.ControlItems.ReceiverFrequency,
            Data = frequencyBytes
        };

        await SendMessageAsync(message).ConfigureAwait(false);
    }

    private async Task SendMessageAsync(NetSdrMessage message)
    {
        if (_networkStream == null)
        {
            throw new NetSdrException("Not connected to receiver.");
        }

        var completeMessage = message.ToByteArray();
        await _networkStream.WriteAsync(completeMessage).ConfigureAwait(false);

        var response = await ReadResponseAsync().ConfigureAwait(false);
        if (response[0] == ProtocolConstants.NakResponse)
        {
            throw new NetSdrException($"Command failed with NAK: {BitConverter.ToString(response)}");
        }
    }

    private async Task<byte[]> ReadResponseAsync()
    {
        if (_networkStream == null)
        {
            throw new NetSdrException("Not connected to receiver.");
        }

        var buffer = new byte[1024];
        var bytesRead = await _networkStream.ReadAsync(buffer.AsMemory(0, buffer.Length)).ConfigureAwait(false);
        return buffer[..bytesRead];
    }

    private void EnsureConnected()
    {
        if (!tcpClientWrapper.Connected)
        {
            throw new NetSdrException("Not connected to receiver.");
        }
    }

    public void Dispose()
    {
        tcpClientWrapper.Dispose();
        _networkStream?.Dispose();
        GC.SuppressFinalize(this);
    }
}
