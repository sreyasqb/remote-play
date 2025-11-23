# ViGEmBus Installation Guide

## What is ViGEmBus?

ViGEmBus (Virtual Gamepad Emulation Bus) is a Windows kernel-mode driver that allows applications to create virtual Xbox 360 and Xbox One controllers. Games see these virtual controllers as real hardware.

## Installation Steps

### Option 1: Download and Install (Recommended)

1. **Download the latest release**
   - Visit: https://github.com/ViGEm/ViGEmBus/releases
   - Download the latest `.msi` installer (e.g., `ViGEmBus_Setup_x64.msi`)

2. **Run the installer**
   - Double-click the downloaded `.msi` file
   - Follow the installation wizard
   - Accept the license agreement
   - Click "Install"

3. **Reboot your computer** (recommended)
   - While not always required, rebooting ensures the driver loads correctly

4. **Verify installation**
   - Open Device Manager (Win + X → Device Manager)
   - Look for "System devices" → "Virtual Gamepad Emulation Bus"
   - If you see it, the installation was successful!

### Option 2: Using WinGet (Command Line)

```powershell
# Install via WinGet (if available)
winget install Nefarius.ViGEmBus
```

## Troubleshooting

### Driver Not Installing

- **Run as Administrator**: Right-click the installer → "Run as administrator"
- **Disable Secure Boot**: Some systems require disabling Secure Boot in BIOS
- **Windows 10/11**: Ensure you're running a supported version

### Driver Not Loading

- **Check Device Manager**: Look for yellow exclamation marks
- **Reboot**: A simple reboot often fixes the issue
- **Reinstall**: Uninstall completely, reboot, then reinstall

### Test Driver Installation

Run this PowerShell command to check if the driver is loaded:

```powershell
Get-PnpDevice -FriendlyName "*Virtual Gamepad*"
```

You should see:
```
Status     Class    FriendlyName
------     -----    ------------
OK         System   Virtual Gamepad Emulation Bus
```

## Uninstallation

If you need to uninstall ViGEmBus:

1. **Using Windows Settings**
   - Settings → Apps → Apps & features
   - Find "ViGEmBus Driver"
   - Click Uninstall

2. **Using Control Panel**
   - Control Panel → Programs → Programs and Features
   - Find "ViGEmBus Driver"
   - Uninstall

3. **Reboot** after uninstallation

## Important Notes

- ⚠️ **ViGEmBus was retired in November 2023** due to trademark issues
- ✅ **The driver still works perfectly** for existing installations
- ℹ️ A successor called "VirtualPad" is in development
- ✅ Safe to use for personal projects

## Alternative: Manual Driver Installation

If the MSI installer doesn't work:

1. Download the driver files from the GitHub releases
2. Extract to a folder
3. Open Device Manager
4. Action → Add legacy hardware
5. Install from list or specific location
6. Have Disk → Browse to extracted folder
7. Select the `.inf` file

## Links

- **Official GitHub**: https://github.com/ViGEm/ViGEmBus
- **Releases**: https://github.com/ViGEm/ViGEmBus/releases
- **Documentation**: https://vigem.org/
- **Support Community**: https://discord.vigem.org/

---

**After installation, you're ready to run the Remote Play server!**
