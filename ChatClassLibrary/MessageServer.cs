using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ChatClassLibrary
{
    public class MessageServer
    {
        public IPAddress ServerIP { get; private set; }
        public int ServerListeningPort { get; private set; }

        private TcpListener ServerSocket { get; set; }


        /// <summary>
        /// List of all connected clients.
        /// </summary>
        private Dictionary<Guid, ClientHandler> ClientHandlerTable { get; set; }

        /// <summary>
        /// List of all currently available chatrooms.
        /// </summary>
        private Dictionary<Guid, ChatroomHandler> ChatroomHandlerTable { get; set; }

        private ChatroomHandler publicRoom;
        private ChatroomHandler publicRoom2;

        /// <summary>
        /// Initialize a server that use the given IP and listen to the specified port.
        /// </summary>
        /// <param name="serverIP">IP address of the local machine to use.</param>
        /// <param name="port">Listening port.</param>
        public MessageServer(IPAddress serverIP, int port)
        {
            this.ServerIP = serverIP;
            this.ServerListeningPort = port;
            this.ServerSocket = new TcpListener(serverIP, port);
            //this.ClientHandlerTable = new Dictionary<Guid, ClientHandler>();
            this.ChatroomHandlerTable = new Dictionary<Guid, ChatroomHandler>();

            this.publicRoom = new ChatroomHandler(Guid.Empty) { ChatroomName = "Public Room" };
            this.publicRoom2 = new ChatroomHandler(Guid.NewGuid()) { ChatroomName = "Another Public Room" };
            this.ChatroomHandlerTable.Add(Guid.Empty, publicRoom);
            this.ChatroomHandlerTable.Add(publicRoom2.ChatroomId, publicRoom2);
            this.ClientHandlerTable = publicRoom.ClientHandlerTable;
        }

        /// <summary>
        /// Initialize a server that use the given IP and listen to the default port.
        /// </summary>
        /// <param name="serverIP">IP address of the local machine to use.</param>
        public MessageServer(IPAddress serverIP)
            : this(serverIP, ChatProtocol.ServerListeningPort) { }


        public void StartListening()
        {
            TcpClient clientSocket = null;
            Message reply = new Message
            {
                Type = MessageType.Control,
                SenderId = Message.NullID,
            };

            this.ServerSocket.Start();

            Console.WriteLine("-------------------------------------------------------------------------------");
            Console.WriteLine("Server started, using the following IPEndPoint:");
            Console.WriteLine("  - IP address  = {0}", this.ServerIP);
            Console.WriteLine("  - Port number = {0}", this.ServerListeningPort);
            Console.WriteLine("-------------------------------------------------------------------------------");
            Console.WriteLine("Waiting for client...\n");

            while (true)
            {   // Wait for client connection.
                clientSocket = ServerSocket.AcceptTcpClient();

                NetworkStream networkStream = clientSocket.GetStream();
                Message request = ChatProtocol.ReceiveMessage(networkStream);
                Guid clientId = request.SenderId;

                Console.WriteLine("Client requested connection:");
                Console.WriteLine(request.ToString());

                if (request.ControlInfo != ControlInfo.ClientRequestConnection)
                {   // The first message must be a request.
                    clientSocket.Close();
                    continue;
                }

                if (this.ClientHandlerTable.ContainsKey(request.SenderId))
                {   // Duplicate IDs; don't allow connection.
                    Console.WriteLine("  Client [{0}] tried to connect using duplicated ID: {1}.",
                            clientSocket.Client.RemoteEndPoint, clientId.ToString());

                    // Reject client connection.
                    reply.ControlInfo = ControlInfo.ConnectionRejected;
                    reply.TargetId = clientId;
                    reply.Text = "There is already a client with the same ID.";
                    ChatProtocol.SendMessage(reply, networkStream);

                    clientSocket.Close();
                    continue;
                }

                // Accept client connection.
                reply.ControlInfo = ControlInfo.ConnectionAccepted;
                reply.TargetId = clientId;
                ChatProtocol.SendMessage(reply, networkStream);

                // Add to client list.
                ClientHandler handler = new ClientHandler(clientId, clientSocket);
                handler.DisplayName = request.Text;
                //this.ClientHandlerTable.Add(clientId, handler);
                handler.BeginReceive();

                handler.ClientDisconnected += (sender, e) => DisplayClientList();  // FIXME

                this.publicRoom.AddClient(handler);

                this.publicRoom2.AddClient(handler);

                // TODO: send chatroom list updates
                SendFullChatroomList(this.ClientHandlerTable.Values);
            }
        }

        private void CreateChatroom()
        {

        }

        private bool RemoveChatroom()
        {
            return false;
        }

        private void SendFullChatroomList(ICollection<ClientHandler> targets)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var entry in this.ChatroomHandlerTable)
            {
                sb.Append(entry.Key.ToString()).AppendLine();
                sb.Append(entry.Value.ChatroomName).AppendLine();
                sb.Append(entry.Value.ClientCount).AppendLine();
            }

            Message roomList = new Message
            {
                Type = MessageType.Control,
                ControlInfo = ControlInfo.ChatroomList,
                SenderId = Message.NullID,
                TargetId = Message.NullID,  // No need to be specific?
                Text = sb.ToString()
            };

            foreach (var targetClient in targets)
                targetClient.SendMessage(roomList);
        }

        private void SendFullChatroomList(ClientHandler target)
        {
            SendFullChatroomList(new ClientHandler[] { target });
        }

        private void DisplayClientList()
        {
            Console.WriteLine("  Connected Clients: {0}", this.ClientHandlerTable.Count);
            foreach (var entry in this.ClientHandlerTable)
            {
                Console.WriteLine("   |- {0} (id={1} | name={2})", entry.Value.RemoteEndPoint,
                    entry.Key, entry.Value.DisplayName);
            }
        }

    }

    public class ChatroomHandler
    {
        /// <summary>
        /// Unique identifier of this chatroom.
        /// </summary>
        public Guid ChatroomId { get; private set; }

        public string ChatroomName { get; set; }

        /// <summary>
        /// List of all clients in this chatroom.
        /// </summary>
        public Dictionary<Guid, ClientHandler> ClientHandlerTable { get; private set; }

        /// <summary>
        /// Number of clients in this chatroom.
        /// </summary>
        public int ClientCount
            => this.ClientHandlerTable.Count;

        //--------------------------------------------------------------------------------------//

        /// <summary>
        /// Create a new empty chatroom.
        /// </summary>
        /// <param name="chatroomId"></param>
        public ChatroomHandler(Guid chatroomId)
        {
            this.ChatroomId = chatroomId;
            this.ClientHandlerTable = new Dictionary<Guid, ClientHandler>();
        }

        /// <summary>
        /// Create a new empty chatroom. (The ID is randomly generated).
        /// </summary>
        public ChatroomHandler()
            : this(Guid.NewGuid()) { }

        //--------------------------------------------------------------------------------------//

        /// <summary>
        /// Add a connected client to this chatroom.
        /// </summary>
        /// <param name="client">Handler for the client.</param>
        /// <returns>True if the client is successfully added.</returns>
        public bool AddClient(ClientHandler client)
        {
            if (this.ClientHandlerTable.ContainsKey(client.ClientId))
            {   // Already in the chatroom.
                return false;
            }
            this.ClientHandlerTable.Add(client.ClientId, client);

            // Tell other members of the chatroom.
            Message notification = new Message
            {
                Type = MessageType.Control,
                ControlInfo = ControlInfo.ClientJoinedChatroom,
                SenderId = client.ClientId,
                TargetId = this.ChatroomId,
                Text = "Client '" + client.DisplayName + "' has joined the chat."
            };
            BroadcastMessage(notification, except: client.ClientId);

            // Tell the client itself.
            notification.Text = "You are now a member of this chatroom.";
            client.SendMessage(notification);

            // Broadcast the new list of clients
            // TODO: more efficient way, sending only diff
            SendFullClientList(this.ClientHandlerTable.Values);

            client.MessageReceived += Client_MessageReceived;
            client.ClientDisconnected += Client_ClientDisconnected;

            return true;
        }

        private void Client_ClientDisconnected(object sender, ConnectionEventArgs e)
        {
            RemoveClient(((ClientHandler) sender).ClientId);
        }

        private void Client_MessageReceived(object sender, MessageEventArgs e)
        {
            if (e.Message.TargetId == this.ChatroomId)
                BroadcastMessage(e.Message);
        }

        /// <summary>
        /// Remove the client with the given ID from the chatroom.
        /// </summary>
        /// <param name="clientId">GUID of the client to remove.</param>
        /// <returns>True if the client is successfully removed.</returns>
        public bool RemoveClient(Guid clientId)
        {
            if (!this.ClientHandlerTable.ContainsKey(clientId))
            {   // No such client.
                return false;
            }
            ClientHandler client = this.ClientHandlerTable[clientId];
            client.MessageReceived -= Client_MessageReceived;
            this.ClientHandlerTable.Remove(clientId);

            // Tell other members of the chatroom.
            Message notification = new Message
            {
                Type = MessageType.Control,
                ControlInfo = ControlInfo.ClientLeftChatroom,
                SenderId = client.ClientId,
                TargetId = this.ChatroomId,
                Text = "Client '" + client.DisplayName + "' has left the chat."
            };
            BroadcastMessage(notification, except: client.ClientId);

            // Tell the client itself.
            notification.Text = "You have left this chatroom.";
            client.SendMessage(notification);


            // Broadcast the new list of clients
            // TODO: more efficient way, sending only diff
            SendFullClientList(this.ClientHandlerTable.Values);

            return true;
        }

        /// <summary>
        /// Send a special message containing list of all client IDs and names
        /// in this chatroom to the specified clients.
        /// </summary>
        /// <param name="targets">Clients to send the list to.</param>
        public void SendFullClientList(ICollection<ClientHandler> targets)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var entry in this.ClientHandlerTable)
            {
                sb.Append(entry.Key.ToString()).AppendLine();
                sb.Append(entry.Value.DisplayName).AppendLine();
            }

            Message clientList = new Message
            {
                Type = MessageType.Control,
                ControlInfo = ControlInfo.ClientList,
                SenderId = this.ChatroomId,
                TargetId = Message.NullID,  // No need to be specific?
                Text = sb.ToString()
            };

            foreach (var targetClient in targets)
                targetClient.SendMessage(clientList);
        }

        /// <summary>
        /// Send a special message containing list of all client IDs and names
        /// in this chatroom to the specified client.
        /// </summary>
        /// <param name="target">Client to send the list to.</param>
        public void SendFullClientList(ClientHandler target)
        {
            SendFullClientList(new ClientHandler[] { target });
        }

        /// <summary>
        /// Send a message to all clients in this chatroom.
        /// </summary>
        /// <param name="message">A message to be sent.</param>
        public void BroadcastMessage(Message message)
        {
            BroadcastMessage(message, Guid.Empty);
        }

        /// <summary>
        /// Send a message to all clients in this chatroom (except one, if specified).
        /// </summary>
        /// <param name="message">A message to be sent.</param>
        /// <param name="except">ID of the client not to send the message to.</param>
        public void BroadcastMessage(Message message, Guid except)
        {
            foreach (var entry in this.ClientHandlerTable)
            {   // Key is the GUID, value is the handler.
                if (entry.Key != except)
                    entry.Value.SendMessage(message);
            }
        }

    }

    public class ClientHandler
    {
        public Guid ClientId { get; private set; }
        public string DisplayName { get; set; }

        private TcpClient ClientSocket { get; set; }
        private NetworkStream NetworkStream { get; set; }

        private Thread Thread { get; set; }

        public bool Connected
            => this.ClientSocket?.Connected ?? false;
        public IPEndPoint RemoteEndPoint
            => (IPEndPoint) this.ClientSocket?.Client.RemoteEndPoint;

        /// <summary>
        /// Create a new client handler for the given socket.
        /// </summary>
        /// <param name="clientId">GUID of the client.</param>
        /// <param name="clientSocket">Socket that communicates with the client.</param>
        public ClientHandler(Guid clientId, TcpClient clientSocket)
        {
            this.ClientId = clientId;
            this.ClientSocket = clientSocket;
            this.NetworkStream = clientSocket.GetStream();
        }

        /// <summary>
        /// Send a message to the handled client.
        /// </summary>
        /// <param name="message">A message to be sent.</param>
        public void SendMessage(Message message)
        {
            try
            {   // Convert message to bytes and send on a stream.
                ChatProtocol.SendMessage(message, this.NetworkStream);
                this.OnMessageSent(new MessageEventArgs(message));
            }
            catch
            {
                this.OnMessageSendingFailed(new MessageEventArgs(message));
            }
        }

        /// <summary>
        /// Start receiving messages from the handled client.
        /// </summary>
        public void BeginReceive()
        {
            // Create a new thread that do the actual message receiving.
            this.Thread = new Thread(ThreadBeginReceive);
            this.Thread.Name = "ClientHandler [" + this.ClientId.ToString() + "]";
            this.Thread.Start();
        }

        /// <summary>
        /// Close the socket.
        /// </summary>
        public void Disconnect()
        {
            if (!this.Connected)
            {   // Already disconnected.
                return;
            }

            this.ClientSocket.Close();
            this.ClientSocket = null;
            this.NetworkStream = null;

            this.OnClientDisconnected(new ConnectionEventArgs());
        }

        private void ThreadBeginReceive()
        {
            while (this.Connected)
            {
                try
                {   // (Blocking) read from the socket.
                    Message message = ChatProtocol.ReceiveMessage(this.NetworkStream);
                    this.OnMessageReceived(new MessageEventArgs(message));
                }
                catch
                {
                    this.OnMessageReceivingingFailed(new MessageEventArgs());
                    Disconnect();
                }
            }
            this.OnClientDisconnected(new ConnectionEventArgs());
        }

        //--------------------------------------------------------------------------------------//
        #region Event Handlers and Events

        public event EventHandler<MessageEventArgs> MessageSent;
        public event EventHandler<MessageEventArgs> MessageReceived;
        public event EventHandler<MessageEventArgs> MessageSendingFailed;
        public event EventHandler<MessageEventArgs> MessageReceivingingFailed;

        public event EventHandler<ConnectionEventArgs> ClientDisconnected;

        protected virtual void OnMessageSent(MessageEventArgs e)
            => MessageSent?.Invoke(this, e);
        protected virtual void OnMessageReceived(MessageEventArgs e)
            => MessageReceived?.Invoke(this, e);
        protected virtual void OnMessageSendingFailed(MessageEventArgs e)
            => MessageSendingFailed?.Invoke(this, e);
        protected virtual void OnMessageReceivingingFailed(MessageEventArgs e)
            => MessageReceivingingFailed?.Invoke(this, e);

        protected virtual void OnClientDisconnected(ConnectionEventArgs e)
            => ClientDisconnected?.Invoke(this, e);

        #endregion
        //--------------------------------------------------------------------------------------//

    }
}
