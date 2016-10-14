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


        public static IPEndPoint GetLocalIPEndPoint(int port)
        {
            //string localHostName = Dns.GetHostName();
            //IPHostEntry ipHostInfo = Dns.GetHostEntry(localHostName);

            //AddressFamily IPv4 = AddressFamily.InterNetwork;
            //IPAddress[] ipAddressList = ipHostInfo.AddressList;
            //IPAddress ipAddress = ipAddressList.Where(ip => ip.AddressFamily == IPv4).First();
            IPAddress ipAddress = IPAddress.Parse("127.0.0.1");

            IPEndPoint endPoint = new IPEndPoint(ipAddress, port);

            return endPoint;
        }

        public static void Broadcast(string message, string username = null, bool showName = false)
        {
            string sendData = showName ? username + ": " + message : message;

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
            {
                Console.WriteLine("   |  - {0} ({1})", ((TcpClient) item.Value).Client.RemoteEndPoint, item.Key);
            }
        }

        public static void RemoveClient(string clientId)
        {
            ClientTable.Remove(clientId);
            Console.WriteLine("--------------------------------------------------------------------------------");
            Console.WriteLine("  Removed Client '{0}'", clientId);
            DisplayClientList();
            Console.WriteLine("--------------------------------------------------------------------------------");

            Broadcast("<client '" + clientId + "' disconnected>");
        }

        /// <summary>
        /// Start the server.
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args)
        {
            IPEndPoint localEndPoint = GetLocalIPEndPoint(ChatProtocol.ServerListeningPort);
            TcpListener serverSocket = new TcpListener(localEndPoint);
            TcpClient clientSocket = null;
            int clientCount = 0;

            serverSocket.Start();

            Console.WriteLine("--------------------------------------------------------------------------------");
            Console.WriteLine("  Server started, using the following IPEndPoint:");
            Console.WriteLine("    - IP address  = {0}", localEndPoint.Address);
            Console.WriteLine("    - Port number = {0}", localEndPoint.Port);
            Console.WriteLine("--------------------------------------------------------------------------------");
            Console.WriteLine("  Waiting for client...\n");

            while (true)
            {
                clientCount += 1;
                clientSocket = serverSocket.AcceptTcpClient();
                
                NetworkStream networkStream = clientSocket.GetStream();
                string username = ChatProtocol.ReadMessage(networkStream);

                ClientTable.Add(username, clientSocket);

                Console.WriteLine("--------------------------------------------------------------------------------");
                Console.WriteLine("  Added Client '{0}' [{1}]", username, clientSocket.Client.RemoteEndPoint);
                DisplayClientList();
                Console.WriteLine("--------------------------------------------------------------------------------");

                if (username == "exit") break; // TODO: proper exit condition

                Broadcast("<client '" + username + "' joined the chat>");

                ClientHandler clientHandler = new ClientHandler(username, clientSocket);
                clientHandler.StartThread();
            }

            clientSocket.Close();
            serverSocket.Stop();

            Console.WriteLine("Server shut down. Press ENTER to exit.");
            Console.ReadLine();
        }
    }
    
}
