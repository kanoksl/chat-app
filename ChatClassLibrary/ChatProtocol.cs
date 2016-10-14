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
        public static Encoding TextEncoding => Encoding.ASCII;

        
        private static byte[] IntToBytes(int value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            return bytes;
        }

        private static int BytesToInt(byte[] bytes)
        {
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            int value = BitConverter.ToInt32(bytes, 0);
            return value;
        }

        private static T[] Concat<T>(T[] x, T[] y)
        {
            var z = new T[x.Length + y.Length];
            x.CopyTo(z, 0);
            y.CopyTo(z, x.Length);
            return z;
        }

        /// <summary>
        /// Send a basic string message over the given NetworkStream. The data will be sent as 
        /// a byte array with the first 4 bytes specifying the length of message (as Int32), 
        /// and the remaining bytes the message encoded using protocol's default encoding.
        /// </summary>
        /// <param name="message">String message to be sent.</param>
        /// <param name="stream">NetworkStream to write to.</param>
        public static void SendMessage(string message, NetworkStream stream)
        {
            int length = message.Length;
            byte[] prefix = IntToBytes(length);
            byte[] data = TextEncoding.GetBytes(message);

            _Log("Sending string message of length = {0}", length);
            _Log(" |- prefix length = {0} bytes", prefix.Length);
            _Log(" |- data length = {0} bytes", data.Length);

            byte[] sendBytes = Concat(prefix, data);
            stream.Write(sendBytes, 0, sendBytes.Length);
            stream.Flush();

            _Log("Finished sending a total of {0} bytes", sendBytes.Length);
        }

        /// <summary>
        /// Read a string message from the given NetworkStream. Strips out all protocol-specific 
        /// headers (e.g. message length).
        /// </summary>
        /// <param name="stream">NetworkStream to read from.</param>
        /// <returns>Message as string encoded using protocol's default encoding.</returns>
        public static string ReadMessage(NetworkStream stream)
        {
            try
            {
                _Log("Waiting for new message");

                byte[] prefix = new byte[4];
                stream.Read(prefix, 0, 4);
                int dataLength = BytesToInt(prefix);

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


        private static void _Log(string message, params object[] args)
        {
            Console.WriteLine("[protocol] " + message, args);
        }
    }
}
