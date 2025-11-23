# Remote-Play Software Research

## Project Overview

Building a remote-play software where friends can connect their Xbox controllers remotely, and the host system recognizes these inputs as actual Xbox controller inputs for games.

## Key Components

### 1. **Virtual Controller Emulation (Server/Host Side)**
The host system needs to create virtual Xbox controllers that games will recognize as real hardware.

### 2. **Network Communication**
Low-latency transmission of controller inputs from clients to the server.

### 3. **Controller Input Capture (Client Side)**
Capturing physical controller inputs on the client machines.

---

## Recommended Technology Stack

### **Best Programming Language: C# (.NET)**

**Reasons:**
- ✅ Excellent Windows integration and native Xbox controller support
- ✅ Rich ecosystem for virtual controller emulation (ViGEmBus)
- ✅ Built-in networking libraries (`System.Net.Sockets`)
- ✅ Easier to develop and debug compared to C++
- ✅ Cross-platform support with .NET Core
- ✅ Good performance for real-time applications

**Alternative: C++**
- Better for absolute maximum performance
- More complex development
- Good if you need cross-platform support or kernel-level access

**Alternative: Python** (Prototyping only)
- Great for quick prototypes using `vgamepad` library
- Not recommended for production due to performance concerns

---

## Core Technologies & Libraries

### 1. Virtual Controller Emulation - **ViGEmBus**

**What is ViGEmBus?**
- A Windows kernel-mode driver that creates virtual Xbox 360/One controllers
- Games recognize these virtual controllers as genuine hardware
- Industry standard used by DS4Windows, x360ce, and other popular tools

**Important Note:**
> [!WARNING]
> ViGEmBus was officially retired in November 2023 due to trademark issues. However:
> - The driver still works perfectly
> - Existing code and libraries are still functional
> - A successor called "VirtualPad" is in development
> - For this project, ViGEmBus remains the best choice

**Installation:**
- Download ViGEmBus driver installer from GitHub
- Install the `.msi` package on the host system
- Driver provides ViGEmClient API for creating virtual controllers

**C# Integration:**
- Uses ViGEmClient library
- Can create up to 4 virtual Xbox 360 controllers (or Xbox One controllers)
- Simple API to feed input data to virtual controllers

### 2. Network Communication - **UDP Sockets**

**Why UDP over TCP?**
- ✅ Lower latency (critical for gaming)
- ✅ No connection overhead
- ✅ Occasional packet loss is acceptable for controller input
- ✅ More responsive feel

**C# Implementation:**
```
System.Net.Sockets namespace
- UdpClient class for easy UDP communication
- Asynchronous operations to prevent blocking game thread
```

**Data Structure:**
- Serialize controller state (buttons, analog sticks, triggers)
- Keep packets small (< 100 bytes typically)
- Send only when state changes (optimization)

### 3. Controller Input Capture

**For Client Side (Multiple Options):**

**A. XInput (Recommended for Xbox Controllers)**
- Native Windows API for Xbox controllers
- Supports up to 4 controllers
- Simple and reliable
- Built-in vibration/rumble support

**B. DirectInput**
- Lower-level API
- Supports more controller types
- More complex to implement

**C. Windows.Gaming.Input (Modern)**
- Modern WinRT API for Windows 10+
- Better controller type detection
- Semantic information for different controller types

---

## Architecture Design

### System Components

```
┌─────────────────────────────────────────────────────────────┐
│                         CLIENT 1                            │
│  ┌──────────────┐      ┌──────────────┐                    │
│  │   Physical   │─────>│    XInput    │                    │
│  │ Xbox Controller     │   Capture    │                    │
│  └──────────────┘      └──────┬───────┘                    │
│                               │                              │
│                               v                              │
│                        ┌──────────────┐                     │
│                        │  Serialize & │                     │
│                        │  UDP Send    │                     │
│                        └──────┬───────┘                     │
└───────────────────────────────┼─────────────────────────────┘
                                │ Network (UDP)
                                v
┌─────────────────────────────────────────────────────────────┐
│                      SERVER/HOST                             │
│                        ┌──────────────┐                     │
│                        │  UDP Receive │                     │
│                        │ Deserialize  │                     │
│                        └──────┬───────┘                     │
│                               │                              │
│                               v                              │
│                   ┌────────────────────────┐                │
│                   │   ViGEmBus Driver      │                │
│                   │  Virtual Xbox 360 #1   │                │
│                   │  Virtual Xbox 360 #2   │                │
│                   │  Virtual Xbox 360 #3   │                │
│                   │  Virtual Xbox 360 #4   │                │
│                   └────────────┬───────────┘                │
│                                │                             │
│                                v                             │
│                          ┌──────────┐                       │
│                          │   Game   │                       │
│                          │(sees real│                       │
│                          │controllers)                      │
│                          └──────────┘                       │
└─────────────────────────────────────────────────────────────┘
```

### Data Flow

