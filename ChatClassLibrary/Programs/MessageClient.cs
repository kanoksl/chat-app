using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

using ChatClassLibrary.Protocols;

namespace ChatClassLibrary
{
    public class ClientInfo
    {
        public Guid ClientId { get; set; }
        public string DisplayName { get; set; }

        public int ProfileImage 
            => this.ClientId.ToByteArray()[0];  // The first byte is the icon number.

        public string ProfileImagePath 
            => Path.GetFullPath(string.Format(".\\resource_images\\profile_{0:00}.png", this.ProfileImage));

        public ClientInfo(Guid id, string name)
        {
            this.ClientId = id;
            this.DisplayName = name;
        }

        public override string ToString()
        {
            return "ClientInfo\n"
                    + "    ClientID: " + this.ClientId.ToString()
                    + "    DisplayName: " + this.DisplayName;
        }
    }

    public class ChatroomInfo
    {
        public Guid ChatroomId { get; set; }
        public string ChatroomName { get; set; }
        public int MemberCount => Members?.Count ?? 0;
        public List<Guid> Members { get; set; }  // Only for joined rooms.
        public List<ClientInfo> MembersInfo { get; set; }

        public ChatroomInfo(Guid id, string name)
        {
            this.ChatroomId = id;
            this.ChatroomName = name;
            this.Members = new List<Guid>();
            this.MembersInfo = new List<ClientInfo>();
        }

        public override string ToString()
        {
            return "ChatroomInfo\n"
                    + "    ChatroomID: " + this.ChatroomId.ToString()
                    + "    ChatroomName: " + this.ChatroomName
                    + "    MemberCount: " + this.MemberCount;
        }
    }

    public class MessageClient
    {
        public MessageClient(IPAddress serverIP, int port, int profileImage = 0)
        {
            var serverEP = new IPEndPoint(serverIP, port);
            this.ServerEndPoint = serverEP;

            byte[] clientGuid = Guid.NewGuid().ToByteArray();
            clientGuid[0] = (byte) profileImage;  // The first byte is the profile image number.
            this.ClientId = new Guid(clientGuid);

            this.KnownClients = new Dictionary<Guid, ClientInfo>();
            this.KnownChatrooms = new Dictionary<Guid, ChatroomInfo>();
            this.KnownChatrooms.Add(ProtocolSettings.PublicRoomId, 
                                    new ChatroomInfo(ProtocolSettings.PublicRoomId, "Public Room"));
            this.JoinedChatrooms = new List<Guid>();
        }

        public MessageClient(IPAddress serverIP, int profileImage = 0)
            : this(serverIP, ProtocolSettings.ChatProtocolPort, profileImage) { }

        //--------------------------------------------------------------------------------------//

        #region Public Properties

        public IPEndPoint ServerEndPoint { get; set; }
        public TcpClient ClientSocket { get; set; }
        public NetworkStream NetworkStream { get; set; }
        
        public Guid ClientId { get; private set; }
        public string DisplayName { get; set; }
        
        public Dictionary<Guid, ClientInfo> KnownClients { get; set; }
        public Dictionary<Guid, ChatroomInfo> KnownChatrooms { get; set; }
        public List<Guid> JoinedChatrooms { get; set; }
        
        public bool Connected
            => this.ClientSocket?.Connected ?? false;

        public int ProfileImage
            => this.ClientId.ToByteArray()[0];  // The first byte is the icon number.
        public string ProfileImagePath
            => Path.GetFullPath(string.Format(".\\resource_images\\profile_{0:00}.png", this.ProfileImage));

        #endregion

        //--------------------------------------------------------------------------------------//

        public string GetChatroomName(Guid chatroomId)
        {
            if (KnownChatrooms.ContainsKey(chatroomId))
                return KnownChatrooms[chatroomId].ChatroomName;
            else
                return "<UNKNOWN ROOM>";
        }

