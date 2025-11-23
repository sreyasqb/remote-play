using System;
using System.Net;
using System.Net.Sockets;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Open.Nat;
using RemotePlay.Common;

namespace RemotePlay.Server;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("╔═══════════════════════════════════════════════════════════╗");
        Console.WriteLine("║      Remote Play - Server (Virtual Controller Host)      ║");
        Console.WriteLine("╚═══════════════════════════════════════════════════════════╝");
        Console.WriteLine();
        
        // Get port configuration
        Console.Write($"Enter listening port (default: {NetworkConfig.DEFAULT_PORT}): ");
        string? portInput = Console.ReadLine();
        int port = NetworkConfig.DEFAULT_PORT;
        if (!string.IsNullOrWhiteSpace(portInput) && int.TryParse(portInput, out int parsedPort))
        {
            port = parsedPort;
        }
        
        Console.WriteLine();
        Console.WriteLine($"Configuration:");
        Console.WriteLine($"  Port: {port}");
        Console.WriteLine($"  Max Controllers: {NetworkConfig.MAX_CONTROLLERS}");
        Console.WriteLine($"  Client Timeout: {NetworkConfig.CLIENT_TIMEOUT_MS}ms");
        Console.WriteLine();
        
        // Display local IP addresses
        Console.WriteLine("Local IP Addresses:");
        try
        {
            var hostName = Dns.GetHostName();
            var addresses = Dns.GetHostAddresses(hostName);
            foreach (var addr in addresses)
            {
                if (addr.AddressFamily == AddressFamily.InterNetwork)
                {
                    Console.WriteLine($"  - {addr}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Unable to determine IP addresses: {ex.Message}");
        }
        Console.WriteLine();

        // Internet Play Setup (UPnP & Public IP)
        Console.WriteLine("Configuring for Internet Play...");
        try 
        {
            // 1. Get Public IP
            Console.Write("  Fetching Public IP... ");
            using (var httpClient = new HttpClient())
            {
                try 
                {
                    string publicIp = await httpClient.GetStringAsync("https://api.ipify.org");
                    Console.WriteLine(publicIp);
                    Console.WriteLine($"  -> Share this IP with your friends: {publicIp}");
                }
                catch
                {
                    Console.WriteLine("Failed (Could not reach ipify.org)");
                }
            }

            // 2. Setup UPnP
            Console.Write($"  Attempting UPnP Port Forwarding for UDP {port}... ");
            try
            {
                var discoverer = new NatDiscoverer();
                var cts = new CancellationTokenSource(5000); // 5 second timeout
                var device = await discoverer.DiscoverDeviceAsync(PortMapper.Upnp, cts);
                
                await device.CreatePortMapAsync(new Mapping(Protocol.Udp, port, port, "RemotePlay Server"));
                Console.WriteLine("Success!");
                Console.WriteLine("  -> Router configured automatically. Friends can connect directly.");
            }
            catch (NatDeviceNotFoundException)
            {
                Console.WriteLine("No UPnP device found.");
                Console.WriteLine("  -> You may need to manually forward port " + port + " on your router.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed ({ex.Message})");
                Console.WriteLine("  -> You may need to manually forward port " + port + " on your router.");
            }
        }
        catch (Exception ex)
        {
             Console.WriteLine($"Error configuring internet play: {ex.Message}");
        }
        Console.WriteLine();
        
        // Initialize components
        VirtualControllerManager? controllerManager = null;
        NetworkReceiver? networkReceiver = null;
        
        try
        {
            Console.WriteLine("Initializing ViGEmBus virtual controller manager...");
            controllerManager = new VirtualControllerManager();
            Console.WriteLine();
            
            Console.WriteLine("Starting network receiver...");
            networkReceiver = new NetworkReceiver(controllerManager, port);
            
            // Event handler for received states
            long totalPacketsReceived = 0;
            DateTime lastStatsTime = DateTime.Now;
            
            networkReceiver.OnControllerStateReceived += (state, client) =>
            {
                totalPacketsReceived++;
                
                // Update stats every 2 seconds
                if ((DateTime.Now - lastStatsTime).TotalSeconds >= 2.0)
                {
                    var stats = networkReceiver.GetClientStats();
                    Console.SetCursorPosition(0, Console.CursorTop);
                    Console.Write($"Total packets: {totalPacketsReceived} | Active clients: {stats.Count}    ");
                    lastStatsTime = DateTime.Now;
                }
            };
            
            Console.WriteLine();
            Console.WriteLine("═══════════════════════════════════════════════════════════");
            Console.WriteLine("Server is running! Waiting for clients...");
            Console.WriteLine("Press ESC to stop the server");
            Console.WriteLine("═══════════════════════════════════════════════════════════");
            Console.WriteLine();
            
            // Start receiver in background
            var cancellationTokenSource = new CancellationTokenSource();
            var receiverTask = networkReceiver.StartAsync(cancellationTokenSource.Token);
            
            // Wait for ESC key
            bool running = true;
            while (running)
            {
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true);
                    if (key.Key == ConsoleKey.Escape)
                    {
                        running = false;
                    }
                    else if (key.Key == ConsoleKey.S)
                    {
                        // Print detailed statistics
                        Console.WriteLine();
                        Console.WriteLine("═══════════════════════════════════════════════════════════");
                        Console.WriteLine("Client Statistics:");
                        var stats = networkReceiver.GetClientStats();
                        if (stats.Count == 0)
                        {
                            Console.WriteLine("  No active clients");
                        }
                        else
                        {
                            foreach (var kvp in stats)
                            {
                                Console.WriteLine($"  Controller #{kvp.Key}:");
                                Console.WriteLine($"    Client: {kvp.Value.EndPoint}");
                                Console.WriteLine($"    Packets: {kvp.Value.PacketsReceived}");
                            }
                        }
                        Console.WriteLine("═══════════════════════════════════════════════════════════");
                        Console.WriteLine();
                    }
                }
                
                await Task.Delay(100);
            }
            
            // Shutdown
            Console.WriteLine();
            Console.WriteLine("Shutting down server...");
            cancellationTokenSource.Cancel();
            
            try
            {
                await receiverTask;
            }
            catch (OperationCanceledException)
            {
                // Expected
            }
            
            Console.WriteLine($"Total packets received: {totalPacketsReceived}");
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            Console.WriteLine($"FATAL ERROR: {ex.Message}");
            Console.WriteLine();
            Console.WriteLine("Stack trace:");
            Console.WriteLine(ex.StackTrace);
            Console.WriteLine();
            
            if (ex.Message.Contains("ViGEm") || ex.Message.Contains("driver"))
            {
                Console.WriteLine("═══════════════════════════════════════════════════════════");
                Console.WriteLine("IMPORTANT: Make sure ViGEmBus driver is installed!");
                Console.WriteLine();
                Console.WriteLine("Download and install from:");
                Console.WriteLine("https://github.com/ViGEm/ViGEmBus/releases");
                Console.WriteLine();
                Console.WriteLine("Install the .msi file and restart this application.");
                Console.WriteLine("═══════════════════════════════════════════════════════════");
            }
        }
        finally
        {
            // Cleanup
            networkReceiver?.Dispose();
            controllerManager?.Dispose();
        }
        
        Console.WriteLine();
        Console.WriteLine("Server stopped. Press any key to exit...");
        Console.ReadKey();
    }
}
