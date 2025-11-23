using System;
using System.Runtime.InteropServices;

namespace RemotePlay.Common;

/// <summary>
/// Represents the complete state of an Xbox controller
/// This structure matches the XInput gamepad layout for compatibility
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct ControllerState
{
    // Controller buttons (bit flags)
    public ushort Buttons;
    
    // Triggers (0-255)
    public byte LeftTrigger;
    public byte RightTrigger;
    
    // Analog sticks (-32768 to 32767)
    public short LeftThumbX;
    public short LeftThumbY;
    public short RightThumbX;
    public short RightThumbY;
    
    // Metadata
    public byte ControllerId; // 0-3
    public uint PacketNumber; // Increments with each update
    
    // XInput button constants
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
        Buttons = 0;
        LeftTrigger = 0;
        RightTrigger = 0;
        LeftThumbX = 0;
        LeftThumbY = 0;
        RightThumbX = 0;
        RightThumbY = 0;
        ControllerId = 0;
        PacketNumber = 0;
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
               (LeftThumbX < -1000 || LeftThumbX > 1000) ||
               (LeftThumbY < -1000 || LeftThumbY > 1000) ||
               (RightThumbX < -1000 || RightThumbX > 1000) ||
               (RightThumbY < -1000 || RightThumbY > 1000);
    }
    
    public override string ToString()
    {
        return $"Controller {ControllerId} [Packet: {PacketNumber}] " +
               $"Buttons: 0x{Buttons:X4}, " +
               $"LT: {LeftTrigger}, RT: {RightTrigger}, " +
               $"LS: ({LeftThumbX},{LeftThumbY}), " +
               $"RS: ({RightThumbX},{RightThumbY})";
    }
}
