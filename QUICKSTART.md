# Quick Start Guide

## 🚀 Get Started in 5 Minutes

### Step 1: Install ViGEmBus (Server PC Only)

**The server MUST have ViGEmBus installed to create virtual controllers.**

1. Download: https://github.com/ViGEm/ViGEmBus/releases/latest
2. Install the `.msi` file
3. Reboot (recommended)

See [VIGEM_INSTALLATION.md](VIGEM_INSTALLATION.md) for detailed instructions.

### Step 2: Build the Project

```powershell
# Navigate to project directory
cd d:/Coding/remote-play

# Build everything
dotnet build -c Release
```

### Step 3: Run the Server

On the **host PC** (where the game runs):

```powershell
# Run from project root
dotnet run --project src/Server/RemotePlay.Server.csproj

# OR run the executable
cd src/Server/bin/Release/net8.0
./RemotePlay.Server.exe
```

**Configuration:**
- Press ENTER for default port (11000)
- Note the displayed IP address (you'll need this for clients)

### Step 4: Run the Client

On **each friend's PC**:

1. **Plug in Xbox controller** (USB or wireless)
2. **Run the client:**

```powershell
# Run from project root
dotnet run --project src/Client/RemotePlay.Client.csproj

# OR run the executable
cd src/Client/bin/Release/net8.0
./RemotePlay.Client.exe
```

**Configuration:**
- Enter the server IP (from Step 3)
- Press ENTER for default port (11000)
- Choose controller index (0 for first, 1 for second, etc.)

### Step 5: Test It!

On the **server PC**:

1. Open **Game Controllers** settings:
   - Press `Win + R`
   - Type `joy.cpl`
   - Press Enter

2. You should see **Xbox 360 Controller(s)** appear

3. Select one and click **Properties**

4. **Press buttons on the client controller** - you should see them light up!

### Step 6: Play Games!

Launch any game that supports Xbox controllers on the server PC. The game will see the virtual controllers as real hardware!

---

## 🎮 Using with Games

### Which Controller Index to Use?

- **Client #1**: Use index 0 (becomes Controller 1 in games)
- **Client #2**: Use index 1 (becomes Controller 2 in games)
- **Client #3**: Use index 2 (becomes Controller 3 in games)
- **Client #4**: Use index 3 (becomes Controller 4 in games)

### Supported Games

Works with **any game that supports Xbox controllers**:

- ✅ **Steam**: All games with "Full Controller Support"
- ✅ **Epic Games Store**: Games with controller support
- ✅ **Xbox Game Pass**: All games
- ✅ **Standalone games**: Anything using XInput

### Testing in Steam

1. Start server
2. Start client(s)
3. Open **Steam Big Picture Mode**
4. Go to **Settings** → **Controller** → **Test Controllers**
5. You should see your virtual controllers!

---

## 🔧 Firewall Configuration

### Windows Firewall (Server PC)

Allow incoming UDP traffic on port 11000:

```powershell
# Run as Administrator
New-NetFirewallRule -DisplayName "RemotePlay Server" -Direction Inbound -Protocol UDP -LocalPort 11000 -Action Allow
```

Or manually:
1. Windows Security → Firewall & network protection
2. Advanced settings
3. Inbound Rules → New Rule
4. Rule Type: Port
5. Protocol: UDP, Port: 11000
6. Allow the connection
7. Apply to all profiles
8. Name it "RemotePlay Server"

---

## 📊 Monitoring

### Server Statistics

While server is running:
- Press **S** to show detailed statistics
- Shows: Client IPs, controller indices, packet counts

### Client Statistics

The client displays:
- Packets sent per second
- Whether controller has active input
- Connection status

---

## ⚠️ Troubleshooting

### "No Xbox controller found"

**On client PC:**
1. Open Device Manager
2. Check for "Xbox Gaming Device" or similar
3. Try a different USB port
4. Update Xbox Accessories app from Microsoft Store

### "Failed to initialize ViGEmBus"

**On server PC:**
1. Make sure ViGEmBus is installed (see Step 1)
2. Reboot after installation
3. Try running server as Administrator
4. Check Device Manager for "Virtual Gamepad Emulation Bus"

### Clients Can't Connect

1. **Verify IP address:** Make sure clients use correct server IP
2. **Same network:** Both PCs must be on same network/WiFi
3. **Firewall:** Add firewall rule (see above)
4. **Ping test:** From client, run `ping <server-ip>`

### High Latency

- Use **wired network** instead of WiFi
- Close bandwidth-heavy applications
- Reduce distance to router
- Check for network congestion

---

## 🎯 Tips & Tricks

### For Best Performance

1. **Use wired Ethernet** on both client and server
2. **Close unnecessary apps** to free up bandwidth
3. **Same subnet** - keep all PCs on same network segment
4. **Quality router** - invest in a good gaming router

### For Multiple Controllers

1. Start server once
2. Start client #1 with index 0
3. Start client #2 with index 1
4. Start client #3 with index 2
5. Start client #4 with index 3

Each client gets its own virtual controller on the server.

### Distributing to Friends

You can send them just the executable:

**Client PC** needs:
- `src/Client/bin/Release/net8.0/` folder contents
- .NET 8 Runtime (or send as self-contained)

**Server PC** needs:
- `src/Server/bin/Release/net8.0/` folder contents
- .NET 8 Runtime
- ViGEmBus driver installed

---

## 📦 Creating Standalone Executables

To create executables that don't require .NET installation:

```powershell
# Build client as standalone
dotnet publish src/Client/RemotePlay.Client.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true

# Build server as standalone
dotnet publish src/Server/RemotePlay.Server.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

Output will be in:
- `src/Client/bin/Release/net8.0/win-x64/publish/`
- `src/Server/bin/Release/net8.0/win-x64/publish/`

Single .exe files that can run without .NET installed!

---

**You're all set! Enjoy remote couch co-op! 🎮**
