using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using ChatClassLibrary;

namespace ChatServer
{
    public class ServerProgram
    {
        public static Hashtable ClientTable = new Hashtable();


        public static IPEndPoint SelectLocalIPEndPoint(int port, bool ipv4Only = true)
        {
            string localHostName = Dns.GetHostName();
            IPHostEntry ipHostInfo = Dns.GetHostEntry(localHostName);
            List<IPAddress> ipAddressList = ipHostInfo.AddressList.ToList();
            if (ipv4Only)
            {
                AddressFamily IPv4 = AddressFamily.InterNetwork;
                ipAddressList = ipAddressList.Where(ip => ip.AddressFamily == IPv4).ToList();
            }
            ipAddressList.Insert(0, IPAddress.Any);
            ipAddressList.Insert(1, IPAddress.Parse("127.0.0.1"));

            Console.WriteLine("IP addresses of the local machine:");
            for (int i = 0; i < ipAddressList.Count; i++)
                Console.WriteLine("  {0} - {1}", i, ipAddressList[i]);
            Console.WriteLine("-------------------------------------------------------------------------------");

            int choice = -1;

            Console.Write("Select an IP address to use: ");
            while (!int.TryParse(Console.ReadLine(), out choice) ||
                   choice >= ipAddressList.Count || choice < 0)
            {
                Console.Write("Invalid choice. Please enter a number in range [0, {1}]: ",
                    ipAddressList.Count);
            }

            IPAddress ipAddress = ipAddressList[choice];
            IPEndPoint endPoint = new IPEndPoint(ipAddress, ChatProtocol.ServerListeningPort);

            return endPoint;
        }

        public static void Broadcast(string message, string username = null, bool showName = false)
        {
            string sendData = showName
                ? username + ": " + message
                : message;

            TcpClient broadcastSocket = null;
            NetworkStream broadcastStream = null;

            foreach (DictionaryEntry item in ClientTable)
            {
                broadcastSocket = (TcpClient) item.Value;
                broadcastStream = broadcastSocket.GetStream();

                ChatProtocol.SendMessage(sendData, broadcastStream);
            }
        }

        private static void DisplayClientList()
        {
            Console.WriteLine("  Connected Clients: {0}", ClientTable.Count);
            foreach (DictionaryEntry item in ClientTable)
                Console.WriteLine("   |- {0} ({1})", ((TcpClient) item.Value).Client.RemoteEndPoint, item.Key);
        }

        public static void RemoveClient(string clientId)
        {
            ClientTable.Remove(clientId);
            Console.WriteLine("-------------------------------------------------------------------------------");
            Console.WriteLine("  Removed Client '{0}'", clientId);
            DisplayClientList();
            Console.WriteLine("-------------------------------------------------------------------------------");

            Broadcast("<client '" + clientId + "' disconnected>");
        }

        /// <summary>
        /// Start the server.
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args)
        {
            Console.WriteLine("Initializing server...");

            IPEndPoint localEndPoint = SelectLocalIPEndPoint(ChatProtocol.ServerListeningPort);
            TcpListener serverSocket = new TcpListener(localEndPoint);
            TcpClient clientSocket = null;

            serverSocket.Start();

            Console.WriteLine("-------------------------------------------------------------------------------");
            Console.WriteLine("Server started, using the following IPEndPoint:");
            Console.WriteLine("  - IP address  = {0}", localEndPoint.Address);
            Console.WriteLine("  - Port number = {0}", localEndPoint.Port);
            Console.WriteLine("-------------------------------------------------------------------------------");
            Console.WriteLine("Waiting for client...\n");

            while (true)
            {
                clientSocket = serverSocket.AcceptTcpClient();

                NetworkStream networkStream = clientSocket.GetStream();
                string clientId = ChatProtocol.ReadMessage(networkStream);

                if (ClientTable.Contains(clientId))
                {   // Duplicate IDs; don't allow connection.
                    Console.WriteLine("  Client [{1}] tried to connect using ID '{0}'. The request was rejected.", 
                        clientId, clientSocket.Client.RemoteEndPoint);
                    // TODO: reject client connection
                    continue;
                }

                ClientTable.Add(clientId, clientSocket);

                Console.WriteLine("-------------------------------------------------------------------------------");
                Console.WriteLine("  Added Client '{0}' [{1}]", clientId, clientSocket.Client.RemoteEndPoint);
                DisplayClientList();
                Console.WriteLine("-------------------------------------------------------------------------------");

                if (clientId == "exit") break; // TODO: proper exit condition

                Broadcast("<client '" + clientId + "' joined the chat>");

                ClientHandler clientHandler = new ClientHandler(clientId, clientSocket);
                clientHandler.StartThread();
            }

            clientSocket.Close();
            serverSocket.Stop();

            Console.WriteLine("Server has shut down. Press ENTER to exit.");
            Console.ReadLine();
        }
    }

}
