namespace IzTestTask
{
    using System.Net.Sockets;
    using System.Text;

    internal class NetSdrClient
    {
        private TcpClient? _tcpClient;
        private NetworkStream? _networkStream;

        public async Task ConnectAsync(string ipAddress, int port = 50000)
        {
            if (_tcpClient != null)
                throw new InvalidOperationException("Already connected to a receiver.");

            _tcpClient = new TcpClient();
            await _tcpClient.ConnectAsync(ipAddress, port);
            _networkStream = _tcpClient.GetStream();

            Console.WriteLine($"Connected to receiver at {ipAddress}:{port}");
        }

        public async Task DisconnectAsync()
        {
            if (_tcpClient == null)
                throw new InvalidOperationException("Not connected to any receiver.");

            await SendCommandAsync("DISCONNECT");
            _tcpClient.Close();
            _tcpClient = null;
            _networkStream = null;

            Console.WriteLine("Disconnected from receiver.");
        }

        public async Task StartReceiverAsync()
        {
            EnsureConnected();
            await SendCommandAsync("RECEIVER START");
        }

        public async Task StopReceiverAsync()
        {
            EnsureConnected();
            await SendCommandAsync("RECEIVER STOP");
        }

        public async Task SetFrequencyAsync(int frequency)
        {
            EnsureConnected();
            await SendCommandAsync($"SET FREQ {frequency}");
        }

        private async Task SendCommandAsync(string command)
        {
            if (_networkStream == null) throw new InvalidOperationException("Not connected to receiver.");

            var commandBytes = Encoding.ASCII.GetBytes(command + "\n");
            await _networkStream.WriteAsync(commandBytes.AsMemory(0, commandBytes.Length));

            byte[] buffer = new byte[1024];
            int bytesRead = await _networkStream.ReadAsync(buffer.AsMemory(0, buffer.Length));
            string response = Encoding.ASCII.GetString(buffer, 0, bytesRead);

            if (response.StartsWith("NAK"))
                throw new InvalidOperationException($"Command failed: {response}");

            Console.WriteLine($"Command '{command}' executed successfully.");
        }

        private void EnsureConnected()
        {
            if (_tcpClient == null || !_tcpClient.Connected)
                throw new InvalidOperationException("Not connected to receiver.");
        }
    }
}
