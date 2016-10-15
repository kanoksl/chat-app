using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

using ChatClassLibrary;

namespace ChatClient
{
    public partial class ChatWindow : Form
    {

        private TcpClient clientSocket = new TcpClient();
        private NetworkStream serverStream = null;

        private bool keepListening = true;
        private Thread listenerThread = null;


        public ChatWindow(string defaultUsername = "user")
        {
            InitializeComponent();

            // Initialize GUI data.
            tbxUsername.Text = defaultUsername;
            tbxServerAddress.Text = "127.0.0.1";

            UpdateGui(GuiUpdateEvent.CanStartConnection);
        }

        //--------------------------------------------------------------------------------------//

        private bool Connect(string serverAddress, string clientId)
        {
            if (listenerThread != null) return false;  // Already connected.

            DisplayMessage("<Connecting to chat server...>");

            try
            {
                clientSocket.Connect(serverAddress, ChatProtocol.ServerListeningPort);

                // First message: request connection with clientId (username)
                serverStream = clientSocket.GetStream();
                ChatProtocol.SendMessage(clientId, serverStream);

                keepListening = true;
                listenerThread = new Thread(ListenToServer);
                listenerThread.Start();

                return true;
            }
            catch (SocketException)
            {
                DisplayMessage("ERROR CONNECTING TO SERVER (check if the address is correct)");
                return false;
            }
        }

        private void ResetConnection()
        {
            keepListening = false;
            listenerThread = null;

            clientSocket.Close();
            clientSocket = new TcpClient();
            serverStream = null;
        }

        private void ListenToServer()
        {
            while (keepListening)
            {
                try
                {
                    string message = ChatProtocol.ReadMessage(serverStream);
                    DisplayMessage(message);
                }
                catch (IOException)
                {
                    if (keepListening)
                    {   // Cannot read from server stream.
                        DisplayMessage("CANNOT READ SERVER STREAM (the server probably has shut down)");
                        ResetConnection();
                        UpdateGui(GuiUpdateEvent.CanStartConnection);
                    }
                    else
                    {   // Client side closed the connection.
                        DisplayMessage("<DISCONNECTED>");
                    }
                    return;
                }
            }
        }

        //--------------------------------------------------------------------------------------//

        private void btnConnect_Click(object sender, EventArgs e)
        {
            string serverAddress = tbxServerAddress.Text.Trim();
            string clientId = tbxUsername.Text.Trim();

            bool connectionSuccess = Connect(serverAddress, clientId);

            if (connectionSuccess)
            {
                UpdateGui(GuiUpdateEvent.ConnectionSuccessful);
            }
            else
            {
                DisplayMessage("<Failed connecting to server.>");
            }
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            string newMessage = tbxMessage.Text.Trim();

            if (newMessage.Length > 0)
            {   // Doesn't allow empty message.
                ChatProtocol.SendMessage(newMessage, serverStream);

                tbxMessage.Text = "";
            }
        }

        private void btnDisconnect_Click(object sender, EventArgs e)
        {
            ResetConnection();
            UpdateGui(GuiUpdateEvent.CanStartConnection);
        }

        private void tbxMessage_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char) Keys.Enter)
            {
                btnSend_Click(sender, e);
                e.Handled = true;
            }
        }
        private void btnFileBrowse_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Title = "Choose a file to upload";
            dialog.Filter = "All Files (*.*)|*.*";
            dialog.CheckFileExists = true;
            dialog.InitialDirectory = @"D:\";
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                tbxFilePath.Text = dialog.FileName;
            }
        }

        private void btnUploadFile_Click(object sender, EventArgs e)
        {
            var progressReporter = new Progress<double>();
            progressReporter.ProgressChanged += (s, progress) =>
            {
                pgbUploadProgress.Value = (int) progress;
            };

            string filePath = tbxFilePath.Text.Trim();
            IPEndPoint serverEP = new IPEndPoint(
                IPAddress.Parse(tbxServerAddress.Text),
                FileProtocol.FtpListeningPort);

            Thread ftpThread = new Thread(() =>
            {
                bool success = FileProtocol.SendFile(filePath, serverEP, progressReporter);
                DisplayMessage(success ? "<file upload completed>"
                                       : "<FILE UPLOAD FAILED>");
            });
            ftpThread.Start();
        }

        //--------------------------------------------------------------------------------------//

        /// <summary>
        /// Show a simple string message on the GUI.
        /// </summary>
        /// <param name="message">A string message to be shown.</param>
        private void DisplayMessage(string message)
        {
            if (this.InvokeRequired)
            {   // Required for updating the GUI from different thread.
                this.Invoke((MethodInvoker) delegate { DisplayMessage(message); });
                return;
            }

            listChat.Items.Add(message);
            listChat.TopIndex = listChat.Items.Count - 1;
            listChat.SelectedIndex = listChat.Items.Count - 1;
        }

        enum GuiUpdateEvent
        {
            CanStartConnection,     // Currently not connected to any server.
            ConnectionSuccessful    // Now connnected to a server.
        }

        private void UpdateGui(GuiUpdateEvent updateEvent)
        {
            if (this.InvokeRequired)
            {   // Required for updating the GUI from different thread.
                this.Invoke((MethodInvoker) delegate { UpdateGui(updateEvent); });
                return;
            }

            switch (updateEvent)
            {
                case GuiUpdateEvent.CanStartConnection:
                    tbxServerAddress.Enabled = true;
                    tbxUsername.Enabled = true;
                    btnConnect.Enabled = true;
                    btnDisconnect.Enabled = false;
                    btnUploadFile.Enabled = false;
                    break;
                case GuiUpdateEvent.ConnectionSuccessful:
                    tbxServerAddress.Enabled = false;
                    tbxUsername.Enabled = false;
                    btnConnect.Enabled = false;
                    btnDisconnect.Enabled = true;
                    btnUploadFile.Enabled = true;
                    break;
                default:
                    break;
            }
        }

        //--------------------------------------------------------------------------------------//
    }
}
