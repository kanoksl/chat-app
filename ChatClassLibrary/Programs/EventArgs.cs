using System;
using System.Net;
using System.Net.Sockets;
using ChatClassLibrary.Protocols;

namespace ChatClassLibrary
{
    public class ChatroomEventArgs : EventArgs
    {
        public Guid ChatroomId { get; private set; }
        public string ChatroomName { get; private set; }
        public ChatroomEventArgs(Guid chatroomId, string chatroomName)
        {
            this.ChatroomId = chatroomId;
            this.ChatroomName = chatroomName;
        }
        public ChatroomEventArgs() { }
    }

    public class ConnectionEventArgs : EventArgs
    {
        public IPEndPoint ServerEndPoint { get; private set; }
        public TcpClient ClientSocket { get; private set; }
        public ConnectionEventArgs(IPEndPoint serverEP, TcpClient clientSocket)
        {
            this.ServerEndPoint = serverEP;
            this.ClientSocket = clientSocket;
        }
        public ConnectionEventArgs() { }
    }

    public class MessageEventArgs : EventArgs
    {
        public Message Message { get; private set; }
        public MessageEventArgs(Message message) { this.Message = message; }
        public MessageEventArgs() { }
    }
}