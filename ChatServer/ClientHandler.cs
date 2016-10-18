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
    class ClientHandler
    {
        private string clientId = null;
        private string displayName = null;
        private TcpClient clientSocket = null;

        private Thread thread = null;

        //--------------------------------------------------------------------------------------//

        public string ClientId
        {
            get { return clientId; }
            set { clientId = value; }
        }

        public string DisplayName
        {
            get { return displayName; }
            set { displayName = value; }
        }

        public TcpClient Socket
        {
            get { return clientSocket; }
            set { clientSocket = value; }
        }

        public IPEndPoint RemoteEndPoint => (IPEndPoint) clientSocket.Client.RemoteEndPoint;

        //--------------------------------------------------------------------------------------//

        public ClientHandler(string clientId, TcpClient clientSocket)
        {
            this.clientId = clientId;
            this.clientSocket = clientSocket;
        }

        /// <summary>
        /// Create a new thread and start listening to the client's message and broadcasting to others.
        /// </summary>
        public void StartThread()
        {
            thread = new Thread(DoChat);
            thread.Name = "ClientHandler for '" + clientId + "'";
            thread.Start();
        }

        private void DoChat()
        {
            while (true)
            {
                try
                {
                    // Read the message the client has sent.
                    NetworkStream networkStream = clientSocket.GetStream();
                    string readData = ChatProtocol.ReadMessage(networkStream);

                    if (readData != null)
                    {
                        Console.WriteLine(" > Client '" + clientId + "' sent: " + readData);

                        // Broadcast the message to other clients.
                        ServerProgram.Broadcast(readData, clientId, true);
                    }
                    else
                    {
                        throw new Exception("null data");
                    }
                }
                catch (Exception ex)
                {
                    //Console.WriteLine(ex.ToString());
                    ServerProgram.RemoveClient(this.clientId);
                    return;
                }
            }
        }
    }
}
