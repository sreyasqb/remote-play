using System;
using System.Runtime.InteropServices;

namespace RemotePlay.Common;

/// <summary>
/// Ultra-fast serialization for 16-byte controller packets
/// Uses direct byte-level operations instead of Marshal for maximum performance
/// </summary>
public static class PacketSerializer
{
    // Exact packet size: 16 bytes (Parsec/Moonlight style)
    public const int PACKET_SIZE = 16;
    
    /// <summary>
    /// Serializes a ControllerState into a 16-byte array for network transmission
    /// Direct byte-level serialization for maximum performance
    /// </summary>
    public static byte[] Serialize(ControllerState state)
    {
        byte[] packet = new byte[PACKET_SIZE];
        
        // Offset 0-1: Header
        packet[0] = state.Flags;
        packet[1] = state.Sequence;
        
        // Offset 2-3: Buttons (little-endian)
        packet[2] = (byte)(state.Buttons & 0xFF);
        packet[3] = (byte)((state.Buttons >> 8) & 0xFF);
        
        // Offset 4-7: Analog sticks (8-bit signed)
        packet[4] = (byte)state.LeftStickX;
        packet[5] = (byte)state.LeftStickY;
        packet[6] = (byte)state.RightStickX;
        packet[7] = (byte)state.RightStickY;
        
        // Offset 8-9: Triggers
        packet[8] = state.LeftTrigger;
        packet[9] = state.RightTrigger;
        
        // Offset 10-15: Reserved (already zero-initialized)
        packet[10] = state.Reserved1;
        packet[11] = state.Reserved2;
        packet[12] = state.Reserved3;
        packet[13] = state.Reserved4;
        packet[14] = state.Reserved5;
        packet[15] = state.Reserved6;
        
        return packet;
    }
    
    /// <summary>
    /// Deserializes a 16-byte array into a ControllerState
    /// Returns null if the packet is invalid
    /// </summary>
    public static ControllerState? Deserialize(byte[] packet)
    {
        if (packet == null || packet.Length < PACKET_SIZE)
        {
            return null;
        }
        
        ControllerState state = new ControllerState();
        
        // Offset 0-1: Header
        state.Flags = packet[0];
        state.Sequence = packet[1];
        
        // Offset 2-3: Buttons (little-endian)
        state.Buttons = (ushort)(packet[2] | (packet[3] << 8));
        
        // Offset 4-7: Analog sticks (8-bit signed)
        state.LeftStickX = (sbyte)packet[4];
        state.LeftStickY = (sbyte)packet[5];
        state.RightStickX = (sbyte)packet[6];
        state.RightStickY = (sbyte)packet[7];
        
        // Offset 8-9: Triggers
        state.LeftTrigger = packet[8];
        state.RightTrigger = packet[9];
        
        // Offset 10-15: Reserved
        state.Reserved1 = packet[10];
        state.Reserved2 = packet[11];
        state.Reserved3 = packet[12];
        state.Reserved4 = packet[13];
        state.Reserved5 = packet[14];
        state.Reserved6 = packet[15];
        
        return state;
    }
    
    /// <summary>
    /// Validates if a packet appears to be valid (quick check)
    /// For the ultra-compact format, we just check size
    /// </summary>
    public static bool IsValidPacket(byte[] packet)
    {
        return packet != null && packet.Length >= PACKET_SIZE;
    }
    
    /// <summary>
    /// Gets the size of the ControllerState structure for verification
    /// Should return 16 bytes
    /// </summary>
    public static int GetStructSize()
    {
        return Marshal.SizeOf<ControllerState>();
    }
}

