using System;
using System.Runtime.InteropServices;
using RemotePlay.Common;

namespace RemotePlay.Client;

/// <summary>
/// Wrapper for XInput API to capture Xbox controller input
/// </summary>
public class XInputCapture : IDisposable
{
    // XInput API
    [DllImport("xinput1_4.dll")]
    private static extern int XInputGetState(int dwUserIndex, ref XINPUT_STATE pState);
    
    [DllImport("xinput1_4.dll")]
    private static extern int XInputSetState(int dwUserIndex, ref XINPUT_VIBRATION pVibration);
    
    private const int ERROR_SUCCESS = 0;
    private const int ERROR_DEVICE_NOT_CONNECTED = 1167;
    
    [StructLayout(LayoutKind.Sequential)]
    private struct XINPUT_STATE
    {
        public uint dwPacketNumber;
        public XINPUT_GAMEPAD Gamepad;
    }
    
    [StructLayout(LayoutKind.Sequential)]
    private struct XINPUT_GAMEPAD
    {
        public ushort wButtons;
        public byte bLeftTrigger;
        public byte bRightTrigger;
        public short sThumbLX;
        public short sThumbLY;
        public short sThumbRX;
        public short sThumbRY;
    }
    
    [StructLayout(LayoutKind.Sequential)]
    private struct XINPUT_VIBRATION
    {
        public ushort wLeftMotorSpeed;
        public ushort wRightMotorSpeed;
    }
    
    private readonly int _controllerIndex;
    private uint _lastPacketNumber = 0;
    private bool _isConnected = false;
    
    public bool IsConnected => _isConnected;
    
    public XInputCapture(int controllerIndex = 0)
    {
        if (controllerIndex < 0 || controllerIndex >= NetworkConfig.MAX_CONTROLLERS)
        {
            throw new ArgumentException($"Controller index must be between 0 and {NetworkConfig.MAX_CONTROLLERS - 1}", nameof(controllerIndex));
        }
        
        _controllerIndex = controllerIndex;
    }
    
    /// <summary>
    /// Reads the current state of the Xbox controller
    /// Returns null if controller is not connected
    /// </summary>
    public ControllerState? GetState()
    {
        XINPUT_STATE xstate = new XINPUT_STATE();
        int result = XInputGetState(_controllerIndex, ref xstate);
        
        if (result == ERROR_DEVICE_NOT_CONNECTED)
        {
            _isConnected = false;
            return null;
        }
        
        if (result != ERROR_SUCCESS)
        {
            _isConnected = false;
            return null;
        }
        
        _isConnected = true;
        _lastPacketNumber = xstate.dwPacketNumber;
        
        // Convert XInput state to our ControllerState
        return new ControllerState
        {
            Buttons = xstate.Gamepad.wButtons,
            LeftTrigger = xstate.Gamepad.bLeftTrigger,
            RightTrigger = xstate.Gamepad.bRightTrigger,
            LeftThumbX = xstate.Gamepad.sThumbLX,
            LeftThumbY = xstate.Gamepad.sThumbLY,
            RightThumbX = xstate.Gamepad.sThumbRX,
            RightThumbY = xstate.Gamepad.sThumbRY,
            ControllerId = (byte)_controllerIndex,
            PacketNumber = xstate.dwPacketNumber
        };
    }
    
    /// <summary>
    /// Checks if the controller is connected and available
    /// </summary>
    public bool CheckConnection()
    {
        XINPUT_STATE xstate = new XINPUT_STATE();
        int result = XInputGetState(_controllerIndex, ref xstate);
        _isConnected = (result == ERROR_SUCCESS);
        return _isConnected;
    }
    
    /// <summary>
    /// Sets vibration/rumble on the controller (optional feature)
    /// </summary>
    public void SetVibration(ushort leftMotor, ushort rightMotor)
    {
        if (!_isConnected) return;
        
        XINPUT_VIBRATION vibration = new XINPUT_VIBRATION
        {
            wLeftMotorSpeed = leftMotor,
            wRightMotorSpeed = rightMotor
        };
        
        XInputSetState(_controllerIndex, ref vibration);
    }
    
    public void Dispose()
    {
        // Stop any vibration
        if (_isConnected)
        {
            SetVibration(0, 0);
        }
    }
}
