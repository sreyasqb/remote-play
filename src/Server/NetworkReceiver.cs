using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using RemotePlay.Common;

namespace RemotePlay.Server;

/// <summary>
/// Handles network communication - receives controller data from clients via UDP
/// </summary>
public class NetworkReceiver : IDisposable
{
    private readonly UdpClient _udpClient;
    private readonly VirtualControllerManager _controllerManager;
    private readonly Dictionary<byte, ClientInfo> _clientTracking;
    private bool _isDisposed = false;
    private bool _isRunning = false;
    
    public delegate void ControllerStateReceivedHandler(ControllerState state, IPEndPoint client);
    public event ControllerStateReceivedHandler? OnControllerStateReceived;
    
    private class ClientInfo
    {
        public IPEndPoint EndPoint { get; set; }
        public DateTime LastPacketTime { get; set; }
        public long PacketsReceived { get; set; }
        
        public ClientInfo(IPEndPoint endPoint)
        {
            EndPoint = endPoint;
            LastPacketTime = DateTime.Now;
            PacketsReceived = 0;
        }
    }
    
    public NetworkReceiver(VirtualControllerManager controllerManager, int port = NetworkConfig.DEFAULT_PORT)
    {
        _controllerManager = controllerManager ?? throw new ArgumentNullException(nameof(controllerManager));
        _clientTracking = new Dictionary<byte, ClientInfo>();
        
        try
        {
            _udpClient = new UdpClient(port);
            Console.WriteLine($"✓ UDP server listening on port {port}");
        }
        catch (SocketException ex)
        {
            Console.WriteLine($"ERROR: Failed to start UDP server: {ex.Message}");
            throw;
        }
    }
    
    /// <summary>
    /// Starts receiving controller data from clients
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException(nameof(NetworkReceiver));
        }
        
        _isRunning = true;
        Console.WriteLine("Network receiver started. Waiting for controller data...");
        Console.WriteLine();
        
        // Start timeout checker task
        var timeoutTask = Task.Run(() => CheckClientTimeouts(cancellationToken), cancellationToken);
        
        try
        {
            while (_isRunning && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // Receive data with cancellation support
                    var receiveTask = _udpClient.ReceiveAsync();
                    var completedTask = await Task.WhenAny(receiveTask, Task.Delay(100, cancellationToken));
                    
                    if (completedTask == receiveTask)
                    {
                        var result = await receiveTask;
                        ProcessPacket(result.Buffer, result.RemoteEndPoint);
                    }
                }
                catch (SocketException ex)
                {
                    if (_isRunning)
                    {
                        Console.WriteLine($"Network error: {ex.Message}");
                    }
                }
                catch (ObjectDisposedException)
                {
                    // UDP client was disposed
                    break;
                }
            }
        }
        finally
        {
            await timeoutTask;
        }
        
        Console.WriteLine("Network receiver stopped.");
    }
    
    /// <summary>
    /// Processes received packet
    /// </summary>
    private void ProcessPacket(byte[] data, IPEndPoint clientEndPoint)
    {
        // Validate packet
        if (!PacketSerializer.IsValidPacket(data))
        {
            Console.WriteLine($"Invalid packet received from {clientEndPoint}");
            return;
        }
        
        // Deserialize
        var state = PacketSerializer.Deserialize(data);
        if (state == null)
        {
            Console.WriteLine($"Failed to deserialize packet from {clientEndPoint}");
            return;
        }
        
        // Track client
        if (!_clientTracking.ContainsKey(state.Value.ControllerId))
        {
            _clientTracking[state.Value.ControllerId] = new ClientInfo(clientEndPoint);
            Console.WriteLine($"New client connected: {clientEndPoint} -> Controller #{state.Value.ControllerId}");
        }
        
        var clientInfo = _clientTracking[state.Value.ControllerId];
        clientInfo.LastPacketTime = DateTime.Now;
        clientInfo.PacketsReceived++;
        
        // Update virtual controller
        try
        {
            _controllerManager.UpdateController(state.Value);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating controller: {ex.Message}");
        }
        
        // Raise event
        OnControllerStateReceived?.Invoke(state.Value, clientEndPoint);
    }
    
    /// <summary>
    /// Periodically checks for client timeouts
    /// </summary>
    private async Task CheckClientTimeouts(CancellationToken cancellationToken)
    {
        while (_isRunning && !cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(1000, cancellationToken);
            
            var now = DateTime.Now;
            var timeoutIds = new List<byte>();
            
            foreach (var kvp in _clientTracking)
            {
                var timeSinceLastPacket = now - kvp.Value.LastPacketTime;
                if (timeSinceLastPacket.TotalMilliseconds > NetworkConfig.CLIENT_TIMEOUT_MS)
                {
                    timeoutIds.Add(kvp.Key);
                }
            }
            
            foreach (var controllerId in timeoutIds)
            {
                var clientInfo = _clientTracking[controllerId];
                Console.WriteLine($"Client timeout: {clientInfo.EndPoint} (Controller #{controllerId})");
                Console.WriteLine($"  Total packets received: {clientInfo.PacketsReceived}");
                
                // Reset controller to neutral
                _controllerManager.ResetController(controllerId);
                
                // Remove from tracking
                _clientTracking.Remove(controllerId);
            }
        }
    }
    
    /// <summary>
    /// Stops the receiver
    /// </summary>
    public void Stop()
    {
        _isRunning = false;
    }
    
    /// <summary>
    /// Gets statistics about connected clients
    /// </summary>
    public Dictionary<byte, (IPEndPoint EndPoint, long PacketsReceived)> GetClientStats()
    {
        var stats = new Dictionary<byte, (IPEndPoint, long)>();
        foreach (var kvp in _clientTracking)
        {
            stats[kvp.Key] = (kvp.Value.EndPoint, kvp.Value.PacketsReceived);
        }
        return stats;
    }
    
    public void Dispose()
    {
        if (!_isDisposed)
        {
            Stop();
            _udpClient?.Close();
            _udpClient?.Dispose();
            _isDisposed = true;
        }
    }
}