1. **Client captures controller input** (XInput polling every ~1-16ms)
2. **Serialize controller state** into compact format
3. **Send UDP packet** to server IP:Port
4. **Server receives packet** and deserializes
5. **Update virtual controller** via ViGEmClient
6. **Game reads virtual controller** as if it were physical hardware

---

## Existing Solutions & References

### Open-Source Projects to Study

#### 1. **NetInput**
- **GitHub:** Look for gamepad streaming projects using UDP
- **What it does:** Streams gamepad input over network using UDP
- **Implementation:** Polls XInput every millisecond, sends state changes
- **Language:** C++
- **Key Takeaway:** Good reference for UDP streaming architecture

#### 2. **Moonlight + Sunshine**
- **Purpose:** Open-source game streaming (like Parsec)
- **Technologies:** 
  - Moonlight: Client implementation
  - Sunshine: Self-hosted server (supports AMD, Intel, NVIDIA)
- **Protocols:** NVIDIA GameStream protocol
- **Note:** Full game streaming with video, but controller input is similar concept

#### 3. **VDX (nefarius/VDX)**
- **GitHub:** Sample C# application for ViGEmBus
- **What it does:** Mirrors physical Xbox controller to virtual controller
- **Language:** C#
- **Key Takeaway:** Perfect reference for ViGEmClient API usage

#### 4. **vgamepad (Python)**
- **Purpose:** Python library for creating virtual gamepads
- **Uses:** ViGEmBus driver
- **Good for:** Quick prototyping and testing concepts

---

## Implementation Considerations

### Latency Optimization

1. **Polling Rate**
   - Client: Poll controller at high rate (60-120 Hz)
   - Send updates immediately on state change
   - Consider interpolation for smoother feel

2. **Network Optimization**
   - Keep packet size minimal
   - Use delta compression (send only changes)
   - Consider UDP with custom reliability for critical inputs

3. **Server Processing**
   - Process inputs on dedicated thread
   - Minimize time between receive and virtual controller update
   - Avoid blocking operations

### Multi-Controller Support

- ViGEmBus supports up to 4 virtual Xbox 360 controllers
- Assign each client a controller index (0-3)
- Include controller index in UDP packets
- Server maintains map of client IP → controller index

### Security Considerations

1. **Authentication**
   - Implement simple token-based authentication
   - Verify client identity before accepting inputs

2. **Rate Limiting**
   - Prevent spam/DOS attacks
   - Limit packets per second per client

3. **Encryption** (Optional)
   - For LAN gaming, might not be necessary
   - For internet play, consider DTLS (UDP with TLS)

### Error Handling

1. **Packet Loss**
   - UDP will drop packets
   - Design to handle missing updates gracefully
   - Most recent state is what matters

2. **Client Disconnection**
   - Detect timeout (no packets for X seconds)
   - Reset virtual controller to neutral state
   - Notify game/user of disconnection

3. **Controller Reconnection**
   - Handle client controller disconnect/reconnect
   - Maintain controller index assignment

---

## Development Roadmap

### Phase 1: Prototype (Single Local Controller)
1. Install ViGEmBus driver
2. Create C# console app
3. Use ViGEmClient to create virtual Xbox 360 controller
4. Read physical controller via XInput
5. Feed inputs to virtual controller
6. Test with a game

### Phase 2: Network Communication
1. Split into Client and Server applications
2. Implement UDP socket communication
3. Design packet format for controller state
4. Test over local network (same machine first)

### Phase 3: Multi-Controller Support
1. Add controller index to packets
2. Server manages multiple virtual controllers
3. Test with 2-4 clients simultaneously

### Phase 4: Optimization & Polish
1. Reduce latency
2. Add reconnection logic
3. Implement simple authentication
4. Create user-friendly configuration

### Phase 5: Testing & Refinement
1. Test with various games
2. Measure and optimize latency
3. Handle edge cases
4. User feedback and iteration

---

## Recommended Project Structure

```
remote-play/
├── src/
│   ├── Common/
│   │   ├── ControllerState.cs       # Shared data structures
│   │   ├── PacketSerializer.cs      # Serialization logic
│   │   └── NetworkConfig.cs         # Network settings
│   │
│   ├── Client/
│   │   ├── Program.cs               # Client entry point
│   │   ├── ControllerCapture.cs     # XInput wrapper
│   │   └── NetworkSender.cs         # UDP sender
│   │
│   └── Server/
│       ├── Program.cs               # Server entry point
│       ├── NetworkReceiver.cs       # UDP receiver
│       └── VirtualControllerManager.cs  # ViGEm wrapper
│
├── lib/
│   └── ViGEmClient.dll              # ViGEm library
│
├── docs/
│   └── RESEARCH.md                  # This file
│
└── README.md
```

---

## Key NuGet Packages

```xml
<!-- Server Side -->
<PackageReference Include="Nefarius.ViGEm.Client" Version="1.21.442" />

<!-- Both Client and Server -->
<!-- Built-in .NET networking, no additional packages needed -->
```

---

