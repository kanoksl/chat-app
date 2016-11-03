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
    public partial class ChatWindowV2 : Form
    {

        private MessageClient client;

        public ChatWindowV2(string defaultUsername = "user")
        {
            InitializeComponent();

            // Initialize GUI data.
            tbxUsername.Text = defaultUsername;
            tbxServerAddress.Text = "127.0.0.1";
            tbxFilePath.Text = @"D:\downloaded_big file.pdf";

        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            if (client != null && client.Connected)
            {
                DisplayMessage("ALREADY CONNECTED");
                return;
            }
            IPAddress serverIP;
            if (IPAddress.TryParse(tbxServerAddress.Text.Trim(), out serverIP))
            {
                client = new MessageClient(serverIP);
                client.DisplayName = tbxUsername.Text;
                client.ConnectionEstablished += Client_ConnectionEstablished;
                client.ConnectionFailed += Client_ConnectionFailed;
                client.MessageReceived += Client_MessageReceived;
                client.MessageSent += Client_MessageSent;
                client.ServerDisconnected += Client_ServerDisconnected;
                client.ClientDisconnected += Client_ClientDisconnected;
                client.MessageReceivingingFailed += Client_MessageReceivingingFailed;
                client.MessageSendingFailed += Client_MessageSendingFailed;
                client.ConnectToServer();
            }
            else
            {
                DisplayMessage("INVALID IP");
            }

        }

        private void Client_MessageSendingFailed(object sender, MessageEventArgs e)
        {
            DisplayMessage("SEND FAIL: msg = " + e.Message.Text);
        }

        private void Client_MessageReceivingingFailed(object sender, MessageEventArgs e)
        {
            DisplayMessage("RECV FAIL: msg = " + e.Message.Text);
        }

        private void Client_ClientDisconnected(object sender, ConnectionEventArgs e)
        {
            DisplayMessage("CLIENT DISCON");
        }

        private void Client_ServerDisconnected(object sender, ConnectionEventArgs e)
        {
            DisplayMessage("SERVER DISCON");
        }

        private void Client_MessageSent(object sender, MessageEventArgs e)
        {
            DisplayMessage("(sent) " + e.Message.Text);
        }

        private void Client_MessageReceived(object sender, MessageEventArgs e)
        {
            DisplayMessage(e.Message.Text);
        }

        private void Client_ConnectionFailed(object sender, ConnectionEventArgs e)
        {
            DisplayMessage("Connection Failed...");
        }

        private void Client_ConnectionEstablished(object sender, ConnectionEventArgs e)
        {
            DisplayMessage("Connected!");
            DisplayMessage("  Server address: " + e.ServerEndPoint.ToString());
            DisplayMessage("  Client address: " + e.ClientSocket.Client.LocalEndPoint.ToString());

            client.BeginReceive();
        }

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

        private void btnSend_Click(object sender, EventArgs e)
        {
            string message = tbxMessage.Text.Trim();
            client.SendMessage(message, Guid.Empty);
            tbxMessage.Text = "";
        }

        private void btnDisconnect_Click(object sender, EventArgs e)
        {
            client.Disconnect();
        }
    }

}