        public string GetClientName(Guid clientId)
        {
            if (KnownClients.ContainsKey(clientId))
                return KnownClients[clientId].DisplayName;
            else
                return "<UNKNOWN CLIENT>";
        }
        
        public async void ConnectToServer()
        {
            await Task.Run(() =>
            {
                try
                {   // Connect to the server's socket.
                    this.ClientSocket = new TcpClient();
                    this.ClientSocket.Connect(this.ServerEndPoint);
                    this.NetworkStream = this.ClientSocket.GetStream();

                    Message request = new Message
                    {
                        Type = MessageType.Control,
                        ControlInfo = ControlInfo.ClientRequestConnection,
                        SenderId = this.ClientId,
                        TargetId = ProtocolSettings.NullId,
                        Text = this.DisplayName
                    };
                    ChatProtocol.SendMessage(request, this.NetworkStream);

                    Message reply = ChatProtocol.ReceiveMessage(this.NetworkStream);
                    if (reply.ControlInfo == ControlInfo.ConnectionAccepted)
                    {
                        this.OnConnectionEstablished(
                            new ConnectionEventArgs(this.ServerEndPoint, this.ClientSocket));
                    }
                    else
                    {
                        Console.WriteLine("Server rejected connection: " + reply.Text);
                        this.OnConnectionFailed(
                            new ConnectionEventArgs(this.ServerEndPoint, this.ClientSocket));
                    }


                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.StackTrace);  // TODO: better handling
                    this.OnConnectionFailed(new ConnectionEventArgs(this.ServerEndPoint,
                                                                    this.ClientSocket));
                }
            });
        }

        public void SendMessage(string messageText, Guid targetId, bool privateMessage = false)
        {
            Message message = new Message
            {
                Type = privateMessage ? MessageType.UserPrivateMessage
                                      : MessageType.UserGroupMessage,
                ControlInfo = ControlInfo.None,
                SenderId = this.ClientId,
                TargetId = targetId,
                Text = messageText
            };

            try
            {
                ChatProtocol.SendMessage(message, this.NetworkStream);
                this.OnMessageSent(new MessageEventArgs(message));
            }
            catch
            {
                this.OnMessageSendingFailed(new MessageEventArgs(message));
            }
        }

        private CancellationTokenSource _receiveCTS;

        public async void BeginReceive()
        {
            _receiveCTS = new CancellationTokenSource();
            var token = _receiveCTS.Token;
            try
            {
                await Task.Run(() =>
                {   // May cause 'exception unhandled by user code' when debugging.
                    while (this.Connected)
                    {
                        token.ThrowIfCancellationRequested();
                        Message message = ChatProtocol.ReceiveMessage(this.NetworkStream);
                        if (!message.IsValid)  // Null message, mostly occur when connection is terminated.
                            continue;
                        else if (message.ControlInfo == ControlInfo.ListOfClients)
                            UpdateKnownClientList(message.SenderId, message.Text);
                        else if (message.ControlInfo == ControlInfo.ListOfChatrooms)
                            UpdateKnownChatroomList(message.Text);
                        else if (message.ControlInfo == ControlInfo.ClientJoinedChatroom && message.SenderId == this.ClientId)
                            _JoinedChatRoom(message.TargetId);
                        else if (message.ControlInfo == ControlInfo.ClientLeftChatroom && message.SenderId == this.ClientId)
                            _LeftChatroom(message.TargetId);
                        else if (message.Type == MessageType.UserPrivateMessage)
                            this.OnPrivateMessageReceived(new MessageEventArgs(message));
                        else if (message.ControlInfo == ControlInfo.FileAvailable)
                            this.OnFileAvailable(new MessageEventArgs(message));
                        else if (message.ControlInfo == ControlInfo.ListOfFiles)
                            this.OnFileListReceived(new MessageEventArgs(message));
                        else
                            this.OnMessageReceived(new MessageEventArgs(message));
                    }
                });
            }
            catch (OperationCanceledException)
            {
                this.OnMessageReceived(new MessageEventArgs(new Message { Text = "CLIENT DISCONNECTED" }));
            }
            catch
            {
                this.OnMessageReceivingingFailed(new MessageEventArgs(new Message { Text = "RECV FAILED" }));
            }

            this.OnServerDisconnected(new ConnectionEventArgs(this.ServerEndPoint, this.ClientSocket));
        }

