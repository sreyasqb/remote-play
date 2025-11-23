using System;
using System.Diagnostics;
using RemotePlay.Common;

namespace RemotePlay.Client;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("╔═══════════════════════════════════════════════════════════╗");
        Console.WriteLine("║         Remote Play - Client (Controller Sender)         ║");
        Console.WriteLine("╚═══════════════════════════════════════════════════════════╝");
        Console.WriteLine();
        
        // Get configuration from user
        Console.Write("Enter server IP address (default: 127.0.0.1): ");
        string? serverIp = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(serverIp))
        {
            serverIp = "127.0.0.1";
        }
        
        Console.Write($"Enter server port (default: {NetworkConfig.DEFAULT_PORT}): ");
        string? portInput = Console.ReadLine();
        int port = NetworkConfig.DEFAULT_PORT;
        if (!string.IsNullOrWhiteSpace(portInput) && int.TryParse(portInput, out int parsedPort))
        {
            port = parsedPort;
        }
        
        Console.Write("Enter controller index (0-3, default: 0): ");
        string? controllerInput = Console.ReadLine();
        int controllerIndex = 0;
        if (!string.IsNullOrWhiteSpace(controllerInput) && int.TryParse(controllerInput, out int parsedIndex))
        {
            controllerIndex = Math.Clamp(parsedIndex, 0, 3);
        }
        
        Console.WriteLine();
        Console.WriteLine($"Configuration:");
        Console.WriteLine($"  Server: {serverIp}:{port}");
        Console.WriteLine($"  Controller Index: {controllerIndex}");
        Console.WriteLine();
        
        // Initialize components
        using var xinput = new XInputCapture(controllerIndex);
        using var network = new NetworkSender(serverIp, port);
        
        // Check if controller is connected
        Console.WriteLine("Checking for Xbox controller...");
        if (!xinput.CheckConnection())
        {
            Console.WriteLine($"ERROR: No Xbox controller found at index {controllerIndex}");
            Console.WriteLine("Please connect an Xbox controller and try again.");
            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
            return;
        }
        
        Console.WriteLine($"✓ Xbox controller detected at index {controllerIndex}");
        Console.WriteLine();
        Console.WriteLine("Starting controller streaming...");
        Console.WriteLine("Press ESC to stop");
        Console.WriteLine();
        
        // Statistics
        long packetsSent = 0;
        var stopwatch = Stopwatch.StartNew();
        DateTime lastStatsUpdate = DateTime.Now;
        ControllerState? lastState = null;
        
        // Main loop
        bool running = true;
        while (running)
        {
            // Check for ESC key
            if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Escape)
            {
                running = false;
                break;
            }
            
            // Read controller state
            var state = xinput.GetState();
            
            if (state == null)
            {
                Console.WriteLine("Controller disconnected. Waiting for reconnection...");
                await Task.Delay(1000);
                
                if (!xinput.CheckConnection())
                {
                    continue;
                }
                
                Console.WriteLine("Controller reconnected!");
                continue;
            }
            
            // Send state if changed (or always send if configured)
            bool shouldSend = !NetworkConfig.SEND_ON_CHANGE;
            
            if (NetworkConfig.SEND_ON_CHANGE)
            {
                if (lastState == null || 
                    state.Value.Buttons != lastState.Value.Buttons ||
                    state.Value.LeftTrigger != lastState.Value.LeftTrigger ||
                    state.Value.RightTrigger != lastState.Value.RightTrigger ||
                    Math.Abs(state.Value.LeftThumbX - lastState.Value.LeftThumbX) > 500 ||
                    Math.Abs(state.Value.LeftThumbY - lastState.Value.LeftThumbY) > 500 ||
                    Math.Abs(state.Value.RightThumbX - lastState.Value.RightThumbX) > 500 ||
                    Math.Abs(state.Value.RightThumbY - lastState.Value.RightThumbY) > 500)
                {
                    shouldSend = true;
                }
            }
            
            if (shouldSend)
            {
                await network.SendControllerStateAsync(state.Value);
                packetsSent++;
                lastState = state;
                
                // Debug: Log first few packets
                if (packetsSent <= 5 || packetsSent % 100 == 0)
                {
                    Console.WriteLine();
                    Console.WriteLine($"[DEBUG] Sent packet #{packetsSent} to {serverIp}:{port}");
                }
            }
            
            // Update statistics every second
            if ((DateTime.Now - lastStatsUpdate).TotalSeconds >= 1.0)
            {
                double elapsed = stopwatch.Elapsed.TotalSeconds;
                double packetsPerSecond = packetsSent / elapsed;
                
                Console.SetCursorPosition(0, Console.CursorTop);
                Console.Write($"Packets sent: {packetsSent} | Rate: {packetsPerSecond:F1} pps | Active: {state.Value.HasInput()}    ");
                
                lastStatsUpdate = DateTime.Now;
            }
            
            // Polling interval
            await Task.Delay(NetworkConfig.POLLING_INTERVAL_MS);
        }
        
        Console.WriteLine();
        Console.WriteLine();
        Console.WriteLine("Shutting down...");
        Console.WriteLine($"Total packets sent: {packetsSent}");
        Console.WriteLine($"Runtime: {stopwatch.Elapsed.TotalSeconds:F1}s");
    }
}
