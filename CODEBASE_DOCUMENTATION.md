# Remote Play - Complete Codebase Documentation

## Table of Contents
1. [Project Overview](#project-overview)
2. [Architecture](#architecture)
3. [Common Library](#common-library)
4. [Client Application](#client-application)
5. [Server Application](#server-application)
6. [Data Flow](#data-flow)
7. [Network Protocol](#network-protocol)

---

## Project Overview

**Remote Play** is a C# .NET 8.0 application that streams Xbox controller inputs over a network from client PCs to a server PC, where they are emulated as virtual Xbox 360 controllers using ViGEmBus.

### Key Technologies
- **.NET 8.0** - Modern C# framework
- **XInput API** - Windows Xbox controller interface (xinput1_4.dll)
- **ViGEmBus** - Virtual gamepad emulation driver
- **UDP Sockets** - Low-latency networking
- **Open.NAT** - UPnP port forwarding

---

## Architecture

The project is organized into three main components:

```
RemotePlay.Common (Class Library)
    ↓ Referenced by ↓
RemotePlay.Client (Console App)    RemotePlay.Server (Console App)
```

### Project Dependencies

**RemotePlay.Common.csproj**
- No external dependencies
- Shared by both Client and Server

**RemotePlay.Client.csproj**
- References: RemotePlay.Common

**RemotePlay.Server.csproj**
- References: RemotePlay.Common
- NuGet: Nefarius.ViGEm.Client (v1.21.256)
- NuGet: Open.Nat (v2.1.0)

---

## Common Library

Location: `src/Common/`

### 1. ControllerState.cs

**Purpose**: Defines the data structure representing a complete Xbox controller state.

#### Structure Definition
```csharp
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct ControllerState
```

**Why Sequential Layout?**
- Ensures fields are laid out in memory exactly as declared
- `Pack = 1` removes padding between fields for minimal size
- Critical for binary serialization over network

#### Fields

| Field | Type | Range | Description |
|-------|------|-------|-------------|
| `Buttons` | ushort | 0-65535 | Bit flags for all buttons |
| `LeftTrigger` | byte | 0-255 | Left trigger pressure |
| `RightTrigger` | byte | 0-255 | Right trigger pressure |
| `LeftThumbX` | short | -32768 to 32767 | Left stick horizontal |
| `LeftThumbY` | short | -32768 to 32767 | Left stick vertical |
| `RightThumbX` | short | -32768 to 32767 | Right stick horizontal |
| `RightThumbY` | short | -32768 to 32767 | Right stick vertical |
| `ControllerId` | byte | 0-3 | Which controller (0-3) |
| `PacketNumber` | uint | 0-4294967295 | Sequence number |

**Total Size**: 17 bytes (extremely compact)

#### Button Constants

All buttons are stored as bit flags in the `Buttons` field:

```csharp
XINPUT_GAMEPAD_DPAD_UP          = 0x0001  // Bit 0
XINPUT_GAMEPAD_DPAD_DOWN        = 0x0002  // Bit 1
XINPUT_GAMEPAD_DPAD_LEFT        = 0x0004  // Bit 2
XINPUT_GAMEPAD_DPAD_RIGHT       = 0x0008  // Bit 3
XINPUT_GAMEPAD_START            = 0x0010  // Bit 4
XINPUT_GAMEPAD_BACK             = 0x0020  // Bit 5
XINPUT_GAMEPAD_LEFT_THUMB       = 0x0040  // Bit 6
XINPUT_GAMEPAD_RIGHT_THUMB      = 0x0080  // Bit 7
XINPUT_GAMEPAD_LEFT_SHOULDER    = 0x0100  // Bit 8
XINPUT_GAMEPAD_RIGHT_SHOULDER   = 0x0200  // Bit 9
XINPUT_GAMEPAD_A                = 0x1000  // Bit 12
XINPUT_GAMEPAD_B                = 0x2000  // Bit 13
XINPUT_GAMEPAD_X                = 0x4000  // Bit 14
XINPUT_GAMEPAD_Y                = 0x8000  // Bit 15
```

#### Methods

**`IsButtonPressed(ushort button)`**
- Checks if a specific button is pressed using bitwise AND
- Example: `state.IsButtonPressed(XINPUT_GAMEPAD_A)`

**`HasInput()`**
- Returns true if any input is active (not neutral)
- Checks buttons, triggers, and stick positions
- Deadzone: ±1000 for analog sticks

**`ToString()`**
- Provides human-readable representation for debugging

---

### 2. NetworkConfig.cs

**Purpose**: Centralized configuration constants shared between client and server.

#### Constants

| Constant | Value | Description |
|----------|-------|-------------|
| `DEFAULT_PORT` | 11000 | UDP port for communication |
| `MAX_CONTROLLERS` | 4 | Maximum simultaneous controllers |
| `CLIENT_TIMEOUT_MS` | 5000 | Client timeout (5 seconds) |
| `POLLING_INTERVAL_MS` | 1 | Controller polling rate (~1000 Hz) |
| `MAX_PACKET_SIZE` | 256 | Maximum UDP packet size |
| `SEND_ON_CHANGE` | false | Send continuously vs. on-change |

**Why POLLING_INTERVAL_MS = 1?**
- Provides ~1000 Hz polling rate
- Ensures instant response to controller input
- Thread.Sleep(1) typically sleeps 1-2ms on Windows

**Why SEND_ON_CHANGE = false?**
- Originally true, changed to false for better responsiveness
- Sends packets continuously regardless of state changes
- Eliminates potential missed updates

---

### 3. PacketSerializer.cs

**Purpose**: Handles binary serialization/deserialization of ControllerState for network transmission.

#### Packet Format

```
[MAGIC_NUMBER (4 bytes)][VERSION (1 byte)][CONTROLLER_STATE (17 bytes)]
Total: 22 bytes
```

#### Constants

- **MAGIC_NUMBER**: `0x52504C59` ("RPLY" in ASCII hex)
- **PROTOCOL_VERSION**: `1`
- **PACKET_SIZE**: 22 bytes

#### Methods

**`Serialize(ControllerState state)`**

1. Creates 22-byte buffer
2. Writes magic number (4 bytes)
3. Writes protocol version (1 byte)
4. Marshals ControllerState struct to bytes (17 bytes)
5. Returns complete packet

**Technical Details**:
- Uses `Marshal.StructureToPtr()` for struct → bytes conversion
- Allocates unmanaged memory temporarily
- Properly frees memory in finally block

**`Deserialize(byte[] packet)`**

1. Validates packet size (≥22 bytes)
2. Verifies magic number
3. Checks protocol version
4. Unmarshals bytes to ControllerState struct
5. Returns nullable ControllerState

**Returns null if**:
- Packet too small
- Invalid magic number
- Wrong protocol version

**`IsValidPacket(byte[] packet)`**

Quick validation without full deserialization:
- Checks minimum size
- Verifies magic number
- Checks protocol version

---

## Client Application

Location: `src/Client/`

### 1. Program.cs

**Purpose**: Main entry point for the client application.

#### Flow

1. **Display Banner**
2. **Get Configuration from User**
   - Server IP (default: 127.0.0.1)
   - Port (default: 11000)
   - Controller index (0-3)
3. **Initialize Components**
   - XInputCapture (controller reader)
   - NetworkSender (UDP sender)
4. **Check Controller Connection**
   - Exit if no controller found
5. **Main Loop**
   - Poll controller at POLLING_INTERVAL_MS
   - Send state if changed (or always if SEND_ON_CHANGE=false)
   - Update statistics every second
   - Check for ESC key to exit
6. **Cleanup and Shutdown**

#### Key Variables

- `packetsSent`: Total packets transmitted
- `lastState`: Previous controller state for change detection
- `lastStatsUpdate`: Timestamp for statistics display

#### Change Detection Logic

```csharp
if (NetworkConfig.SEND_ON_CHANGE)
{
    // Check if buttons changed
    // Check if triggers changed
    // Check if sticks moved >500 units (deadzone)
    shouldSend = true;
}
```

**Stick Deadzone**: 500 units prevents jitter from sending unnecessary packets.

#### Performance Optimization

- Uses `Thread.Sleep()` instead of `Task.Delay()` for precise timing
- Synchronous sending (no await) for minimum latency
- Updates console statistics only once per second

---

### 2. XInputCapture.cs

**Purpose**: Wrapper for Windows XInput API to read Xbox controller state.

#### P/Invoke Declarations

```csharp
[DllImport("xinput1_4.dll")]
private static extern int XInputGetState(int dwUserIndex, ref XINPUT_STATE pState);

[DllImport("xinput1_4.dll")]
private static extern int XInputSetState(int dwUserIndex, ref XINPUT_VIBRATION pVibration);
```

**Why xinput1_4.dll?**
- Modern version (Windows 8+)
- Supports Xbox One controllers
- Fallback: xinput1_3.dll for Windows 7

#### Native Structures

**XINPUT_STATE**
```csharp
struct XINPUT_STATE
{
    uint dwPacketNumber;      // Increments when state changes
    XINPUT_GAMEPAD Gamepad;   // Controller data
}
```

**XINPUT_GAMEPAD**
```csharp
struct XINPUT_GAMEPAD
{
    ushort wButtons;          // Button bit flags
    byte bLeftTrigger;        // 0-255
    byte bRightTrigger;       // 0-255
    short sThumbLX;           // -32768 to 32767
    short sThumbLY;
    short sThumbRX;
    short sThumbRY;
}
```

**XINPUT_VIBRATION**
```csharp
struct XINPUT_VIBRATION
{
    ushort wLeftMotorSpeed;   // 0-65535
    ushort wRightMotorSpeed;  // 0-65535
}
```

#### Error Codes

- `ERROR_SUCCESS = 0`: Controller connected and read successfully
- `ERROR_DEVICE_NOT_CONNECTED = 1167`: Controller not found

#### Methods

**`GetState()`**

1. Calls XInputGetState with controller index
2. Returns null if disconnected
3. Converts XINPUT_GAMEPAD to ControllerState
4. Updates _isConnected flag
5. Stores packet number

**`CheckConnection()`**

- Quick check without full state read
- Updates _isConnected flag
- Returns boolean

**`SetVibration(ushort leftMotor, ushort rightMotor)`**

- Sets controller rumble (0-65535)
- Currently unused but available for future features

**`Dispose()`**

- Stops vibration on cleanup
- Implements IDisposable pattern

---

### 3. NetworkSender.cs

**Purpose**: Sends controller data to server via UDP with minimum latency.

#### Design Philosophy

**CRITICAL: Zero-latency design**
- Synchronous sending (blocking)
- No OS-level buffering
- No packet queuing
- Immediate transmission

#### Socket Configuration

```csharp
_socket.SendBufferSize = 0;    // Disable OS buffering
_socket.Blocking = true;        // Synchronous mode
_socket.DontFragment = true;    // Single packet, no fragmentation
```

**Why SendBufferSize = 0?**
- Packets sent immediately to network
- No queuing delay in OS
- Trade-off: Slightly higher CPU usage

**Why Blocking = true?**
- Ensures packet is sent before returning
- Predictable timing
- Simpler error handling

#### Methods

**`SendControllerState(ControllerState state)`**

1. Serialize state to bytes (22 bytes)
2. Call `_socket.Send()` - blocks until sent
3. Catch and log errors (doesn't crash)

**Flow**:
```
ControllerState → PacketSerializer.Serialize() → Socket.Send() → Network
```

**`SendControllerStateAsync(ControllerState state)`**

- Wrapper for async compatibility
- Actually calls synchronous version
- Returns completed Task immediately

---

## Server Application

Location: `src/Server/`

### 1. Program.cs

**Purpose**: Main entry point for server application.

#### Flow

1. **Display Banner**
2. **Get Port Configuration**
3. **Display Local IP Addresses**
   - Uses `Dns.GetHostAddresses()` to find all IPv4 addresses
4. **Internet Play Setup**
   - Fetch public IP from api.ipify.org
   - Attempt UPnP port forwarding via Open.NAT
5. **Initialize Components**
   - VirtualControllerManager (ViGEm)
   - NetworkReceiver (UDP listener)
6. **Setup Event Handler**
   - OnControllerStateReceived event
   - Update statistics every 2 seconds
7. **Main Loop**
   - Wait for ESC to stop
   - Press 'S' for detailed statistics
8. **Shutdown**
   - Cancel receiver task
   - Dispose components

#### UPnP Port Forwarding

```csharp
var discoverer = new NatDiscoverer();
var device = await discoverer.DiscoverDeviceAsync(PortMapper.Upnp, timeout);
await device.CreatePortMapAsync(new Mapping(Protocol.Udp, port, port, "RemotePlay Server"));
```

**What this does**:
- Discovers UPnP-enabled router
- Creates port forwarding rule: External Port → Internal Port
- Allows internet clients to connect

**Fallback**: Manual port forwarding if UPnP fails

#### Statistics Display

- **Total packets**: Cumulative count
- **Active clients**: Number of connected controllers
- **Per-client stats**: Endpoint and packet count

---

### 2. NetworkReceiver.cs

**Purpose**: Receives controller data from clients via UDP.

#### Architecture

- Uses `UdpClient` (higher-level than raw Socket)
- Asynchronous receiving with cancellation support
- Tracks multiple clients simultaneously
- Automatic timeout detection

#### Client Tracking

**ClientInfo Class**:
```csharp
class ClientInfo
{
    IPEndPoint EndPoint;           // Client address
    DateTime LastPacketTime;       // For timeout detection
    long PacketsReceived;          // Statistics
}
```

**Dictionary**: `Dictionary<byte, ClientInfo>`
- Key: Controller ID (0-3)
- Value: Client information

#### Methods

**`StartAsync(CancellationToken cancellationToken)`**

Main receiving loop:

1. Start timeout checker task
2. Loop while running:
   - Receive UDP packet (with timeout)
   - Process packet
   - Handle errors gracefully
3. Wait for timeout task to complete

**Cancellation Support**:
```csharp
var receiveTask = _udpClient.ReceiveAsync();
var completedTask = await Task.WhenAny(receiveTask, Task.Delay(100, cancellationToken));
```

This allows checking cancellation token every 100ms.

**`ProcessPacket(byte[] data, IPEndPoint clientEndPoint)`**

1. Validate packet using `PacketSerializer.IsValidPacket()`
2. Deserialize to ControllerState
3. Track client (add if new)
4. Update last packet time
5. Call `VirtualControllerManager.UpdateController()`
6. Raise OnControllerStateReceived event

**Error Handling**:
- Invalid packets: Log and ignore
- Deserialization failure: Log and ignore
- Controller update error: Log but continue

**`CheckClientTimeouts(CancellationToken cancellationToken)`**

Background task that runs every second:

1. Check each client's last packet time
2. If > CLIENT_TIMEOUT_MS (5000ms):
   - Log timeout
   - Reset virtual controller to neutral
   - Remove from tracking

**Why timeout?**
- Prevents stuck buttons if client crashes
- Cleans up disconnected clients
- Resets controller to safe state

**`GetClientStats()`**

Returns dictionary of active clients with:
- Controller ID
- IP endpoint
- Packet count

---

### 3. VirtualControllerManager.cs

**Purpose**: Manages virtual Xbox 360 controllers using ViGEmBus driver.

#### ViGEmBus Overview

**ViGEmBus** is a Windows kernel-mode driver that:
- Emulates Xbox 360 and DualShock 4 controllers
- Appears to games as real hardware
- Supports up to 4 Xbox 360 controllers simultaneously

#### Initialization

```csharp
_client = new ViGEmClient();
```

**What happens**:
- Connects to ViGEmBus driver
- Throws exception if driver not installed
- Provides helpful error message with download link

#### Controller Management

**Dictionary**: `Dictionary<byte, IXbox360Controller>`
- Key: Controller ID (0-3)
- Value: Virtual controller instance

**`GetOrCreateController(byte controllerId)`**

Lazy initialization pattern:

1. Check if controller already exists
2. If not:
   - Create new Xbox360Controller
   - Connect it (appears in Windows)
   - Store in dictionary
   - Log creation
3. Return controller instance

**Why lazy?**
- Only creates controllers when needed
- Saves resources
- Cleaner device manager

**`UpdateController(ControllerState state)`**

Maps network state to virtual controller:

**Buttons** (14 buttons):
```csharp
controller.SetButtonState(Xbox360Button.A, (state.Buttons & XINPUT_GAMEPAD_A) != 0);
// Repeat for all buttons...
```

**Triggers** (2 triggers):
```csharp
controller.SetSliderValue(Xbox360Slider.LeftTrigger, state.LeftTrigger);
controller.SetSliderValue(Xbox360Slider.RightTrigger, state.RightTrigger);
```

**Analog Sticks** (4 axes):
```csharp
controller.SetAxisValue(Xbox360Axis.LeftThumbX, state.LeftThumbX);
controller.SetAxisValue(Xbox360Axis.LeftThumbY, state.LeftThumbY);
controller.SetAxisValue(Xbox360Axis.RightThumbX, state.RightThumbX);
controller.SetAxisValue(Xbox360Axis.RightThumbY, state.RightThumbY);
```

**Submit Report**:
```csharp
controller.SubmitReport();
```

This sends all changes to the driver in one atomic operation.

**`ResetController(byte controllerId)`**

Sets controller to neutral state:
- All buttons released
- Triggers at 0
- Sticks centered (0, 0)

**Used when**:
- Client times out
- Client disconnects
- Server shutdown

**`RemoveController(byte controllerId)`**

1. Reset to neutral
2. Disconnect virtual controller
3. Remove from dictionary
4. Controller disappears from Windows

**`Dispose()`**

Cleanup on server shutdown:

1. Reset all controllers
2. Disconnect all controllers
3. Clear dictionary
4. Dispose ViGEmClient

---

## Data Flow

### Complete Flow Diagram

```
CLIENT SIDE:
┌─────────────────┐
│ Physical Xbox   │
│   Controller    │
└────────┬────────┘
         │ XInput API (xinput1_4.dll)
         v
┌─────────────────┐
│ XInputCapture   │  GetState() every 1ms
│   .GetState()   │
└────────┬────────┘
         │ ControllerState struct
         v
┌─────────────────┐
│  Program.cs     │  Main loop
│   Main Loop     │
└────────┬────────┘
         │ If changed (or always)
         v
┌─────────────────┐
│ PacketSerializer│  Serialize to 22 bytes
│   .Serialize()  │
└────────┬────────┘
         │ byte[22]
         v
┌─────────────────┐
│ NetworkSender   │  UDP Socket.Send()
│ .SendController │
│     State()     │
└────────┬────────┘
         │
         │ UDP Packet over Network
         │
         v
═══════════════════════════════════════════

SERVER SIDE:
         │
         v
┌─────────────────┐
│ NetworkReceiver │  UdpClient.ReceiveAsync()
│  .StartAsync()  │
└────────┬────────┘
         │ byte[22]
         v
┌─────────────────┐
│ PacketSerializer│  Deserialize from bytes
│  .Deserialize() │
└────────┬────────┘
         │ ControllerState struct
         v
┌─────────────────┐
│ NetworkReceiver │  Track client, update time
│ .ProcessPacket()│
└────────┬────────┘
         │ ControllerState
         v
┌─────────────────┐
│VirtualController│  Map to ViGEm API
│   Manager       │
│.UpdateController│
└────────┬────────┘
         │ ViGEm API calls
         v
┌─────────────────┐
│  ViGEmBus       │  Kernel driver
│    Driver       │
└────────┬────────┘
         │ HID Device
         v
┌─────────────────┐
│  Windows sees   │
│ Virtual Xbox360 │
│   Controller    │
└────────┬────────┘
         │
         v
┌─────────────────┐
│      Game       │  Reads controller via XInput
└─────────────────┘
```

### Timing Analysis

**Client Side** (per packet):
1. XInputGetState: ~0.1ms
2. Serialize: ~0.01ms
3. Socket.Send: ~0.1-1ms (network dependent)

**Network**:
- LAN: 1-5ms
- Internet: 20-100ms (varies)

**Server Side** (per packet):
1. UdpClient.Receive: ~0.1ms
2. Deserialize: ~0.01ms
3. ViGEm Update: ~0.1ms

**Total Latency (LAN)**: ~2-10ms
**Total Latency (Internet)**: ~25-110ms

---

## Network Protocol

### Packet Structure

```
Offset | Size | Field              | Description
-------|------|--------------------|---------------------------------
0      | 4    | MAGIC_NUMBER       | 0x52504C59 ("RPLY")
4      | 1    | PROTOCOL_VERSION   | 1
5      | 2    | Buttons            | Button bit flags
7      | 1    | LeftTrigger        | 0-255
8      | 1    | RightTrigger       | 0-255
9      | 2    | LeftThumbX         | -32768 to 32767
11     | 2    | LeftThumbY         | -32768 to 32767
13     | 2    | RightThumbX        | -32768 to 32767
15     | 2    | RightThumbY        | -32768 to 32767
17     | 1    | ControllerId       | 0-3
18     | 4    | PacketNumber       | Sequence number
-------|------|--------------------|---------------------------------
Total: 22 bytes
```

### Why UDP?

**Advantages**:
- No connection overhead
- No retransmission delays
- Lower latency than TCP
- Simpler protocol

**Disadvantages**:
- Packets can be lost
- Packets can arrive out of order
- No built-in reliability

**Why it works for controllers**:
- High packet rate (1000 Hz) means lost packets are quickly replaced
- Latest state is what matters, not history
- Out-of-order packets are acceptable (latest wins)

### Reliability Mechanisms

1. **Magic Number**: Validates packet is from our protocol
2. **Version Check**: Ensures client/server compatibility
3. **Size Validation**: Prevents buffer overflows
4. **Timeout Detection**: Removes dead clients
5. **Continuous Sending**: Lost packets replaced quickly

---

## Summary

This codebase implements a complete remote controller streaming solution with:

- **Low latency**: ~2-10ms on LAN
- **Efficient protocol**: 22-byte packets
- **Robust error handling**: Timeouts, validation, graceful degradation
- **Clean architecture**: Separation of concerns, reusable components
- **Production ready**: Proper resource management, logging, statistics

The design prioritizes **responsiveness** and **simplicity** over complex reliability mechanisms, which is appropriate for real-time controller input where the latest state is most important.
