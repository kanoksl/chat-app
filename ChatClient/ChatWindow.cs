using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

using ChatClassLibrary;
using System.IO;

namespace ChatClient
{
    public partial class ChatWindow : Form
    {

        private TcpClient clientSocket = new TcpClient();
        private NetworkStream serverStream = null;
        private string message = null;

        private bool keepListening = true;
        private Thread listenerThread = null;

        public ChatWindow(string defaultUsername = "user")
        {
            InitializeComponent();

            tbxUsername.Text = defaultUsername;
            tbxServerAddress.Text = "127.0.0.1";
        }

        //--------------------------------------------------------------------------------------//

        private bool Connect(string serverAddress, string clientId)
        {
            if (listenerThread != null)
            {

                return false;
            }

            this.message = "<Connecting to chat server...>";
            DisplayMessage();

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
            catch (SocketException ex)
            {
                this.message = "ERROR CONNECTING TO SERVER (check if the address is correct)";
                DisplayMessage();
                return false;
            }
        }

        private void Disconnect()
        {
            //if (listenerThread != null)
            //{
            //    listenerThread.Abort();
            //    listenerThread = null;
            //}
            keepListening = false;
            listenerThread = null;
            ResetConnection();
        }

        private void ResetConnection()
        {
            if (this.InvokeRequired)
            {  // Required for updating the GUI from different thread.
                this.Invoke(new MethodInvoker(ResetConnection));
                return;
            }

            clientSocket.Close();
            clientSocket = new TcpClient();
            serverStream = null;
            this.message = null;

            tbxServerAddress.Enabled = true;
            tbxUsername.Enabled = true;
            btnConnect.Enabled = true;
        }

        private void ListenToServer()
        {
            while (keepListening)
            {
                try
                {
                    this.message = ChatProtocol.ReadMessage(serverStream);
                    DisplayMessage();
                }
                catch (IOException ex)
                {
                    if (keepListening)
                    {
                        this.message = "CANNOT READ SERVER STREAM (the server probably has shut down)";
                        DisplayMessage();
                        ResetConnection();
                    }
                    else
                    {
                        this.message = "<Disconnected>";
                        DisplayMessage();
                    }
                    return;
                }
            }
        }

        private void DisplayMessage()
        {
            if (this.InvokeRequired)
            {  // Required for updating the GUI from different thread.
                this.Invoke(new MethodInvoker(DisplayMessage));
                return;
            }

            listChat.Items.Add(this.message);
            listChat.TopIndex = listChat.Items.Count - 1;
            listChat.SelectedIndex = listChat.Items.Count - 1;
        }

        //--------------------------------------------------------------------------------------//

        private void btnConnect_Click(object sender, EventArgs e)
        {
            string serverAddress = tbxServerAddress.Text.Trim();
            string clientId = tbxUsername.Text.Trim();

            if (Connect(serverAddress, clientId))
            {
                tbxServerAddress.Enabled = false;
                tbxUsername.Enabled = false;
                btnConnect.Enabled = false;
            }
            else
            {
                this.message = "<Connecting to chat server failed.>";
                DisplayMessage();
            }
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            string newMessage = tbxMessage.Text.Trim();

            if (newMessage == "end")
            {
                Disconnect();
                return;
            }
            else if (newMessage.Length > 0)
            {
                ChatProtocol.SendMessage(newMessage, serverStream);

                tbxMessage.Text = "";
            }


        }

        private void tbxMessage_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char) Keys.Enter)
            {
                btnSend_Click(sender, e);
                e.Handled = true;
            }
        }

        //--------------------------------------------------------------------------------------//

    }
}
