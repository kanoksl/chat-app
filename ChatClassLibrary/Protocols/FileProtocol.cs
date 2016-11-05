using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ChatClassLibrary.Protocols
{
    public static class FileProtocol
    {
        /// <summary>
        /// Length of the info packet = SenderID + TargetID + FileName + FileSize + FileHash.
        /// </summary>
        private const int InfoPacketLength = 16 + 16 + 256 + 8 + 16;

        /// <summary>
        /// Send a file over TCP sockets.
        /// </summary>
        /// <param name="filePath">Local path of the file to be sent.</param>
        /// <param name="receiverEP">IP address and port of the receiver (running ReceiveFile()).</param>
        /// <param name="senderId">GUID of the sender, e.g. client ID.</param>
        /// <param name="targetId">GUID of the target, e.g. chatroom ID.</param>
        /// <param name="log">Output string for logging errors.</param>
        /// <param name="progress">Progress reporter.</param>
        /// <returns>True if the file sending finished successfully, otherwise false.</returns>
        public static bool SendFile(string filePath,
                                    IPEndPoint receiverEP,
                                    Guid senderId,
                                    Guid targetId,
                                    out string log,
                                    IProgress<double> progress = null)
        {
            TcpClient socket = new TcpClient();
            NetworkStream networkStream = null;

            try
            {
                FileProtocol._Log("Connecting to receiver at {0}", receiverEP);
                socket.Connect(receiverEP);
                networkStream = socket.GetStream();

                FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                long fileLength = fileStream.Length;
                long packetCount = (long) Math.Ceiling((double) fileLength /
                                                       ProtocolSettings.FileProtocolBufferSize);
                long bytesSent = 0;

                //// Step 1: Send the information about the file as the first packet. ///////////////////////

                byte[] senderIdBytes = senderId.ToByteArray();  // 16 bytes.
                byte[] targetIdBytes = targetId.ToByteArray();  // 16 bytes.

                byte[] fileName = new byte[256];  // Max file name length = 255 bytes.
                ProtocolSettings.TextEncoding.GetBytes(Path.GetFileName(filePath)).CopyTo(fileName, 0);
                byte[] fileSize = Utility.ToByteArray(fileLength);  // 8 bytes.
                byte[] fileHash = Utility.CalculateMD5(filePath);  // 16 bytes.

                byte[] infoPacket = Utility.Concat(senderIdBytes, targetIdBytes,
                                                   fileName, fileSize, fileHash); // 312 bytes.

                FileProtocol._Log("Sending the sender/file information packet...");
                FileProtocol._Log(" SenderID = " + senderId);
                FileProtocol._Log(" TargetID = " + targetId);
                FileProtocol._Log(" FileName = " + Path.GetFileName(filePath));
                FileProtocol._Log(" FileSize = {0} bytes", fileLength);
                FileProtocol._Log(" FileHash = " + Utility.ToHashString(fileHash));

                networkStream.Write(infoPacket, 0, infoPacket.Length);

                //// Step 2: Receive the confirmation from the receiver. ////////////////////////////////////

                byte[] response = new byte[256];
                networkStream.Read(response, 0, response.Length);
                bool receiverAccept = (response[0] == 1);  // The first byte indicates accept/reject.
                if (!receiverAccept)
                {
                    string errMsg = ProtocolSettings.TextEncoding.GetString(response, 1, response.Length - 1);
                    log = "Receiver rejected file transfer request. Reason: " + errMsg;
                    FileProtocol._Log(log);
                    return false;
                }

                //// Step 3: Start the actual file transfer. ////////////////////////////////////////////////

                progress?.Report(0);  // 0% complete.

                FileProtocol._Log("Sending a file of length = {0} bytes ({1} packets)", fileLength, packetCount);
                FileProtocol._Log(" ({0})", filePath);

                for (int i = 0; i < packetCount; i++)
                {
                    int bytesToSend = (int) Math.Min(fileLength - bytesSent, ProtocolSettings.FileProtocolBufferSize);
                    byte[] sendBytes = new byte[bytesToSend];

                    fileStream.Read(sendBytes, 0, bytesToSend);
                    FileProtocol._Log(" |- read {0} bytes from file", bytesToSend);
                    networkStream.Write(sendBytes, 0, bytesToSend);
                    FileProtocol._Log(" |- sent {0} bytes to receiver", bytesToSend);

                    bytesSent += bytesToSend;
                    progress?.Report((double) bytesSent / fileLength * 100);
                }

                progress?.Report(100);  // Finished transfer; 100% complete.
                FileProtocol._Log("Finished sending {0} bytes ({1} packets)", fileLength, packetCount);

                fileStream.Close();

                log = "File transfer finished without problems.";
                return true;
            }
            catch (Exception ex)
            {
                log = "Exception caught: " + ex.Message;
                FileProtocol._Log(log);
                return false;
            }
            finally
            {
                networkStream?.Close();
                socket.Close();
            }
        }

        /// <summary>
        /// Receive a file sending between TCP sockets.
        /// </summary>
        /// <param name="listenerSocket">Socket that the sender will connect to.</param>
        /// <param name="senderId">GUID of the sender, e.g. client ID.</param>
        /// <param name="targetId">GUID of the target, e.g. chatroom ID.</param>
        /// <param name="log">Output string for file information such as file name, size and hash.</param>
        /// <param name="savePath">Path, including the file name, to save to. If null, the file will
        /// be saved inside ".\[targetId]\".</param>
        /// <param name="progress">Progress reporter.</param>
        /// <returns>True if the file is downloaded successfully, otherwise false.</returns>
        public static bool ReceiveFile(TcpListener listenerSocket,
                                       out Guid senderId,
                                       out Guid targetId,
                                       out string log,
                                       string savePath = null,
                                       IProgress<double> progress = null)
        {
            TcpClient socket = null;
            NetworkStream networkStream = null;

            try
            {
                socket = listenerSocket.AcceptTcpClient();
                networkStream = socket.GetStream();

                //// Step 1: Receive information about the sender and the file. /////////////////////////////////

                byte[] infoPacket = new byte[FileProtocol.InfoPacketLength];
                networkStream.Read(infoPacket, 0, infoPacket.Length);

                senderId = new Guid(Utility.Slice(infoPacket, 0, 16));
                targetId = new Guid(Utility.Slice(infoPacket, 16, 16));

                string fileName = ProtocolSettings.TextEncoding.GetString(infoPacket, 32, 256).Trim('\0');
                long fileLength = Utility.BytesToInt64(infoPacket, 288);
                byte[] fileHash = Utility.Slice(infoPacket, 296, 16);
                string fileHashStr = Utility.ToHashString(fileHash);

                FileProtocol._Log("Received a sender/file information packet");
                FileProtocol._Log(" SenderID = " + senderId.ToString());
                FileProtocol._Log(" TargetID = " + targetId.ToString());
                FileProtocol._Log(" FileName = " + fileName);
                FileProtocol._Log(" FileSize = {0} bytes", fileLength);
                FileProtocol._Log(" FileHash = " + fileHashStr);

                StringBuilder sb = new StringBuilder();
                sb.Append(fileName).AppendLine(); // File name from sender.
                sb.Append(fileLength).AppendLine(); // File length in bytes.
                sb.Append(DateTime.Now.ToBinary()).AppendLine(); // Time received.
                sb.Append(fileHashStr); // File checksum.
                log = sb.ToString();

                if (savePath == null)
                {
                    string saveFolder = ".\\" + targetId;
                    Directory.CreateDirectory(saveFolder); // Create the save folder if not exists
                    savePath = saveFolder + "\\" + fileName;
                }

                //// Step 2: Respond whether to accept or reject file transfer. /////////////////////////////////

                byte[] response = new byte[256];
                bool accept = !File.Exists(savePath); // Reject if file already exists. (No overwriting).

                if (accept)
                {
                    response[0] = 1;
                    networkStream.Write(response, 0, response.Length);
                    FileProtocol._Log("Accepted file transfer.");
                }
                else
                {
                    response[0] = 0;
                    string message = "There is already a file with that name on the receiver side.";
                    ProtocolSettings.TextEncoding.GetBytes(message, 0, message.Length, response, 1);
                    networkStream.Write(response, 0, response.Length);
                    FileProtocol._Log("Rejected file transfer.");
                    return false;
                }

                //// Step 3: Start the actual file transfer. ////////////////////////////////////////////////////

                progress?.Report(0); // 0% complete.

                FileProtocol._Log("Receiving a file of length = {0} bytes", fileLength);
                FileProtocol._Log(" (Save to '{0}')", savePath);

                FileStream fileStream = new FileStream(savePath, FileMode.OpenOrCreate, FileAccess.Write);
                long readLength = 0;
                int packetCount = 0;

                while (readLength < fileLength)
                {
                    int bytesToRead = (int) Math.Min(ProtocolSettings.FileProtocolBufferSize, fileLength - readLength);
                    byte[] readBytes = new byte[bytesToRead];

                    int bytesRead = networkStream.Read(readBytes, 0, bytesToRead);
                    FileProtocol._Log(" |- read {0} bytes from network", bytesRead);
                    fileStream.Write(readBytes, 0, bytesRead);
                    FileProtocol._Log(" |- write {0} bytes to file", bytesRead);

                    readLength += bytesToRead;
                    packetCount += 1;

                    progress?.Report((double) readLength / fileLength * 100);
                }

                progress?.Report(100); // Finished transfer; 100% complete.
                FileProtocol._Log("Finished receiving {0} bytes ({1} packets)", fileLength, packetCount);

                fileStream.Close();

                //// Step 4: Check the downloaded file's checksum. //////////////////////////////////////////////

                byte[] downloadedFileHash = Utility.CalculateMD5(savePath); // 16 bytes.
                FileProtocol._Log(" Hash (Downloaded)  = " + Utility.ToHashString(downloadedFileHash));
                FileProtocol._Log(" Hash (From Sender) = " + Utility.ToHashString(fileHash));
                if (!Enumerable.SequenceEqual(fileHash, downloadedFileHash))
                {
                    // Hash differs from the sender's.
                    FileProtocol._Log("ERROR: hash differs from original; the downloaded file is damaged.");
                    // Delete the downloaded (damaged) file.
                    if (File.Exists(savePath))
                        File.Delete(savePath);
                    return false;
                }

                // Passed all steps, download finished successfully.
                return true;
            }
            catch (Exception ex)
            {
                FileProtocol._Log("Exception caught: " + ex.Message);
                throw;
            }
            finally
            {
                networkStream?.Close();
                socket?.Close();
            }
        }

        //--------------------------------------------------------------------------------------//

        private static void _Log(string message, params object[] args)
        {
            Console.WriteLine("[file_protocol] " + message, args);
        }
    }
}
