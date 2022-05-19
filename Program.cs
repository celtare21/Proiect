using Cassia;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;

namespace Proiect
{
    public static class Program
    {
        private static readonly Dictionary<int, ITerminalServicesSession> LocalSessions = new();

        public static void Main()
        {
            int numOption;

            while (true)
            {
                Console.WriteLine("1. Check RDC Connections");
                Console.WriteLine("2. Check LAN connections");
                var option = Console.ReadLine();

                if (!int.TryParse(option, out numOption))
                {
                    Console.Clear();
                    continue;
                }

                break;
            }

            switch (numOption)
            {
                case 1:
                    CheckRdcConnections();
                    break;
                case 2:
                    CheckLanConnection();
                    break;
            }

            Console.ReadKey();
        }

        private static void CheckRdcConnections()
        {
            while (IsRdcActive())
            {
                Console.Write("Type the session id you want to close: ");
                var id = Console.ReadLine();

                if (!int.TryParse(id, out var idNum))
                {
                    Console.WriteLine("Wrong session id!");
                    continue;
                }

                Console.WriteLine($"Stopped connection with session id {idNum}");

                LocalSessions[idNum].StopRemoteControl();
                LocalSessions.Remove(idNum);

                Console.ReadKey();
            }
        }

        private static bool IsRdcActive()
        {
            var manager = new TerminalServicesManager();

            using (var server = manager.GetLocalServer())
            {
                server.Open();

                foreach (var session in server.GetSessions().Where(session =>
                             session.ConnectionState is ConnectionState.Active or ConnectionState.Connected &&
                             System.Security.Principal.WindowsIdentity.GetCurrent().Name != session.UserAccount.Value))
                {
                    Console.WriteLine($"Current users connected: {session.UserAccount.Value} with session id: {session.SessionId}");
                    LocalSessions.Add(session.SessionId, session);
                }

                if (LocalSessions.Count != 0)
                    return true;

                Console.WriteLine("No connections found!");
                Console.WriteLine("Press any key to quit...");

                return false;
            }
        }

        private static void CheckLanConnection()
        {
            var networkConnection = IsNetworkAvailable();
            Console.WriteLine($"LAN adapter status: {networkConnection}");
            Console.WriteLine("Press ENTER to toggle status or any other key to exit");
            var key = Console.ReadKey();

            if (key.Key == ConsoleKey.Enter)
                ToggleAdapterStatus(!networkConnection);
        }

        private static bool IsNetworkAvailable()
        {
            if (!NetworkInterface.GetIsNetworkAvailable())
                return false;

            return (from ni in NetworkInterface.GetAllNetworkInterfaces()
                where ni.OperationalStatus == OperationalStatus.Up && ni.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                      ni.NetworkInterfaceType != NetworkInterfaceType.Tunnel
                where ni.Speed >= 0
                where ni.Description.IndexOf("virtual", StringComparison.OrdinalIgnoreCase) < 0 && ni.Name.IndexOf("virtual", StringComparison.OrdinalIgnoreCase) < 0
                select ni).Any(ni => !ni.Description.Equals("Microsoft Loopback Adapter", StringComparison.OrdinalIgnoreCase));
        }

        private static void ToggleAdapterStatus(bool enable)
        {
            const string interfaceName = "Ethernet";
            var option = enable ? "enable" : "disable";
            var process = new System.Diagnostics.Process();
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "netsh",
                Arguments = $"interface set interface \"{interfaceName}\" {option}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            process.StartInfo = startInfo;
            process.EnableRaisingEvents = true;

            process.Start();
            process.WaitForExit();

            Console.WriteLine($"Connection status: {option}");
            Console.WriteLine("Press any key to quit...");
        }
    }
}