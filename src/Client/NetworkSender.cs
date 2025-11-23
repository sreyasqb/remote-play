using System;
using System.Net;
using System.Net.Sockets;
using RemotePlay.Common;

namespace RemotePlay.Client;

/// <summary>
/// Handles network communication - sends controller data to server via UDP
/// </summary>
public class NetworkSender : IDisposable
{
    private readonly UdpClient _udpClient;
    private readonly IPEndPoint _serverEndPoint;
    private bool _isDisposed = false;
    
    public NetworkSender(string serverIp, int port = NetworkConfig.DEFAULT_PORT)
    {
        _udpClient = new UdpClient();
        
        // Parse server IP
        if (!IPAddress.TryParse(serverIp, out IPAddress? address))
        {
            throw new ArgumentException($"Invalid IP address: {serverIp}", nameof(serverIp));
        }
        
        _serverEndPoint = new IPEndPoint(address, port);
        
        Console.WriteLine($"Network sender initialized. Target: {_serverEndPoint}");
    }
    
    /// <summary>
    /// Sends controller state to the server
    /// </summary>
    public void SendControllerState(ControllerState state)
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException(nameof(NetworkSender));
        }
        
        try
        {
            byte[] packet = PacketSerializer.Serialize(state);
            _udpClient.Send(packet, packet.Length, _serverEndPoint);
        }
        catch (SocketException ex)
        {
            Console.WriteLine($"Network error: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error sending data: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Send async (non-blocking)
    /// </summary>
    public async Task SendControllerStateAsync(ControllerState state)
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException(nameof(NetworkSender));
        }
        
        try
        {
            byte[] packet = PacketSerializer.Serialize(state);
            await _udpClient.SendAsync(packet, packet.Length, _serverEndPoint);
        }
        catch (SocketException ex)
        {
            Console.WriteLine($"Network error: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error sending data: {ex.Message}");
        }
    }
    
    public void Dispose()
    {
        if (!_isDisposed)
        {
            _udpClient?.Close();
            _udpClient?.Dispose();
            _isDisposed = true;
        }
    }
}
