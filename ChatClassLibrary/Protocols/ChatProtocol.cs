using System;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace ChatClassLibrary.Protocols
{
    public static class ChatProtocol
    {
        // The simplified structure of a message packet is:
        // 
        //   [ <45-byte> || <variable length, optional> ] 
        //       |            |
        //       |           Message Text
        //      Fixed-length Message Header
        // 
        // SendMessage first builds a byte packet, and simply writes the packet to network stream.
        // ReceiveMessage reads 45-byte header, then reads the text of length specified in the
        //   header, then builds a Message object for returning.

        public static void SendMessage(Message message, NetworkStream stream)
        {
            byte[] packet = message.BuildPacket();
            Message.UpdatePacketTimeStamp(packet);

            stream.Write(packet, 0, packet.Length);
            stream.Flush();

            ChatProtocol._Log("Finished sending a message packet ({0} bytes)", packet.Length);
            ChatProtocol._Log(message.ToString());
        }

        public static Message ReceiveMessage(NetworkStream stream)
        {
            try
            {
                ChatProtocol._Log("Waiting for new message...");

                byte[] headerPacket = new byte[Message.HeaderLength];
                stream.Read(headerPacket, 0, Message.HeaderLength);  // Block here.

                Message message = Message.FromPacket(headerPacket);
                message.TimeReceived = DateTime.Now;

                // Data Length field is located in the last 4 bytes of the header.
                int dataLength = Utility.BytesToInt32(headerPacket, Message.HeaderLength - 4);

                if (dataLength == 0)  // No text part to read.
                    return message;

                StringBuilder sb = new StringBuilder();
                int readLength = 0;

                ChatProtocol._Log("Reading message data of length = {0} bytes", dataLength);
                while (readLength < dataLength)
                {
                    int bytesToRead = Math.Min(ProtocolSettings.ChatProtocolBufferSize, dataLength - readLength);
                    byte[] readBytes = new byte[bytesToRead];

                    int bytesRead = stream.Read(readBytes, 0, bytesToRead);
                    ChatProtocol._Log(" |- read {0} bytes", bytesRead);

                    sb.Append(ProtocolSettings.TextEncoding.GetString(readBytes));
                    readLength += bytesRead;
                }

                message.Text = sb.ToString();

                ChatProtocol._Log("Finished reading {0} bytes", readLength);
                ChatProtocol._Log(message.ToString());

                return message;
            }
            catch (IOException ex)
            {   // Mostly happened when the stream is closed.
                ChatProtocol._Log("Exception caught: " + ex.Message);
                throw;
            }
        }

        //--------------------------------------------------------------------------------------//

        private static void _Log(string message, params object[] args)
        {
            Console.WriteLine("[protocol] " + message, args);
        }
    }
}
