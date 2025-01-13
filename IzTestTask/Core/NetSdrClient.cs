using IzTestTask.Constants;
using IzTestTask.Exceptions;
using IzTestTask.Interfaces;
using System.Buffers.Binary;
using IzTestTask.Enums;
using IzTestTask.Models;

namespace IzTestTask.Core;

/// <summary>
/// Implements the NetSDR protocol client for controlling SDR receivers.
/// </summary>
public class NetSdrClient : INetSdrClient
{
    private readonly ITcpClientWrapper _tcpClientWrapper;
    private Stream? _networkStream;

    /// <summary>
    /// Initializes a new instance of the NetSDR client.
    /// </summary>
    /// <param name="tcpClientWrapper">TCP client wrapper for network communications.</param>
    public NetSdrClient(ITcpClientWrapper tcpClientWrapper)
    {
        _tcpClientWrapper = tcpClientWrapper ?? throw new ArgumentNullException(nameof(tcpClientWrapper));
    }

    /// <summary>
    /// Establishes a connection to the NetSDR receiver.
    /// </summary>
    /// <param name="ipAddress">IP address of the receiver.</param>
    /// <param name="port">Port number (default: 50000).</param>
    /// <exception cref="NetSdrException">Thrown when already connected or connection fails.</exception>
    public async Task ConnectAsync(string ipAddress, int port = ProtocolConstants.Ports.DefaultTcp)
    {
        if (_tcpClientWrapper.Connected)
        {
            throw new NetSdrException("Already connected to a receiver.");
        }

        await _tcpClientWrapper.ConnectAsync(ipAddress, port).ConfigureAwait(false);
        _networkStream = _tcpClientWrapper.GetStream();
    }

    /// <summary>
    /// Disconnects from the NetSDR receiver.
    /// </summary>
    /// <exception cref="NetSdrException">Thrown when not connected to a receiver.</exception>
    public Task DisconnectAsync()
    {
        if (!_tcpClientWrapper.Connected)
        {
            throw new NetSdrException("Not connected to any receiver.");
        }

        _tcpClientWrapper.Close();
        _networkStream = null;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Sets the receiver state (Start/Stop).
    /// </summary>
    /// <param name="state">The desired receiver state.</param>
    /// <exception cref="NetSdrException">Thrown when not connected or command fails.</exception>
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

    /// <summary>
    /// Sets the receiver frequency.
    /// </summary>
    /// <param name="frequency">The desired frequency in Hz.</param>
    /// <exception cref="NetSdrException">Thrown when not connected or command fails.</exception>
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
        if (!_tcpClientWrapper.Connected)
        {
            throw new NetSdrException("Not connected to receiver.");
        }
    }

    /// <summary>
    /// Releases all resources used by the NetSDR client.
    /// </summary>
    public void Dispose()
    {
        _tcpClientWrapper.Dispose();
        _networkStream?.Dispose();
        GC.SuppressFinalize(this);
    }
}