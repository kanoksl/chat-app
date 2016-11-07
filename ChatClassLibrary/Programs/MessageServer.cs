using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using ChatClassLibrary.Protocols;

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

            this.publicRoom.ClientJoinedChatroom += Chatroom_ClientJoinedChatroom;
            this.publicRoom2.ClientJoinedChatroom += Chatroom_ClientJoinedChatroom;
            this.publicRoom.ChatroomBecameEmpty += Chatroom_ChatroomBecameEmpty;
            this.publicRoom2.ChatroomBecameEmpty += Chatroom_ChatroomBecameEmpty;

            this.ClientHandlerTable = new Dictionary<Guid, ClientHandler>();
        }

        private void Chatroom_ChatroomBecameEmpty(object sender, ChatroomEventArgs e)
        {
            if (e.ChatroomId != Guid.Empty)
                RemoveChatroom(e.ChatroomId);
        }

        private void Chatroom_ClientJoinedChatroom(object sender, ChatroomEventArgs e)
        {
            SendFileList(e.ChatroomId);
        }

        /// <summary>
        /// Initialize a server that use the given IP and listen to the default port.
        /// </summary>
        /// <param name="serverIP">IP address of the local machine to use.</param>
        public MessageServer(IPAddress serverIP)
            : this(serverIP, ProtocolSettings.ChatProtocolPort) { }

        private void StartFTPListener(int port)
        {
            TcpListener ftpSocket = new TcpListener(this.ServerIP, port);
            Thread ftpThread = new Thread(() =>
            {
                lock (this._lock)
                {
                    Console.WriteLine("FTP Listener Thread started, using the following IPEndPoint:");
                    Console.WriteLine("  - IP address  = {0}", this.ServerIP);
                    Console.WriteLine("  - Port number = {0}", port);
                    Console.WriteLine("Waiting for file upload requests...");
                    Console.WriteLine("-------------------------------------------------------------------------------");
                }

                ftpSocket.Start();

//                while (true)
//                {
                    Guid senderId;
                    Guid targetId;
                    string fileInfo;  // Name, size, time, and hash.
                    bool received;
                    try
                    {
                        received = FileProtocol.ReceiveFile(ftpSocket, out senderId, out targetId, out fileInfo);
                    }
                    catch
                    {
                        received = false;
                        fileInfo = "";
                        senderId = Guid.Empty;
                        targetId = Guid.Empty;
                    }
                     
                    if (received)
                    {
                        Console.WriteLine("File Transfer Finished.");
                        Message notification = new Message
                        {
                            Type = MessageType.Control,
                            ControlInfo = ControlInfo.FileAvailable,
                            SenderId = senderId,
                            TargetId = targetId,
                            Text = fileInfo
                        };
                        _SendMessageToThisId(targetId, notification);
                        if (ClientHandlerTable.ContainsKey(targetId))  // Private chat.
                            _SendMessageToThisId(senderId, notification);
                        else  // Group chat.
                            SendFileList(targetId);
                    }
                    else
                    {
                        Console.WriteLine("Error occurred while receiving file.");
                    }
//                }
                ftpSocket.Stop();

                lock (this._lock)
                {
                    Console.WriteLine("FTP Listener Thread stopped");
                    Console.WriteLine("-------------------------------------------------------------------------------");
                }
            });
            ftpThread.Name = "Server FTP Listener Thread";
            ftpThread.Start();
        }

        private void SendFileList(Guid targetId)
        {
            StringBuilder sb = new StringBuilder();

            string dir = ".\\" + targetId.ToString();
            if (!Directory.Exists(dir))
                return;

            string[] fileEntries = Directory.GetFiles(dir);
            foreach (var file in fileEntries)
            {   // Must not include the currently uploading files.
                try
                {
                    using (new FileStream(file, FileMode.Open)) {}
                }
                catch
                {   // Probably currently being uploaded.
                    continue;
                }
                FileInfo info = new FileInfo(file);
                sb.Append(info.Name).AppendLine();
                sb.Append(info.Length).AppendLine();
                sb.Append(info.LastWriteTime.ToBinary()).AppendLine();
                // TODO: file uploader
                sb.AppendLine("<UNKNOWN UPLOADER>");
            }

            Message fileList = new Message
            {
                Type = MessageType.Control,
                ControlInfo = ControlInfo.ListOfFiles,
                SenderId = targetId,
                TargetId = ProtocolSettings.NullId,
                Text = sb.ToString()
            };
            _SendMessageToThisId(targetId, fileList);
        }

        private bool _SendMessageToThisId(Guid roomOrClientId, Message message)
        {
            if (ChatroomHandlerTable.ContainsKey(roomOrClientId))  // Sent in a group chat.
                ChatroomHandlerTable[roomOrClientId].BroadcastMessage(message);
            else if (ClientHandlerTable.ContainsKey(roomOrClientId))  // Sent in a private chat.
                ClientHandlerTable[roomOrClientId].SendMessage(message);
            else  // ID not found.
                return false;
            return true;
        }

        private object _lock = new object();

        public void StartListening()
        {
            TcpClient clientSocket = null;
            Message reply = new Message
            {
                Type = MessageType.Control,
                SenderId = ProtocolSettings.NullId,
            };

//            this.StartFTPListener(ProtocolSettings.FileProtocolPort);
            this.ServerSocket.Start();

            lock (_lock)
            {
                Console.WriteLine("Server started, using the following IPEndPoint:");
                Console.WriteLine("  - IP address  = {0}", this.ServerIP);
                Console.WriteLine("  - Port number = {0}", this.ServerListeningPort);
                Console.WriteLine("Waiting for client...");
                Console.WriteLine("-------------------------------------------------------------------------------");
            }

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

                handler.ClientRequestJoinChatroom += Handler_ClientRequestJoinChatroom;
                handler.ClientRequestLeaveChatroom += Handler_ClientRequestLeaveChatroom;
                handler.ClientDisconnected += (sender, e) =>
                {
                    this.ClientHandlerTable.Remove(((ClientHandler) sender).ClientId);
                    DisplayClientList();
                };
                handler.PrivateMessageReceived += Handler_PrivateMessageReceived;
                handler.FileRemoveRequestReceived += Handler_FileRemoveRequestReceived;
                handler.FileDownloadRequestReceived += Handler_FileDownloadRequestReceived;
                handler.FileUploadRequestReceived += Handler_FileUploadRequestReceived;
                handler.ClientRequestCreateChatroom += Handler_ClientRequestCreateChatroom;

                this.ClientHandlerTable.Add(clientId, handler);

                this.publicRoom.AddClient(handler);

                // TODO: send chatroom list updates
                //SendFullChatroomList(this.ClientHandlerTable.Values);
                SendFullClientAndChatroomList();
            }
        }

        private void Handler_FileUploadRequestReceived(object sender, MessageEventArgs e)
        {
            // Create new FTP listener thread and send its port back to the client.

            int port = Utility.FreeTcpPort();

            StartFTPListener(port);
            
            Message message = new Message
            {
                Type = MessageType.Control,
                ControlInfo = ControlInfo.FtpPortOpened,
                SenderId = e.Message.TargetId,
                TargetId = e.Message.SenderId,
                Text = port.ToString()
            };

            ClientHandler senderClient = ClientHandlerTable[e.Message.SenderId];
            senderClient.SendMessage(message);
        }

        private void Handler_ClientRequestCreateChatroom(object sender, MessageEventArgs e)
        {
            Guid newRoomId = CreateChatroom(e.Message.Text);
            ChatroomHandlerTable[newRoomId].AddClient(ClientHandlerTable[e.Message.SenderId]);
            SendFullClientAndChatroomList();
        }

        private void Handler_FileDownloadRequestReceived(object sender, MessageEventArgs e)
        {
            Console.WriteLine("CLIENT REQUESTED FILE DOWNLOAD");

            string[] info = e.Message.Text.Split('\n');

            IPAddress clientAddr = IPAddress.Parse(info[0].Trim());
            int clientPort = int.Parse(info[1].Trim());
            string fileName = info[2].Trim();
            
            string filePath = ".\\" + e.Message.TargetId.ToString() + "\\" + fileName;
            IPEndPoint clientEP = new IPEndPoint(clientAddr, clientPort);

            Thread ftpThread = new Thread(() =>
            {
                string log;
                bool success = FileProtocol.SendFile(filePath, clientEP,
                    e.Message.TargetId, e.Message.SenderId, out log);

                if (success)
                    Console.WriteLine("Handler_FileDownloadRequestReceived: success");
                else
                    Console.WriteLine("Handler_FileDownloadRequestReceived: failed\n\t" + log);
            });

            ftpThread.Name = "FTP Thread";
            ftpThread.Start();
        }

        private void Handler_FileRemoveRequestReceived(object sender, MessageEventArgs e)
        {
            ClientHandler senderClient = ClientHandlerTable[e.Message.SenderId];
            Guid targetId = e.Message.TargetId;
            string fileName = e.Message.Text;
            string filePath = ".\\" + targetId + "\\" + fileName;
            if (File.Exists(filePath))
            {
                File.Delete(filePath);

                Message notification = new Message
                {
                    Type = MessageType.SystemMessage,
                    ControlInfo = ControlInfo.None,
                    SenderId = ProtocolSettings.NullId,
                    TargetId = targetId,
                    Text = "Client '" + senderClient.DisplayName + "' has removed the file '" + fileName + "'."
                };
                _SendMessageToThisId(targetId, notification);
                if (ClientHandlerTable.ContainsKey(targetId))  // Private chat.
                    _SendMessageToThisId(senderClient.ClientId, notification);
                else  // Group chat.
                    SendFileList(targetId);
            }
        }

        private void Handler_PrivateMessageReceived(object sender, MessageEventArgs e)
        {
            if (!ClientHandlerTable.ContainsKey(e.Message.TargetId))
                return;

            var target = ClientHandlerTable[e.Message.TargetId];
            target.SendMessage(e.Message);

            ((ClientHandler) sender).SendMessage(e.Message);
        }

        private void Handler_ClientRequestLeaveChatroom(object sender, ChatroomEventArgs e)
        {
            Guid roomId = e.ChatroomId;
            ClientHandler client = (ClientHandler) sender;

            if (ChatroomHandlerTable.ContainsKey(roomId) &&
                ChatroomHandlerTable[roomId].ClientHandlerTable.ContainsKey(client.ClientId))
            {
                ChatroomHandlerTable[roomId].RemoveClient(client.ClientId);

                //SendFullChatroomList(this.ClientHandlerTable.Values);
                SendFullClientAndChatroomList();
            }
        }

        private void Handler_ClientRequestJoinChatroom(object sender, ChatroomEventArgs e)
        {
            Guid roomId = e.ChatroomId;
            ClientHandler client = (ClientHandler) sender;

            if (ChatroomHandlerTable.ContainsKey(roomId) &&
                !ChatroomHandlerTable[roomId].ClientHandlerTable.ContainsKey(client.ClientId))
            {
                ChatroomHandlerTable[roomId].AddClient(client);

                //SendFullChatroomList(this.ClientHandlerTable.Values);
                SendFullClientAndChatroomList();
            }
        }

        private Guid CreateChatroom(string roomName)
        {
            ChatroomHandler room = new ChatroomHandler();
            room.ChatroomName = roomName;

            ChatroomHandlerTable.Add(room.ChatroomId, room);

            room.ClientJoinedChatroom += Chatroom_ClientJoinedChatroom;
            room.ChatroomBecameEmpty += Chatroom_ChatroomBecameEmpty;
            
            SendFullChatroomList(ClientHandlerTable.Values);
            //SendFullClientAndChatroomList();

            return room.ChatroomId;
        }

        private bool RemoveChatroom(Guid roomId)
        {
            if (roomId == Guid.Empty)
                return false;  // Doesn't allow removal of Public Room.

            ChatroomHandlerTable.Remove(roomId);
            if (Directory.Exists(".\\" + roomId))
            {
                var dir = new DirectoryInfo(".\\" + roomId);
                dir.Delete(recursive: true);
            }

            SendFullChatroomList(ClientHandlerTable.Values);

            return true;
        }

        private void SendFullClientAndChatroomList()
        {
            SendFullChatroomList(this.ClientHandlerTable.Values);
            foreach (var room in this.ChatroomHandlerTable.Values)
                room.SendFullClientList(this.ClientHandlerTable.Values);
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
                ControlInfo = ControlInfo.ListOfChatrooms,
                SenderId = ProtocolSettings.NullId,
                TargetId = ProtocolSettings.NullId,  // No need to be specific?
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
}
