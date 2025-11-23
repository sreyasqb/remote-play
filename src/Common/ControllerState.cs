using System;
using System.Runtime.InteropServices;

namespace RemotePlay.Common;

/// <summary>
/// Ultra-compact 16-byte controller state packet (Parsec/Moonlight style)
/// Optimized for low-latency WAN transmission over UDP
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct ControllerState
{
    // Packet header (2 bytes)
    public byte Flags;          // Bit flags: gyro, rumble, heartbeat, etc.
    public byte Sequence;       // Sequence number (0-255, wrapping)
    
    // Buttons (2 bytes)
    public ushort Buttons;      // Button bitmask
    
    // Analog sticks - 8-bit signed (4 bytes)
    public sbyte LeftStickX;    // -128 to 127
    public sbyte LeftStickY;    // -128 to 127
    public sbyte RightStickX;   // -128 to 127
    public sbyte RightStickY;   // -128 to 127
    
    // Triggers - 8-bit unsigned (2 bytes)
    public byte LeftTrigger;    // 0-255
    public byte RightTrigger;   // 0-255
    
    // Reserved for future use (6 bytes) - gyro, touchpad, etc.
    public byte Reserved1;
    public byte Reserved2;
    public byte Reserved3;
    public byte Reserved4;
    public byte Reserved5;
    public byte Reserved6;
    
    // Total: 16 bytes
    
    // Packet flags
    public const byte FLAG_HAS_GYRO = 0x01;
    public const byte FLAG_HAS_RUMBLE = 0x02;
    public const byte FLAG_HEARTBEAT = 0x04;
    public const byte FLAG_KEYBOARD = 0x08;
    public const byte FLAG_MOUSE = 0x10;
    
    // XInput button constants (matching XInput layout)
    public const ushort XINPUT_GAMEPAD_DPAD_UP = 0x0001;
    public const ushort XINPUT_GAMEPAD_DPAD_DOWN = 0x0002;
    public const ushort XINPUT_GAMEPAD_DPAD_LEFT = 0x0004;
    public const ushort XINPUT_GAMEPAD_DPAD_RIGHT = 0x0008;
    public const ushort XINPUT_GAMEPAD_START = 0x0010;
    public const ushort XINPUT_GAMEPAD_BACK = 0x0020;
    public const ushort XINPUT_GAMEPAD_LEFT_THUMB = 0x0040;
    public const ushort XINPUT_GAMEPAD_RIGHT_THUMB = 0x0080;
    public const ushort XINPUT_GAMEPAD_LEFT_SHOULDER = 0x0100;
    public const ushort XINPUT_GAMEPAD_RIGHT_SHOULDER = 0x0200;
    public const ushort XINPUT_GAMEPAD_A = 0x1000;
    public const ushort XINPUT_GAMEPAD_B = 0x2000;
    public const ushort XINPUT_GAMEPAD_X = 0x4000;
    public const ushort XINPUT_GAMEPAD_Y = 0x8000;
    
    public ControllerState()
    {
        Flags = 0;
        Sequence = 0;
        Buttons = 0;
        LeftStickX = 0;
        LeftStickY = 0;
        RightStickX = 0;
        RightStickY = 0;
        LeftTrigger = 0;
        RightTrigger = 0;
        Reserved1 = 0;
        Reserved2 = 0;
        Reserved3 = 0;
        Reserved4 = 0;
        Reserved5 = 0;
        Reserved6 = 0;
    }
    
    /// <summary>
    /// Encodes a 16-bit stick value (-32768 to 32767) to 8-bit (-128 to 127)
    /// Uses Parsec/Moonlight compression technique
    /// </summary>
    public static sbyte EncodeStick(short value)
    {
        // Divide by 258 to map ±32768 to ±127
        return (sbyte)(value / 258);
    }
    
    /// <summary>
    /// Decodes an 8-bit stick value (-128 to 127) back to 16-bit (-32768 to 32767)
    /// </summary>
    public static short DecodeStick(sbyte value)
    {
        // Multiply by 258 to restore range
        return (short)(value * 258);
    }
    
    /// <summary>
    /// Encodes a float stick value (-1.0 to 1.0) to 8-bit (-128 to 127)
    /// </summary>
    public static sbyte EncodeStickFloat(float value)
    {
        return (sbyte)(value * 127.0f);
    }
    
    /// <summary>
    /// Decodes an 8-bit stick value to float (-1.0 to 1.0)
    /// </summary>
    public static float DecodeStickFloat(sbyte value)
    {
        return value / 127.0f;
    }
    
    /// <summary>
    /// Encodes a float trigger value (0.0 to 1.0) to 8-bit (0 to 255)
    /// </summary>
    public static byte EncodeTrigger(float value)
    {
        return (byte)(value * 255.0f);
    }
    
    /// <summary>
    /// Checks if a specific button is pressed
    /// </summary>
    public bool IsButtonPressed(ushort button)
    {
        return (Buttons & button) != 0;
    }
    
    /// <summary>
    /// Returns true if any input is active (not neutral position)
    /// </summary>
    public bool HasInput()
    {
        return Buttons != 0 || LeftTrigger != 0 || RightTrigger != 0 ||
               (LeftStickX < -10 || LeftStickX > 10) ||
               (LeftStickY < -10 || LeftStickY > 10) ||
               (RightStickX < -10 || RightStickX > 10) ||
               (RightStickY < -10 || RightStickY > 10);
    }
    
    // commit
    public override string ToString()
    {
        return $"[Seq: {Sequence}] " +
               $"Buttons: 0x{Buttons:X4}, " +
               $"LT: {LeftTrigger}, RT: {RightTrigger}, " +
               $"LS: ({LeftStickX},{LeftStickY}), " +
               $"RS: ({RightStickX},{RightStickY})";
    }
}
