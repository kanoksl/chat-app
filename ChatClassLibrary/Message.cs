using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatClassLibrary
{
    [Flags]
    public enum MessageType
    {
        Control = 0x0,          // 00  Control information.
        SystemMessage = 0x2,    // 10  Special message from the application.
        UserMessage = 0x3       // 11  Messages that the user typed.
    }

    [Flags]
    public enum ControlInfo  // TODO: may reassign the values to make use of bitwise operations
    {
        None = 0x00,                     // 00 0000  Data message, not control message.
        ClientRequestConnection = 0x20,  // 10 0000  The client wants to connect to server.
        ConnectionAccepted = 0x10,       // 01 0000  The server accepted connection.
        ConnectionRejected = 0x11,       // 01 0001  The server rejected connection.
        RequestFileUpload = 0x2A,        // 10 1010  The client wants to upload a file to server.
        RequestFileDownload = 0x2B,      // 10 1011  The client wants to download a file from server.
        RequestGranted = 0x1C,           // 01 1100  The server allowed file upload/download.
        RequestDenied = 0x1D             // 01 1101  The server rejected file transfer request.
    }

    // The structure of a message packet is:
    //
    //    2-bit Message Type
    //     |    6-bit Control Code                     Time Sent (binary DateTime)
    //     |     |                                      |
    //   [ 1 1 , 0 0 0 0 0 0 | <16-byte> | <16-byte> | <8-byte> || <4-byte> | <variable> ]
    //                           |           |                       |          |
    //                  Sender GUID          |      Data Length (int32)         |
    //                     Target chatroom GUID              Data (actual message)
    //
    // The first byte is a combination of Message Type and Control Info.
    // In case of server-generated messages, the Sender GUID field is set to a special value, e.g. all zeroes.
    // The Data Length and Data fields can be missing (for some types of control message).
    // Packet length must be 41 bytes (no data) or longer than 45 bytes (with data).
    //
    public class Message
    {
        private static Encoding TextEncoding => ChatProtocol.TextEncoding;

        //--------------------------------------------------------------------------------------//

        /// <summary>
        /// (2-bit) The type of the message.
        /// </summary>
        public MessageType Type { get; set; }

        /// <summary>
        /// (6-bit) The control code, if the message is not a data message.
        /// </summary>
        public ControlInfo ControlInfo { get; set; }

        /// <summary>
        /// (16-byte GUID) The client who send the message.
        /// </summary>
        public Guid SenderId { get; set; }

        /// <summary>
        /// (16-byte GUID) Chatroom that the message is sent to.
        /// </summary>
        public Guid TargetId { get; set; }

        /// <summary>
        /// (8-byte) The time the message was sent (right before writing on a network stream).
        /// </summary>
        public DateTime TimeSent { get; set; }

        /// <summary>
        /// (not included in packet) The time the message was read by the receiver.
        /// </summary>
        public DateTime TimeReceive { get; set; }

        /// <summary>
        /// (variable-length) The content of the message.
        /// </summary>
        public string Text { get; set; }

        //--------------------------------------------------------------------------------------//

        /// <summary>
        /// Convert a Message object into a byte array (to be sent over a network).
        /// </summary>
        /// <returns>A byte array representing the Message object.</returns>
        public byte[] BuildPacket()
        {
            byte[] packet = null;

            byte[] firstByte = { (byte) ((((int) this.Type) << 6) + ((int) this.ControlInfo)) };
            byte[] senderGuid = this.SenderId.ToByteArray();
            byte[] targetGuid = this.TargetId.ToByteArray();
            byte[] timeSent = Utility.ToByteArray(this.TimeSent.ToUniversalTime().ToBinary()); //???

            if (this.Text != null)
            {
                byte[] data = TextEncoding.GetBytes(this.Text);
                byte[] dataLength = Utility.ToByteArray(data.Length);
                packet = Utility.Concat(firstByte, senderGuid, targetGuid, timeSent, dataLength, data);
            }
            else
            {
                packet = Utility.Concat(firstByte, senderGuid, targetGuid, timeSent);
            }

            return packet;
        }

        /// <summary>
        /// Convert a byte array (read from a network stream) into a Message object.
        /// </summary>
        /// <param name="packet">A byte array representing a Message object.</param>
        /// <returns>A Message object.</returns>
        public static Message FromPacket(byte[] packet)
        {
            if (packet.Length < 41) return null;  // Incorrect packet bytes (too short).
            if (packet.Length < 45 && packet.Length > 41) return null;

            int messageType = packet[0] >> 6;
            int controlCode = packet[0] & 0x3F;

            Guid senderGuid = new Guid(Utility.Slice(packet, 1, 16));
            Guid targetGuid = new Guid(Utility.Slice(packet, 1 + 16, 16));

            long timeSentLong = Utility.BytesToInt64(Utility.Slice(packet, 1 + 16 + 16, 8));
            DateTime timeSent = DateTime.FromBinary(timeSentLong);
            string text = (packet.Length == 41) ? null
                        : TextEncoding.GetString(packet, 45, packet.Length - 45);

            return new Message
            {
                Type = (MessageType) messageType,
                ControlInfo = (ControlInfo) controlCode,
                SenderId = senderGuid,
                TargetId = targetGuid,
                TimeSent = timeSent,
                TimeReceive = DateTime.MinValue, //???
                Text = text
            };
        }

        //--------------------------------------------------------------------------------------//
        
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("Message (T:{0}, CTRL:{1})\n", this.Type, this.ControlInfo);
            sb.Append("  - sender: ").Append(this.SenderId).AppendLine();
            sb.Append("  - target: ").Append(this.TargetId).AppendLine();
            sb.Append("  - time sent: ").Append(this.TimeSent).AppendLine();
            sb.Append("  - time receive: ").Append(this.TimeReceive).AppendLine();
            if (this.Text != null)
            {
                sb.Append("  - content: ").Append(this.Text).AppendLine();
                sb.Append("  - content length (bytes): ").Append(TextEncoding.GetByteCount(this.Text));
            }
            return sb.ToString();
        }


        static void Main(string[] args)
        {
            Message m = new Message
            {
                Type = MessageType.SystemMessage,
                ControlInfo = ControlInfo.ConnectionRejected,
                SenderId = Guid.NewGuid(),
                TargetId = Guid.NewGuid(),
                TimeSent = DateTime.Now,
                TimeReceive = DateTime.MinValue,
                Text = "Hello World"
            };
            byte[] b = m.BuildPacket();
            Message m2 = Message.FromPacket(b);
            Console.WriteLine(m);
            Console.WriteLine(m2);
            Console.Read();
        }

    }
}