## Example Code Snippets

### Server: Creating a Virtual Controller

```csharp
using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;

// Initialize ViGEm client
var client = new ViGEmClient();

// Create virtual Xbox 360 controller
var controller = client.CreateXbox360Controller();

// Connect the virtual controller
controller.Connect();

// Update controller state
controller.SetButtonState(Xbox360Button.A, true);
controller.SetAxisValue(Xbox360Axis.LeftThumbX, 32767); // Max right
controller.SubmitReport(); // Apply changes

// Cleanup
controller.Disconnect();
client.Dispose();
```

### Client: Reading XInput

```csharp
using System.Runtime.InteropServices;

// XInput constants
const int XUSER_MAX_COUNT = 4;
const int ERROR_SUCCESS = 0;

[DllImport("xinput1_4.dll")]
static extern int XInputGetState(int dwUserIndex, ref XINPUT_STATE pState);

[StructLayout(LayoutKind.Sequential)]
struct XINPUT_STATE
{
    public uint dwPacketNumber;
    public XINPUT_GAMEPAD Gamepad;
}

[StructLayout(LayoutKind.Sequential)]
struct XINPUT_GAMEPAD
{
    public ushort wButtons;
    public byte bLeftTrigger;
    public byte bRightTrigger;
    public short sThumbLX;
    public short sThumbLY;
    public short sThumbRX;
    public short sThumbRY;
}

// Usage
XINPUT_STATE state = new XINPUT_STATE();
int result = XInputGetState(0, ref state); // Controller 0
if (result == ERROR_SUCCESS)
{
    // Controller is connected, read state.Gamepad
}
```

### Network: UDP Communication

```csharp
// Server (Receiver)
using System.Net;
using System.Net.Sockets;

var udpServer = new UdpClient(11000);
var remoteEP = new IPEndPoint(IPAddress.Any, 11000);

while (true)
{
    byte[] data = udpServer.Receive(ref remoteEP);
    // Deserialize and process controller data
}

// Client (Sender)
var udpClient = new UdpClient();
var serverEP = new IPEndPoint(IPAddress.Parse("192.168.1.100"), 11000);

byte[] data = SerializeControllerState(state);
udpClient.Send(data, data.Length, serverEP);
```

---

## Testing Strategy

1. **Local Testing**
   - Test on same machine first
   - Use localhost (127.0.0.1)

2. **LAN Testing**
   - Test on local network
   - Measure latency with ping

3. **Game Compatibility**
   - Test with various games (Steam games, Epic, etc.)
   - Verify all buttons/axes work correctly

4. **Performance Testing**
   - Monitor CPU usage
   - Measure input lag
   - Test with multiple simultaneous clients

---

## Resources

### Documentation
- [ViGEmBus GitHub](https://github.com/ViGEm/ViGEmBus)
- [ViGEmClient Documentation](https://github.com/ViGEm/ViGEmClient)
- [XInput Documentation](https://docs.microsoft.com/en-us/windows/win32/xinput/xinput-game-controller-apis-portal)
- [UDP Socket Programming in C#](https://docs.microsoft.com/en-us/dotnet/api/system.net.sockets.udpclient)

### Example Projects
- nefarius/VDX - ViGEm C# example
- NetInput - UDP gamepad streaming
- Moonlight/Sunshine - Full remote play solution

### Alternative Libraries
- vJoy - Alternative virtual joystick (older)
- Python vgamepad - For prototyping

---

## Potential Challenges

1. **Latency** - Network delay can affect gameplay
   - Solution: Optimize packet size, use UDP, local network preferred

2. **Packet Loss** - UDP doesn't guarantee delivery
   - Solution: Design for graceful degradation, most recent state matters

3. **Game Compatibility** - Some games might not recognize virtual controllers
   - Solution: ViGEmBus has excellent compatibility, but test thoroughly

4. **Multiple Controllers** - Managing 4+ simultaneous connections
   - Solution: Careful state management, dedicated network thread

5. **NAT/Firewall Issues** - For internet play
   - Solution: Port forwarding, UPNP, or relay server

---

## Next Steps

1. ✅ Research completed
2. ⬜ Set up development environment
3. ⬜ Install ViGEmBus driver
4. ⬜ Create Phase 1 prototype (local virtual controller)
5. ⬜ Implement network communication
6. ⬜ Test with friends over LAN
7. ⬜ Optimize and polish

---

## Conclusion

**Recommended Approach:**
- **Language:** C# with .NET
- **Virtual Controller:** ViGEmBus + ViGEmClient
- **Networking:** UDP with System.Net.Sockets
- **Input Capture:** XInput for Xbox controllers
- **Architecture:** Client-Server model with UDP streaming

This stack provides the best balance of:
- Performance (low latency)
- Development speed (C# is easier than C++)
- Compatibility (ViGEmBus works with all games)
- Maintainability (clean, modern codebase)

The project is definitely achievable and has been proven by similar projects like NetInput. Start with a simple prototype and iterate!