        private void _JoinedChatRoom(Guid roomId)
        {
            if (!JoinedChatrooms.Contains(roomId))
            {
                JoinedChatrooms.Add(roomId);
                this.OnClientJoinedChatroom(new ChatroomEventArgs(roomId,
                                    GetChatroomName(roomId)));
            }
            this.OnKnownChatroomsUpdated(EventArgs.Empty);
        }

        private void _LeftChatroom(Guid roomId)
        {
            if (JoinedChatrooms.Contains(roomId))
            {
                JoinedChatrooms.Remove(roomId);
                this.OnClientLeftChatroom(new ChatroomEventArgs(roomId,
                                GetChatroomName(roomId)));
            }
            this.OnKnownChatroomsUpdated(EventArgs.Empty);
        }

        public void RequestJoinChatroom(Guid roomId)
        {
            if (JoinedChatrooms.Contains(roomId))
                return;  // Already joined.

            Message request = new Message
            {
                Type = MessageType.Control,
                ControlInfo = ControlInfo.RequestJoinChatroom,
                SenderId = this.ClientId,
                TargetId = roomId
            };
            ChatProtocol.SendMessage(request, this.NetworkStream);
        }

        public void RequestLeaveChatroom(Guid roomId)
        {
            if (!JoinedChatrooms.Contains(roomId))
                return;  // Not a member.

            Message request = new Message
            {
                Type = MessageType.Control,
                ControlInfo = ControlInfo.RequestLeaveChatroom,
                SenderId = this.ClientId,
                TargetId = roomId
            };
            ChatProtocol.SendMessage(request, this.NetworkStream);
        }

        public void RequestCreateChatroom(string roomName)
        {
            Message request = new Message
            {
                Type = MessageType.Control,
                ControlInfo = ControlInfo.RequestCreateChatroom,
                SenderId = this.ClientId,
                TargetId = ProtocolSettings.NullId,
                Text = roomName
            };
            ChatProtocol.SendMessage(request, this.NetworkStream);
        }

        private void UpdateKnownClientList(Guid roomId, string clientList)
        {
            if (roomId == Guid.Empty)
                this.KnownClients.Clear();

            ChatroomInfo roomInfo = null;
            if (this.KnownChatrooms.ContainsKey(roomId))
            {
                roomInfo = KnownChatrooms[roomId];
                roomInfo.Members.Clear();
                roomInfo.MembersInfo.Clear();
            }

            if (clientList != null)
                using (var reader = new StringReader(clientList))
                {
                    string guidStr;
                    while ((guidStr = reader.ReadLine()) != null)
                    {
                        string name = reader.ReadLine();

                        var clientId = Guid.Parse(guidStr);
                        var newClient = new ClientInfo(clientId, name);

                        if (roomId == Guid.Empty || !KnownClients.ContainsKey(clientId))
                            this.KnownClients.Add(clientId, newClient);
                        roomInfo?.Members.Add(clientId);
                        roomInfo?.MembersInfo.Add(newClient);
                    }
                }
            this.OnKnownClientsUpdated(EventArgs.Empty);
        }

