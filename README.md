# Remote Play - Xbox Controller Network Streaming

A **C# .NET** application that enables friends to connect their Xbox controllers over the network to a host PC, where they are recognized as virtual Xbox controllers by games.

## 🎮 Overview

This project allows you to:
- **Stream Xbox controller inputs** from client PCs to a host/server PC
- **Create virtual Xbox controllers** on the host that games recognize as real hardware
- **Support up to 4 simultaneous controllers** over the network
- **Low-latency UDP networking** optimized for real-time gaming

Perfect for couch co-op games when friends want to play remotely!

## 🏗️ Architecture

```
┌─────────────────┐                     ┌─────────────────┐
│  Client PC #1   │                     │   Server PC     │
│  ┌───────────┐  │                     │  ┌──────────┐   │
│  │ Physical  │  │   UDP Network       │  │ ViGEmBus │   │
│  │ Xbox Ctrl │──┼────────────────────>│  │ Virtual  │   │
│  └───────────┘  │  Controller State   │  │ Xbox 360 │   │
└─────────────────┘                     │  └────┬─────┘   │
                                        │       │         │
┌─────────────────┐                     │       v         │
│  Client PC #2   │                     │  ┌─────────┐    │
│  ┌───────────┐  │                     │  │  Game   │    │
│  │ Physical  │──┼────────────────────>│  │ (sees   │    │
│  │ Xbox Ctrl │  │                     │  │  real   │    │
│  └───────────┘  │                     │  │ ctrls!) │    │
└─────────────────┘                     │  └─────────┘    │
                                        └─────────────────┘
```

## ✨ Features

- ✅ **Real Xbox Controller Emulation** - Games see actual Xbox 360 controllers
- ✅ **Low Latency** - UDP-based networking with ~8ms polling
- ✅ **Multi-Controller Support** - Up to 4 controllers simultaneously
- ✅ **Smart Updates** - Only sends data when controller state changes
- ✅ **Auto-Reconnection** - Handles controller disconnects gracefully
- ✅ **Client Timeout Detection** - Auto-cleanup of disconnected clients
- ✅ **Statistics & Monitoring** - Real-time packet statistics

## 📋 Prerequisites

### For Server (Host PC)

