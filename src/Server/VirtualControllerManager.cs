using System;
using System.Collections.Generic;
using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;
using RemotePlay.Common;

namespace RemotePlay.Server;

/// <summary>
/// Manages virtual Xbox 360 controllers using ViGEmBus
/// </summary>
public class VirtualControllerManager : IDisposable
{
    private readonly ViGEmClient _client;
    private readonly Dictionary<byte, IXbox360Controller> _controllers;
    private bool _isDisposed = false;
    
    public VirtualControllerManager()
    {
        try
        {
            _client = new ViGEmClient();
            _controllers = new Dictionary<byte, IXbox360Controller>();
            Console.WriteLine("✓ ViGEmBus client initialized successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: Failed to initialize ViGEmBus client: {ex.Message}");
            Console.WriteLine();
            Console.WriteLine("Make sure ViGEmBus driver is installed!");
            Console.WriteLine("Download from: https://github.com/ViGEm/ViGEmBus/releases");
            throw;
        }
    }
    
    /// <summary>
    /// Creates or gets an existing virtual controller for the given controller ID
    /// </summary>
    public IXbox360Controller GetOrCreateController(byte controllerId)
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException(nameof(VirtualControllerManager));
        }
        
        if (controllerId >= NetworkConfig.MAX_CONTROLLERS)
        {
            throw new ArgumentException($"Controller ID must be between 0 and {NetworkConfig.MAX_CONTROLLERS - 1}", nameof(controllerId));
        }
        
        if (!_controllers.ContainsKey(controllerId))
        {
            var controller = _client.CreateXbox360Controller();
            controller.Connect();
            _controllers[controllerId] = controller;
            Console.WriteLine($"✓ Created virtual Xbox 360 controller #{controllerId}");
        }
        
        return _controllers[controllerId];
    }
    
    /// <summary>
    /// Updates a virtual controller with the provided state
    /// </summary>
    public void UpdateController(ControllerState state)
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException(nameof(VirtualControllerManager));
        }
        
        var controller = GetOrCreateController(state.ControllerId);
        
        // Map buttons
        controller.SetButtonState(Xbox360Button.A, (state.Buttons & ControllerState.XINPUT_GAMEPAD_A) != 0);
        controller.SetButtonState(Xbox360Button.B, (state.Buttons & ControllerState.XINPUT_GAMEPAD_B) != 0);
        controller.SetButtonState(Xbox360Button.X, (state.Buttons & ControllerState.XINPUT_GAMEPAD_X) != 0);
        controller.SetButtonState(Xbox360Button.Y, (state.Buttons & ControllerState.XINPUT_GAMEPAD_Y) != 0);
        
        controller.SetButtonState(Xbox360Button.Start, (state.Buttons & ControllerState.XINPUT_GAMEPAD_START) != 0);
        controller.SetButtonState(Xbox360Button.Back, (state.Buttons & ControllerState.XINPUT_GAMEPAD_BACK) != 0);
        
        controller.SetButtonState(Xbox360Button.LeftShoulder, (state.Buttons & ControllerState.XINPUT_GAMEPAD_LEFT_SHOULDER) != 0);
        controller.SetButtonState(Xbox360Button.RightShoulder, (state.Buttons & ControllerState.XINPUT_GAMEPAD_RIGHT_SHOULDER) != 0);
        
        controller.SetButtonState(Xbox360Button.LeftThumb, (state.Buttons & ControllerState.XINPUT_GAMEPAD_LEFT_THUMB) != 0);
        controller.SetButtonState(Xbox360Button.RightThumb, (state.Buttons & ControllerState.XINPUT_GAMEPAD_RIGHT_THUMB) != 0);
        
        controller.SetButtonState(Xbox360Button.Up, (state.Buttons & ControllerState.XINPUT_GAMEPAD_DPAD_UP) != 0);
        controller.SetButtonState(Xbox360Button.Down, (state.Buttons & ControllerState.XINPUT_GAMEPAD_DPAD_DOWN) != 0);
        controller.SetButtonState(Xbox360Button.Left, (state.Buttons & ControllerState.XINPUT_GAMEPAD_DPAD_LEFT) != 0);
        controller.SetButtonState(Xbox360Button.Right, (state.Buttons & ControllerState.XINPUT_GAMEPAD_DPAD_RIGHT) != 0);
        
        // Map triggers
        controller.SetSliderValue(Xbox360Slider.LeftTrigger, state.LeftTrigger);
        controller.SetSliderValue(Xbox360Slider.RightTrigger, state.RightTrigger);
        
        // Map analog sticks
        controller.SetAxisValue(Xbox360Axis.LeftThumbX, state.LeftThumbX);
        controller.SetAxisValue(Xbox360Axis.LeftThumbY, state.LeftThumbY);
        controller.SetAxisValue(Xbox360Axis.RightThumbX, state.RightThumbX);
        controller.SetAxisValue(Xbox360Axis.RightThumbY, state.RightThumbY);
        
        // Submit the report to the driver
        controller.SubmitReport();
    }
    
    /// <summary>
    /// Resets a controller to neutral state
    /// </summary>
    public void ResetController(byte controllerId)
    {
        if (!_controllers.ContainsKey(controllerId))
        {
            return;
        }
        
        var neutralState = new ControllerState
        {
            ControllerId = controllerId
        };
        
        UpdateController(neutralState);
    }
    
    /// <summary>
    /// Removes and disconnects a virtual controller
    /// </summary>
    public void RemoveController(byte controllerId)
    {
        if (_controllers.ContainsKey(controllerId))
        {
            ResetController(controllerId);
            _controllers[controllerId].Disconnect();
            _controllers.Remove(controllerId);
            Console.WriteLine($"Removed virtual controller #{controllerId}");
        }
    }
    
    public void Dispose()
    {
        if (!_isDisposed)
        {
            // Disconnect all controllers
            foreach (var kvp in _controllers)
            {
                try
                {
                    ResetController(kvp.Key);
                    kvp.Value.Disconnect();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error disconnecting controller {kvp.Key}: {ex.Message}");
                }
            }
            
            _controllers.Clear();
            _client?.Dispose();
            _isDisposed = true;
            
            Console.WriteLine("Virtual controller manager disposed");
        }
    }
}
