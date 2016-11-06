using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using ChatClassLibrary.Protocols;

namespace ChatClassLibrary
{
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

                    if (!message.IsValid)
                        this.Disconnect();
                    else if (message.ControlInfo == ControlInfo.RequestJoinChatroom)
                        this.OnClientRequestJoinChatroom(new ChatroomEventArgs(message.TargetId, null));
                    else if (message.ControlInfo == ControlInfo.RequestLeaveChatroom)
                        this.OnClientRequestLeaveChatroom(new ChatroomEventArgs(message.TargetId, null));
                    else if (message.ControlInfo == ControlInfo.RequestFileRemove)
                    {
                        this.OnFileRemoveRequestReceived(new MessageEventArgs(message));
                        continue;
                    }
                    else if (message.ControlInfo == ControlInfo.RequestFileDownload)
                    {
                        this.OnFileDownloadRequestReceived(new MessageEventArgs(message));
                        continue;
                    }
                    else if (message.ControlInfo == ControlInfo.RequestCreateChatroom)
                    {
                        this.OnClientRequestCreateChatroom(new MessageEventArgs(message));
                        continue;
                    }

                    if (message.Type == MessageType.UserPrivateMessage)
                        this.OnPrivateMessageReceived(new MessageEventArgs(message));
                    else
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
        public event EventHandler<MessageEventArgs> PrivateMessageReceived;

        public event EventHandler<MessageEventArgs> FileDownloadRequestReceived;
        public event EventHandler<MessageEventArgs> FileRemoveRequestReceived;

        public event EventHandler<ChatroomEventArgs> ClientRequestJoinChatroom;
        public event EventHandler<ChatroomEventArgs> ClientRequestLeaveChatroom;
        public event EventHandler<MessageEventArgs> ClientRequestCreateChatroom;

        public event EventHandler<ConnectionEventArgs> ClientDisconnected;

        protected virtual void OnMessageSent(MessageEventArgs e)
                => MessageSent?.Invoke(this, e);
        protected virtual void OnMessageReceived(MessageEventArgs e)
                => MessageReceived?.Invoke(this, e);
        protected virtual void OnMessageSendingFailed(MessageEventArgs e)
                => MessageSendingFailed?.Invoke(this, e);
        protected virtual void OnMessageReceivingingFailed(MessageEventArgs e)
                => MessageReceivingingFailed?.Invoke(this, e);
        protected virtual void OnPrivateMessageReceived(MessageEventArgs e)
                => PrivateMessageReceived?.Invoke(this, e);

        protected virtual void OnFileDownloadRequestReceived(MessageEventArgs e)
                => FileDownloadRequestReceived?.Invoke(this, e);
        protected virtual void OnFileRemoveRequestReceived(MessageEventArgs e)
                => FileRemoveRequestReceived?.Invoke(this, e);

        protected virtual void OnClientRequestJoinChatroom(ChatroomEventArgs e)
                => ClientRequestJoinChatroom?.Invoke(this, e);
        protected virtual void OnClientRequestLeaveChatroom(ChatroomEventArgs e)
                => ClientRequestLeaveChatroom?.Invoke(this, e);
        protected virtual void OnClientRequestCreateChatroom(MessageEventArgs e)
                => ClientRequestCreateChatroom?.Invoke(this, e);

        protected virtual void OnClientDisconnected(ConnectionEventArgs e)
                => ClientDisconnected?.Invoke(this, e);

        #endregion
        //--------------------------------------------------------------------------------------//

    }
}