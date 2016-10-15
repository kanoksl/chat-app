using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ChatClassLibrary
{
    public static class FileProtocol
    {
        /// <summary>
        /// Port that server uses to listen for file upload/download requests.
        /// </summary>
        public static int FtpListeningPort => 60001;

        /// <summary>
        /// Buffer size used in file sending/receiving operations.
        /// </summary>
        public static int FtpBufferSize => 8192;


        public static bool SendFile(string filePath, IPEndPoint receiverEP,
            IProgress<double> progress = null)
        {
            TcpClient receiverSocket = null;
            NetworkStream receiverStream = null;

            try
            {
                _Log("Connecting to receiver at {0}", receiverEP);

                receiverSocket = new TcpClient(receiverEP);
                receiverStream = receiverSocket.GetStream();

                progress.Report(0);  // Starting transfer; 0% complete.

                FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                int packetCount = (int) Math.Ceiling((double) fileStream.Length / FtpBufferSize);
                long fileLength = fileStream.Length;
                long bytesSent = 0;

                _Log("Sending a file of length = {0} bytes ({1} packets)", fileLength, packetCount);
                _Log(" ({0})", filePath);

                for (int i = 0; i < packetCount; i++)
                {
                    int bytesToSend = (int) Math.Min(fileLength - bytesSent, FtpBufferSize);
                    byte[] sendBytes = new byte[bytesToSend];

                    fileStream.Read(sendBytes, 0, bytesToSend);
                    _Log(" |- read {0} bytes from file", bytesToSend);
                    receiverStream.Write(sendBytes, 0, bytesToSend);
                    _Log(" |- sent {0} bytes to receiver", bytesToSend);

                    bytesSent += bytesToSend;

                    progress?.Report((double) bytesSent / fileLength * 100);
                }

                progress?.Report(100);  // Finished transfer; 100% complete.

                _Log("Finished sendign {0} bytes ({1} packets)", fileLength, packetCount);

                fileStream.Close();

                return true;
            }
            catch (Exception ex)
            {
                _Log("Exception caught: " + ex.Message);
                return false;
            }
            finally
            {
                receiverStream?.Close();
                receiverSocket?.Close();
            }
        }

        public static bool ReceiveFile(IPEndPoint receiverEP,
            IProgress<double> progress = null)
        {
            TcpListener listenerSocket = new TcpListener(receiverEP);
            listenerSocket.Start();

            // TODO: receive file

            return false;
        }

        //--------------------------------------------------------------------------------------//

        private static void _Log(string message, params object[] args)
        {
            Console.WriteLine("[file_protocol] " + message, args);
        }
    }
}
