using System;
using System.Runtime.InteropServices;

namespace RemotePlay.Common;

/// <summary>
/// Handles serialization and deserialization of ControllerState for network transmission
/// Uses binary format for minimal packet size
/// </summary>
public static class PacketSerializer
{
    // Packet format: [MAGIC][VERSION][CONTROLLER_STATE]
    private const uint MAGIC_NUMBER = 0x52504C59; // "RPLY" in hex
    private const byte PROTOCOL_VERSION = 1;
    
    // Calculate packet size (using static readonly because Marshal.SizeOf is not a compile-time constant)
    public static readonly int PACKET_SIZE = sizeof(uint) + sizeof(byte) + Marshal.SizeOf<ControllerState>();
    
    /// <summary>
    /// Serializes a ControllerState into a byte array for network transmission
    /// </summary>
    public static byte[] Serialize(ControllerState state)
    {
        byte[] packet = new byte[PACKET_SIZE];
        int offset = 0;
        
        // Write magic number
        BitConverter.GetBytes(MAGIC_NUMBER).CopyTo(packet, offset);
        offset += sizeof(uint);
        
        // Write protocol version
        packet[offset] = PROTOCOL_VERSION;
        offset += sizeof(byte);
        
        // Write controller state
        int stateSize = Marshal.SizeOf<ControllerState>();
        IntPtr ptr = Marshal.AllocHGlobal(stateSize);
        try
        {
            Marshal.StructureToPtr(state, ptr, false);
            Marshal.Copy(ptr, packet, offset, stateSize);
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }
        
        return packet;
    }
    
    /// <summary>
    /// Deserializes a byte array into a ControllerState
    /// Returns null if the packet is invalid
    /// </summary>
    public static ControllerState? Deserialize(byte[] packet)
    {
        if (packet == null || packet.Length < PACKET_SIZE)
        {
            return null;
        }
        
        int offset = 0;
        
        // Verify magic number
        uint magic = BitConverter.ToUInt32(packet, offset);
        offset += sizeof(uint);
        
        if (magic != MAGIC_NUMBER)
        {
            return null;
        }
        
        // Verify protocol version
        byte version = packet[offset];
        offset += sizeof(byte);
        
        if (version != PROTOCOL_VERSION)
        {
            return null;
        }
        
        // Deserialize controller state
        int stateSize = Marshal.SizeOf<ControllerState>();
        IntPtr ptr = Marshal.AllocHGlobal(stateSize);
        try
        {
            Marshal.Copy(packet, offset, ptr, stateSize);
            return Marshal.PtrToStructure<ControllerState>(ptr);
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }
    }
    
    /// <summary>
    /// Validates if a packet appears to be valid (quick check)
    /// </summary>
    public static bool IsValidPacket(byte[] packet)
    {
        if (packet == null || packet.Length < sizeof(uint) + sizeof(byte))
        {
            return false;
        }
        
        uint magic = BitConverter.ToUInt32(packet, 0);
        byte version = packet[sizeof(uint)];
        
        return magic == MAGIC_NUMBER && version == PROTOCOL_VERSION;
    }
}
