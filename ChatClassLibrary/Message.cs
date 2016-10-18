using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatClassLibrary
{
    public enum MessageType
    {
        Control,  // Control information.
        UserMessage,  // Messages that the user typed.
        SystemMessage  // Special message from the application.
    }

    public enum ControlInfo
    {
        None,
        ClientRequestConnection,  // The client wants to connect to server.
        ConnectionAccepted,  // The server accepted connection.
        ConnectionRejected,  // The server rejected connection.
        RequestFileUpload,  // The client wants to upload a file to server.
        RequestFileDownload,  // The client wants to download a file from server.
        RequestGranted  // The server allowed file upload/download.
    }

    public class Message
    {
        private string text;  // (variable-length) The content of the message.
        private string senderId;  // (16-byte GUID) The client who send the message.
        private string targetId;  // (16-byte GUID) Chatroom that the message is sent to.
        private ControlInfo command;  // (6-bit) 
        private MessageType type;  // (2-bit) 

        public string Text
        {
            get { return text; }
            set { text = value; }
        }

        public string Sender
        {
            get { return senderId; }
            set { senderId = value; }
        }

        public ControlInfo ControlInfo
        {
            get { return command; }
            set { command = value;  }
        }
        
        public MessageType Type
        {
            get { return type; }
            set { type = value; }
        }

        //--------------------------------------------------------------------------------------//

        /// <summary>
        /// Convert a Message object into a byte array (to be sent over a network).
        /// </summary>
        /// <returns>A byte array representing the Message object.</returns>
        public byte[] BuildPacket()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Convert a byte array (read from a network stream) into a Message object.
        /// </summary>
        /// <param name="packet">A byte array representing a Message object.</param>
        /// <returns>A Message object.</returns>
        public static Message FromPacket(byte[] packet)
        {
            throw new NotImplementedException();
        }

    }
}
