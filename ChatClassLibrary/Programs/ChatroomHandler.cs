using System;
using System.Collections.Generic;
using System.Text;
using ChatClassLibrary.Protocols;

namespace ChatClassLibrary
{
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

            this.OnClientJoinedChatroom(new ChatroomEventArgs(this.ChatroomId, this.ChatroomName));
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
            SendFullClientList(client);

            if (this.ClientHandlerTable.Count == 0)
                this.OnChatroomBecameEmpty(new ChatroomEventArgs(this.ChatroomId, this.ChatroomName));

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
                ControlInfo = ControlInfo.ListOfClients,
                SenderId = this.ChatroomId,
                TargetId = ProtocolSettings.NullId,  // No need to be specific?
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

        public event EventHandler<ChatroomEventArgs> ClientJoinedChatroom;
        public event EventHandler<ChatroomEventArgs> ClientLeftChatroom;
        public event EventHandler<ChatroomEventArgs> ChatroomBecameEmpty;

        protected virtual void OnClientJoinedChatroom(ChatroomEventArgs e)
                => ClientJoinedChatroom?.Invoke(this, e);
        protected virtual void OnClientLeftChatroom(ChatroomEventArgs e)
                => ClientLeftChatroom?.Invoke(this, e);
        protected virtual void OnChatroomBecameEmpty(ChatroomEventArgs e)
                => ChatroomBecameEmpty?.Invoke(this, e);

    }
}