1. **ViGEmBus Driver** - Required for virtual controller creation
   - Download from: [ViGEm/ViGEmBus Releases](https://github.com/ViGEm/ViGEmBus/releases)
   - Install the `.msi` file
   - Reboot after installation (recommended)

2. **.NET 8.0 Runtime** (included in SDK)
   - Already installed if you built the project

### For Client PCs

1. **Xbox Controller** - Physical Xbox One or Xbox 360 controller
   - Must be connected via USB or wireless adapter
   - Verified by Windows (check in Device Manager or joy.cpl)

2. **.NET 8.0 Runtime**
   - Already included if you built the project

### Network Requirements

- **Local Network (LAN)** - Recommended for best performance
- **Open Port** - Default UDP port 11000 (configurable)
- **Firewall Rules** - Allow UDP traffic on the chosen port

## 🚀 Quick Start

### Building the Project

1. **Clone or navigate to the project directory**
   ```bash
   cd d:/Coding/remote-play
   ```

2. **Build the solution**
   ```powershell
   dotnet build
   ```

3. **Build in Release mode (for production use)**
   ```powershell
   dotnet build -c Release
   ```

### Running the Server (Host PC)

1. **Make sure ViGEmBus is installed** (see Prerequisites)

2. **Run the server**
   ```powershell
   dotnet run --project src/Server/RemotePlay.Server.csproj
   ```

   Or build and run the executable:
   ```powershell
   cd src/Server/bin/Debug/net8.0
   ./RemotePlay.Server.exe
   ```

3. **Configure the server**
   - Enter the port to listen on (default: 11000)
   - Note the displayed local IP address(es)
   - Server will wait for client connections

### Running the Client (Friend's PC)

1. **Make sure Xbox controller is connected**

2. **Run the client**
   ```powershell
   dotnet run --project src/Client/RemotePlay.Client.csproj
   ```

   Or build and run the executable:
   ```powershell
   cd src/Client/bin/Debug/net8.0
   ./RemotePlay.Client.exe
   ```

3. **Configure the client**
   - Enter the server IP address (from server display)
   - Enter the port (default: 11000)
   - Choose controller index (0-3)
   - Client will start streaming controller input

### Testing

1. **Start the server first**
2. **Start one or more clients**
3. **Open Windows Game Controller settings** (`joy.cpl`) on the server PC
   - You should see virtual Xbox 360 controllers appearing
   - Press buttons on the client controller to verify

## 🎯 Usage

### Server Controls

- **ESC** - Stop the server
- **S** - Display detailed client statistics

### Client Controls

- **ESC** - Disconnect and stop the client
- Controller inputs are automatically streamed while connected

## ⚙️ Configuration

### Network Settings

Edit `src/Common/NetworkConfig.cs`:

```csharp
public const int DEFAULT_PORT = 11000;          // UDP port
public const int MAX_CONTROLLERS = 4;           // Max simultaneous controllers
public const int CLIENT_TIMEOUT_MS = 5000;      // Client timeout (5 seconds)
public const int POLLING_INTERVAL_MS = 8;       // Polling rate (~120 Hz)
public const bool SEND_ON_CHANGE = true;        // Only send when state changes
```

### Controller Mapping

The application uses standard XInput mapping:
- **Buttons**: A, B, X, Y, Start, Back, LB, RB, LS, RS
- **D-Pad**: Up, Down, Left, Right
- **Triggers**: LT, RT (0-255)
- **Analog Sticks**: Left/Right stick X/Y (-32768 to 32767)

## 🔧 Troubleshooting

### Server Issues

**"Failed to initialize ViGEmBus client"**
- Install ViGEmBus driver from the official releases
- Reboot after installation
- Run server as Administrator (if needed)

**"Failed to start UDP server"**
- Port may be in use by another application
- Try a different port number
- Check firewall settings

### Client Issues

**"No Xbox controller found"**
- Ensure controller is connected (USB or wireless)
- Check in Windows Settings → Devices → Bluetooth & other devices
- Verify in joy.cpl (Game Controllers)
- Try a different USB port
- Update Xbox Accessories app

**Connection not working**
- Verify server IP address is correct
- Check both PCs are on the same network
- Verify firewall allows UDP traffic on the port
- Try pinging the server from client

### Performance Issues

**High latency / input lag**
- Use a wired network connection instead of WiFi
- Reduce `POLLING_INTERVAL_MS` (but may increase CPU usage)
- Ensure no bandwidth-heavy applications are running
- Check for network congestion

**Choppy input**
- Check client statistics for packet drop
- Improve network quality
- Set `SEND_ON_CHANGE = false` for constant updates

## 📁 Project Structure

```
remote-play/
├── src/
│   ├── Common/                    # Shared library
│   │   ├── ControllerState.cs    # Controller data structure
│   │   ├── PacketSerializer.cs    # Network serialization
│   │   └── NetworkConfig.cs       # Configuration constants
│   │
│   ├── Client/                    # Client application
│   │   ├── Program.cs             # Main client entry point
│   │   ├── XInputCapture.cs       # XInput controller reading
│   │   └── NetworkSender.cs       # UDP transmission
│   │
│   └── Server/                    # Server application
│       ├── Program.cs                     # Main server entry point
│       ├── VirtualControllerManager.cs    # ViGEm integration
│       └── NetworkReceiver.cs             # UDP reception
│
├── RemotePlay.sln                 # Visual Studio solution
├── RESEARCH.md                    # Technical research document
└── README.md                      # This file
```

## 🔬 Technical Details

### Network Protocol

- **Transport**: UDP (connectionless, low latency)
- **Packet Format**: `[MAGIC][VERSION][CONTROLLER_STATE]`
  - Magic Number: 0x52504C59 ("RPLY")
  - Protocol Version: 1
  - Controller State: Binary serialized struct
- **Packet Size**: ~21 bytes (very efficient)
- **Validation**: Magic number + version check

### Performance Metrics

- **Polling Rate**: ~120 Hz (8ms intervals)
- **Network Rate**: Variable (only sends on change by default)
- **Latency**: Typically <20ms on LAN
- **Bandwidth**: ~5-10 KB/s per controller (very low)

### Technologies Used

- **.NET 8.0** - Modern C# framework
- **ViGEmBus** - Virtual gamepad emulation driver
- **Nefarius.ViGEm.Client** - C# wrapper for ViGEmBus
- **XInput** - Windows Xbox controller API
- **UDP Sockets** - Low-latency networking

## 🎮 Game Compatibility

This solution works with **any game that supports Xbox controllers**, including:
- ✅ Steam games
- ✅ Epic Games Store
- ✅ Xbox Game Pass / Microsoft Store games
- ✅ Standalone games with XInput support

Games see the virtual controllers as real Xbox 360 controllers connected to the PC.

## 🚧 Limitations

- **Windows Only** - Server requires ViGEmBus (Windows driver)
- **Xbox Controllers Only** - Clients need XInput-compatible controllers
- **LAN Recommended** - Works over internet but latency may be high
- **Max 4 Controllers** - Limited by ViGEmBus and XInput design

## 🔮 Future Enhancements

Potential improvements:
- [ ] PlayStation controller support (DualShock/DualSense)
- [ ] Web-based configuration interface
- [ ] Encryption for internet play
- [ ] Haptic feedback (vibration) from server to client
- [ ] Controller remapping options
- [ ] Mobile client support (theoretical)

## 📝 License

This project is for educational and personal use. Please respect the licenses of:
- ViGEmBus (Nefarius Software Solutions)
- Any games you use this software with

## 🙏 Credits

- **ViGEmBus** - Virtual gamepad emulation framework
- **Research** - Based on open-source projects like NetInput and Moonlight

## 📧 Support

For issues or questions:
1. Check the Troubleshooting section
2. Verify ViGEmBus is properly installed
3. Test with `joy.cpl` to verify controller detection
4. Check firewall and network settings

---

**Enjoy remote couch co-op gaming!** 🎮🎮🎮🎮
