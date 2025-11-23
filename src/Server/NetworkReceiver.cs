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
    private readonly Dictionary<string, ClientInfo> _clientTracking; // Track by IP:Port
    private bool _isDisposed = false;
    private bool _isRunning = false;
    
    public delegate void ControllerStateReceivedHandler(ControllerState state, IPEndPoint client);
    public event ControllerStateReceivedHandler? OnControllerStateReceived;
    
    private class ClientInfo
    {
        public IPEndPoint EndPoint { get; set; }
        public byte ControllerId { get; set; } // Assigned controller ID
        public DateTime LastPacketTime { get; set; }
        public long PacketsReceived { get; set; }
        
        public ClientInfo(IPEndPoint endPoint, byte controllerId)
        {
            EndPoint = endPoint;
            ControllerId = controllerId;
            LastPacketTime = DateTime.Now;
            PacketsReceived = 0;
        }
    }
    
    public NetworkReceiver(VirtualControllerManager controllerManager, int port = NetworkConfig.DEFAULT_PORT)
    {
        _controllerManager = controllerManager ?? throw new ArgumentNullException(nameof(controllerManager));
        _clientTracking = new Dictionary<string, ClientInfo>();
        
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
            Console.WriteLine($"Invalid packet received from {clientEndPoint} (size: {data.Length} bytes)");
            return;
        }
        
        // Deserialize
        var state = PacketSerializer.Deserialize(data);
        if (state == null)
        {
            Console.WriteLine($"Failed to deserialize packet from {clientEndPoint}");
            return;
        }
        
        // Track client by IP:Port
        string clientKey = clientEndPoint.ToString();
        byte controllerId = 0; // Default to controller 0 (can be extended for multi-controller)
        
        if (!_clientTracking.ContainsKey(clientKey))
        {
            _clientTracking[clientKey] = new ClientInfo(clientEndPoint, controllerId);
            Console.WriteLine($"New client connected: {clientEndPoint} -> Controller #{controllerId}");
        }
        
        var clientInfo = _clientTracking[clientKey];
        clientInfo.LastPacketTime = DateTime.Now;
        clientInfo.PacketsReceived++;
        controllerId = clientInfo.ControllerId;
        
        // Update virtual controller
        try
        {
            _controllerManager.UpdateController(state.Value, controllerId);
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
            var timeoutKeys = new List<string>();
            
            foreach (var kvp in _clientTracking)
            {
                var timeSinceLastPacket = now - kvp.Value.LastPacketTime;
                if (timeSinceLastPacket.TotalMilliseconds > NetworkConfig.CLIENT_TIMEOUT_MS)
                {
                    timeoutKeys.Add(kvp.Key);
                }
            }
            
            foreach (var clientKey in timeoutKeys)
            {
                var clientInfo = _clientTracking[clientKey];
                Console.WriteLine($"Client timeout: {clientInfo.EndPoint} (Controller #{clientInfo.ControllerId})");
                Console.WriteLine($"  Total packets received: {clientInfo.PacketsReceived}");
                
                // Reset controller to neutral
                _controllerManager.ResetController(clientInfo.ControllerId);
                
                // Remove from tracking
                _clientTracking.Remove(clientKey);
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
    public Dictionary<string, (IPEndPoint EndPoint, byte ControllerId, long PacketsReceived)> GetClientStats()
    {
        var stats = new Dictionary<string, (IPEndPoint, byte, long)>();
        foreach (var kvp in _clientTracking)
        {
            stats[kvp.Key] = (kvp.Value.EndPoint, kvp.Value.ControllerId, kvp.Value.PacketsReceived);
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
