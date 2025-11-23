using System;
using System.Net;
using System.Net.Sockets;
using RemotePlay.Common;

namespace RemotePlay.Client;

/// <summary>
/// Handles network communication - sends controller data to server via UDP
/// Optimized for MINIMUM latency with synchronous sending and no buffering
/// </summary>
public class NetworkSender : IDisposable
{
    private readonly Socket _socket;
    private readonly EndPoint _serverEndPoint;
    private bool _isDisposed = false;
    
    public NetworkSender(string serverIp, int port = NetworkConfig.DEFAULT_PORT)
    {
        // Parse server IP
        if (!IPAddress.TryParse(serverIp, out IPAddress? address))
        {
            throw new ArgumentException($"Invalid IP address: {serverIp}", nameof(serverIp));
        }
        
        _serverEndPoint = new IPEndPoint(address, port);
        
        // Create raw UDP socket for maximum performance
        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        
        // CRITICAL: Disable all buffering and delays for minimum latency
        _socket.SendBufferSize = 0;  // No OS-level send buffering
        _socket.Blocking = true;      // Synchronous for immediate sending
        _socket.DontFragment = true;  // Don't fragment packets
        
        // Connect to server endpoint (for UDP, this just sets the default destination)
        _socket.Connect(_serverEndPoint);
        
        Console.WriteLine($"Network sender initialized. Target: {_serverEndPoint}");
        Console.WriteLine($"  Low-latency mode: Synchronous, zero buffering");
    }
    
    /// <summary>
    /// Sends controller state to the server IMMEDIATELY (synchronous, no buffering)
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
            
            // Synchronous send - blocks until packet is sent (microseconds)
            _socket.Send(packet, packet.Length, SocketFlags.None);
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
    /// Send async (for compatibility, but actually calls synchronous send)
    /// </summary>
    public Task SendControllerStateAsync(ControllerState state)
    {
        // Just call synchronous version - it's already fast enough
        SendControllerState(state);
        return Task.CompletedTask;
    }
    
    public void Dispose()
    {
        if (!_isDisposed)
        {
            _socket?.Close();
            _socket?.Dispose();
            _isDisposed = true;
        }
    }
}
