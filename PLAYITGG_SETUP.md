# Playit.gg Setup Guide for Remote Play

## What You Need

1. **Playit.gg account** (free): https://playit.gg/
2. **Playit.gg agent** installed on your PC

## Setup Instructions

### Step 1: Configure Playit.gg Tunnel

1. **Open Playit.gg agent**
2. **Create a new tunnel:**
   - Click "Add Tunnel" or "New Tunnel"
   - **Tunnel Type:** Choose "UDP"
   - **Local Port:** `11000`
   - **Local IP:** `127.0.0.1` (or your local IP like `192.168.68.108`)
   
3. **Note the tunnel details:**
   - **Tunnel Address:** (e.g., `example.playit.gg` or IP like `147.185.221.20`)
   - **Tunnel Port:** (e.g., `54321`)

### Step 2: Run the Server

```powershell
cd d:/Coding/remote-play
dotnet run --project src/Server/RemotePlay.Server.csproj
```

When prompted:
- **Port:** Press Enter (use default `11000`)

### Step 3: Share Connection Info with Friends

Give your friends:
1. **Server Address:** `<playit.gg tunnel address>` (from Step 1)
2. **Server Port:** `<playit.gg tunnel port>` (from Step 1)

**Example:**
- Address: `example.playit.gg`
- Port: `54321`

### Step 4: Friend Runs Client

Your friend runs:
```powershell
dotnet run --project src/Client/RemotePlay.Client.csproj
```

When prompted:
- **Server IP:** `example.playit.gg` (your playit.gg address)
- **Port:** `54321` (your playit.gg port)
- **Controller Index:** `0`

## Troubleshooting

### "No data received" on server

**Check these:**

1. **Playit.gg tunnel is UDP, not TCP**
   - In playit.gg agent, verify tunnel type is "UDP"
   
2. **Client is sending to correct address/port**
   - Friend must use playit.gg tunnel address and port
   - NOT your public IP (49.205.146.178)
   - NOT port 11000 (unless that's what playit.gg assigned)

3. **Controller is connected and active**
   - Client should show: `Active: True`
   - Press buttons to generate input

4. **Playit.gg agent is running**
   - Make sure the agent is running and tunnel shows "Connected"

### Server Debug Output

With the latest code, the server will show:
```
[DEBUG] Received XX bytes from <IP>:<Port>
```

If you see this, data is arriving!

If you DON'T see this, the issue is:
- Playit.gg tunnel not configured correctly
- Client sending to wrong address/port
- Playit.gg tunnel is TCP instead of UDP

### Test the Tunnel

You can test if the tunnel works with a simple UDP echo test:

**On server (PowerShell):**
```powershell
# Listen for UDP packets
$udp = New-Object System.Net.Sockets.UdpClient 11000
$endpoint = New-Object System.Net.IPEndPoint([System.Net.IPAddress]::Any, 0)
Write-Host "Listening on UDP 11000..."
while($true) {
    $data = $udp.Receive([ref]$endpoint)
    $text = [System.Text.Encoding]::ASCII.GetString($data)
    Write-Host "Received from $($endpoint): $text"
}
```

**On client (PowerShell):**
```powershell
# Send test packet
$udp = New-Object System.Net.Sockets.UdpClient
$bytes = [System.Text.Encoding]::ASCII.GetBytes("TEST")
$udp.Send($bytes, $bytes.Length, "<playit.gg-address>", <playit.gg-port>)
Write-Host "Sent test packet"
```

If this works, the tunnel is fine and the issue is in the application.

## Common Mistakes

❌ Using TCP tunnel instead of UDP  
❌ Client connecting to public IP instead of playit.gg address  
❌ Client using port 11000 instead of playit.gg assigned port  
❌ Playit.gg agent not running  
❌ Tunnel pointing to wrong local port  

✅ Use UDP tunnel  
✅ Client uses playit.gg address and port  
✅ Server listens on port 11000  
✅ Playit.gg agent running and connected  

---

**Still not working?** Share:
1. Playit.gg tunnel configuration screenshot
2. Client console output
3. Server console output (with [DEBUG] messages)
