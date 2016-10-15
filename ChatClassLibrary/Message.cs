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
        private string text;  // The content of the message.
        private string senderId;  // The client who send the message.
        private ControlInfo command;
        private MessageType type;

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


    }
}
