using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;

using ChatClassLibrary;

namespace ChatServer
{
    /// <summary>
    /// A wrapper for ChatClassLibrary.MessageServer that runs in a console window.
    /// </summary>
    public class ChatServerConsole
    {
        /// <summary>
        /// Print out the list of IP addresses of the local computer to the console and let
        /// the user pick which one to use.
        /// </summary>
        /// <param name="autoSelect">Print the list and immediately return IPAddress.Any.</param>
        /// <param name="ipv4Only">Lists only IPv4 addresses, no IPv6.</param>
        /// <returns>An IPAddress that the user selected, or IPAddress.Any if autoSelect.</returns>
        public static IPAddress SelectLocalIPAddress(bool autoSelect = true, bool ipv4Only = true)
        {
            var localHostName = Dns.GetHostName();
            var ipHostInfo = Dns.GetHostEntry(localHostName);
            var ipAddressList = ipHostInfo.AddressList.ToList();
            if (ipv4Only)
            {   // Filter for only IPv4 addresses.
                const AddressFamily v4 = AddressFamily.InterNetwork;
                ipAddressList = ipAddressList.Where(ip => ip.AddressFamily == v4).ToList();
            }
            ipAddressList.Insert(0, IPAddress.Any);
            ipAddressList.Insert(1, IPAddress.Parse("127.0.0.1"));

            Console.WriteLine("IP addresses of the local machine:");
            for (int i = 0; i < ipAddressList.Count; i++)
                Console.WriteLine("  {0} - {1}", i, ipAddressList[i]);
            Console.WriteLine("-------------------------------------------------------------------------------");

            if (autoSelect)  // Auto-select 0.0.0.0
                return IPAddress.Any;

            int choice = -1;
            int count = ipAddressList.Count;
            Console.Write("Select an IP address to use: ");
            while (!int.TryParse(Console.ReadLine(), out choice) || choice >= count || choice < 0)
                Console.Write("Invalid choice. Please enter a number in range [0, {0}]: ", count);

            return ipAddressList[choice];
        }

        //--------------------------------------------------------------------------------------//

        /// <summary>
        /// Starts the server program.
        /// </summary>
        public static void Main(string[] args)
        {
            Console.WriteLine("Initializing Chat Server Console...");

            var localAddress = SelectLocalIPAddress();
            var server = new MessageServer(localAddress);
            
            server.StartListening();
            
            Console.WriteLine("Press ENTER to exit...");
            Console.ReadLine();
        }
    }
}
