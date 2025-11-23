namespace RemotePlay.Common;

/// <summary>
/// Network configuration settings shared between client and server
/// </summary>
public static class NetworkConfig
{
    /// <summary>
    /// Default port for controller input streaming
    /// </summary>
    public const int DEFAULT_PORT = 11000;
    
    /// <summary>
    /// Maximum number of controllers supported
    /// </summary>
    public const int MAX_CONTROLLERS = 4;
    
    /// <summary>
    /// Client timeout in milliseconds (no packets received)
    /// </summary>
    public const int CLIENT_TIMEOUT_MS = 5000;
    
    /// <summary>
    /// Controller polling interval in milliseconds (client-side)
    /// </summary>
    public const int POLLING_INTERVAL_MS = 8; // ~120 Hz
    
    /// <summary>
    /// Maximum packet size in bytes
    /// </summary>
    public const int MAX_PACKET_SIZE = 256;
    
    /// <summary>
    /// Send updates only when controller state changes
    /// Set to false to send at a fixed rate regardless of changes
    /// </summary>
    public const bool SEND_ON_CHANGE = false; // Changed to false for better responsiveness
}
