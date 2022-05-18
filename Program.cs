using Cassia;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Proiect
{
    public static class Program
    {
        private static readonly Dictionary<int, ITerminalServicesSession> LocalSessions = new();

        public static void Main()
        {
            Console.WriteLine("Press any key to scan for active remote desktop sessions...");
            Console.ReadKey();

            CheckRdcConnections();
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

                LocalSessions[idNum].StopRemoteControl();
                LocalSessions.Remove(idNum);
            }

            Console.ReadKey();
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
    }
}