        private void UpdateKnownChatroomList(string chatroomList)
        {
            //this.KnownChatrooms.Clear();
            var temp = new HashSet<Guid>();
            using (var reader = new StringReader(chatroomList))
            {
                string guidStr;
                while ((guidStr = reader.ReadLine()) != null)
                {
                    string name = reader.ReadLine();
                    string countStr = reader.ReadLine();

                    var roomId = Guid.Parse(guidStr);
                    var count = int.Parse(countStr);
                    var newRoom = new ChatroomInfo(roomId, name);

                    temp.Add(roomId);
                    if (!KnownChatrooms.ContainsKey(roomId))
                        this.KnownChatrooms.Add(roomId, newRoom);
                    else if (count != KnownChatrooms[roomId].MemberCount)
                        Console.WriteLine("AAAAAAAAAAAAAAAAAAAAAAAAARRRRRRRRRRRRRRRRGGGGGGGGGHHHHHH");
                }
            }

            List<Guid> ids = new List<Guid>();
            foreach (var roomId in KnownChatrooms.Keys)
                ids.Add(roomId);
            foreach (var roomId in ids)
                if (!temp.Contains(roomId)) KnownChatrooms.Remove(roomId);

            this.OnKnownChatroomsUpdated(EventArgs.Empty);
        }

        public void Disconnect()
        {
            if (this.Connected)
            {
                this.ClientSocket.Close();
                this.ClientSocket = null;
                this.NetworkStream = null;

                this._receiveCTS?.Cancel();

                this.KnownChatrooms.Clear();
                this.KnownClients.Clear();
                this.JoinedChatrooms.Clear();

                this.OnClientDisconnected(new ConnectionEventArgs(this.ServerEndPoint, this.ClientSocket));
            }
        }

        //--------------------------------------------------------------------------------------//
        #region Event Handlers and Events

        public event EventHandler<MessageEventArgs> MessageSent;
        public event EventHandler<MessageEventArgs> MessageReceived;
        public event EventHandler<MessageEventArgs> MessageSendingFailed;
        public event EventHandler<MessageEventArgs> MessageReceivingingFailed;
        public event EventHandler<MessageEventArgs> PrivateMessageReceived;

        public event EventHandler<MessageEventArgs> FileAvailable;
        public event EventHandler<MessageEventArgs> FileListReceived;

        public event EventHandler KnownClientsUpdated;
        public event EventHandler KnownChatroomsUpdated;

        public event EventHandler<ChatroomEventArgs> ClientJoinedChatroom;
        public event EventHandler<ChatroomEventArgs> ClientLeftChatroom;

        public event EventHandler<ConnectionEventArgs> ConnectionEstablished;
        public event EventHandler<ConnectionEventArgs> ConnectionFailed;
        public event EventHandler<ConnectionEventArgs> ClientDisconnected;
        public event EventHandler<ConnectionEventArgs> ServerDisconnected;

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

        protected virtual void OnFileAvailable(MessageEventArgs e)
            => FileAvailable?.Invoke(this, e);
        protected virtual void OnFileListReceived(MessageEventArgs e)
            => FileListReceived?.Invoke(this, e);

        protected virtual void OnKnownClientsUpdated(EventArgs e)
            => KnownClientsUpdated?.Invoke(this, e);
        protected virtual void OnKnownChatroomsUpdated(EventArgs e)
            => KnownChatroomsUpdated?.Invoke(this, e);

        protected virtual void OnClientJoinedChatroom(ChatroomEventArgs e)
            => ClientJoinedChatroom?.Invoke(this, e);
        protected virtual void OnClientLeftChatroom(ChatroomEventArgs e)
            => ClientLeftChatroom?.Invoke(this, e);

        protected virtual void OnConnectionEstablished(ConnectionEventArgs e)
            => ConnectionEstablished?.Invoke(this, e);
        protected virtual void OnConnectionFailed(ConnectionEventArgs e)
            => ConnectionFailed?.Invoke(this, e);
        protected virtual void OnClientDisconnected(ConnectionEventArgs e)
            => ClientDisconnected?.Invoke(this, e);
        protected virtual void OnServerDisconnected(ConnectionEventArgs e)
            => ServerDisconnected?.Invoke(this, e);

        #endregion
        //--------------------------------------------------------------------------------------//

        // more events:
        // client list received
        // chatroom list received // currently included in MessageReceived



        // more methods:
        // join chatroom
        // leave chatroom
    }

}
