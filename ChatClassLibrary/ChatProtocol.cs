using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ChatClassLibrary
{
    public static class ChatProtocol
    {
        /// <summary>
        /// Port that server uses to listen for new client connection.
        /// </summary>
        public static int ServerListeningPort => 60000;

        /// <summary>
        /// Buffer size used in all socket stream reading operations.
        /// </summary>
        public static int StandardBufferSize => 8192;

        /// <summary>
        /// Encoding for all message transfer operations.
        /// </summary>
        public static Encoding TextEncoding => Encoding.UTF8;

        //--------------------------------------------------------------------------------------//

        [Obsolete]
        /// <summary>
        /// Send a basic string message over the given NetworkStream. The data will be sent as 
        /// a byte array with the first 4 bytes specifying the length of message (as Int32), 
        /// and the remaining bytes the message encoded using protocol's default encoding.
        /// </summary>
        /// <param name="message">String message to be sent.</param>
        /// <param name="stream">NetworkStream to write to.</param>
        public static void SendMessage_old(string message, NetworkStream stream)
        {
            int length = message.Length;
            byte[] prefix = Utility.ToByteArray(length);
            byte[] data = TextEncoding.GetBytes(message);

            _Log("Sending string message of length = {0}", length);
            _Log(" |- prefix length = {0} bytes", prefix.Length);
            _Log(" |- data length = {0} bytes", data.Length);

            byte[] sendBytes = Utility.Concat(prefix, data);
            stream.Write(sendBytes, 0, sendBytes.Length);
            stream.Flush();

            _Log("Finished sending a total of {0} bytes", sendBytes.Length);
        }

        [Obsolete]
        /// <summary>
        /// Read a string message from the given NetworkStream. Strips out all protocol-specific 
        /// headers (e.g. message length).
        /// </summary>
        /// <param name="stream">NetworkStream to read from.</param>
        /// <returns>Message as string encoded using protocol's default encoding.</returns>
        public static string ReadMessage_old(NetworkStream stream)
        {
            try
            {
                _Log("Waiting for new message");

                byte[] prefix = new byte[4];
                stream.Read(prefix, 0, 4);
                int dataLength = Utility.BytesToInt32(prefix);

                if (dataLength == 0)
                {
                    return null;
                }

                StringBuilder sb = new StringBuilder();
                int readLength = 0;

                _Log("Reading message of length = {0} bytes", dataLength);

                while (readLength < dataLength)
                {
                    int bytesToRead = Math.Min(StandardBufferSize, dataLength - readLength);
                    byte[] readBytes = new byte[bytesToRead];

                    int bytesRead = stream.Read(readBytes, 0, bytesToRead);
                    _Log(" |- read {0} bytes", bytesRead);

                    sb.AppendFormat("{0}", TextEncoding.GetString(readBytes));
                    readLength += bytesToRead;
                }

                _Log("Finished reading {0} bytes", readLength);

                return sb.ToString();
            }
            catch (IOException ex)
            {
                //return "<stream closed>";
                throw ex;
            }
        }

        //--------------------------------------------------------------------------------------//

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

            _Log("Finished sending a message packet ({0} bytes)", packet.Length);
            _Log(message.ToString());
        }

        public static Message ReceiveMessage(NetworkStream stream)
        {
            try
            {
                _Log("Waiting for new message");

                byte[] packetHeader = new byte[Message.HeaderLength];
                stream.Read(packetHeader, 0, Message.HeaderLength);

                Message message = Message.FromPacket(packetHeader);
                message.TimeReceived = DateTime.Now;

                // Data Length field is located in the last 4 bytes of the header.
                int dataLength = Utility.BytesToInt32(packetHeader, Message.HeaderLength - 4);

                if (dataLength > 0)
                {
                    StringBuilder sb = new StringBuilder();
                    int readLength = 0;

                    _Log("Reading message data of length = {0} bytes", dataLength);
                    while (readLength < dataLength)
                    {
                        int bytesToRead = Math.Min(StandardBufferSize, dataLength - readLength);
                        byte[] readBytes = new byte[bytesToRead];

                        int bytesRead = stream.Read(readBytes, 0, bytesToRead);
                        _Log(" |- read {0} bytes", bytesRead);

                        sb.Append(TextEncoding.GetString(readBytes));
                        readLength += bytesRead;
                    }
                    _Log("Finished reading {0} bytes", readLength);
                    message.Text = sb.ToString();
                    _Log(message.ToString());
                }
                return message;
            }
            catch (IOException ex)
            {
                //return "<stream closed>";
                throw ex;
            }
        }

        //--------------------------------------------------------------------------------------//

        private static void _Log(string message, params object[] args)
        {
            Console.WriteLine("[protocol] " + message, args);
        }
    }
}
