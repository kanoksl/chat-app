using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using ChatClassLibrary;

namespace ChatClient
{
    public partial class ChatWindowV3 : Form
    {

        private MessageClient client;

        public ChatWindowV3(string defaultUsername = "user")
        {
            InitializeComponent();

            // GUI event handlers
            btnConnect.Click += btnConnect_Click;
            btnDisconnect.Click += btnDisconnect_Click;
            btnSend.Click += btnSend_Click;
            tbxInput.KeyPress += tbxInput_KeyPress;

            // Initialize GUI data.
            tbxUsername.Text = defaultUsername;
            tbxServerAddress.Text = "127.0.0.1";
        }
        
        private void tbxInput_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char) Keys.Enter)
            {
                btnSend_Click(sender, e);
                e.Handled = true;
            }
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            string message = tbxInput.Text.Trim();
            client.SendMessage(message, Guid.Empty);  // TODO: should be the chatroom ID
            tbxInput.Text = "";
            tbxInput.Focus();
        }

        private void btnDisconnect_Click(object sender, EventArgs e)
        {
            client.Disconnect();
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
            DisplayMessage("Client_MessageSendingFailed");
        }

        private void Client_MessageReceivingingFailed(object sender, MessageEventArgs e)
        {
            DisplayMessage("Client_MessageReceivingingFailed");
        }

        private void Client_ClientDisconnected(object sender, ConnectionEventArgs e)
        {
            DisplayMessage("Client_ClientDisconnected");
        }

        private void Client_ServerDisconnected(object sender, ConnectionEventArgs e)
        {
            DisplayMessage("Client_ServerDisconnected");
        }

        private void Client_MessageSent(object sender, MessageEventArgs e)
        {
            //DisplayMessage("(sent) " + e.Message.Text);
        }

        private void Client_MessageReceived(object sender, MessageEventArgs e)
        {
            if (e.Message.ControlInfo == ControlInfo.ClientList)
            {
                listClients.Items.Clear();

                // Update client list GUI.
                using (StringReader reader = new StringReader(e.Message.Text))
                {
                    string guid;
                    while ((guid = reader.ReadLine()) != null)
                    {
                        string name = reader.ReadLine();
                        listClients.Items.Add(new ListViewItem(new string[] { guid, name }));
                    }
                }

                return;
            }
            else if (e.Message.ControlInfo == ControlInfo.ChatroomList)
            {
                // TODO: update chatroom list
                listChatrooms.Items.Clear();

                // Update client list GUI.
                using (StringReader reader = new StringReader(e.Message.Text))
                {
                    string guid;
                    while ((guid = reader.ReadLine()) != null)
                    {
                        string name = reader.ReadLine();
                        string count = reader.ReadLine();
                        listChatrooms.Items.Add(new ListViewItem(new string[] { guid, name, count }));
                    }
                }

                return;
            }

            DisplayMessage(e.Message);
        }

        private void Client_ConnectionFailed(object sender, ConnectionEventArgs e)
        {
            DisplayMessage("Connection Failed :(");
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

            string[] entry = new string[3] { DateTime.Now.TimeOfDay.ToString(), "-", message };
            listMessages.Items.Add(new ListViewItem(entry));

            listMessages.SelectedIndices.Clear();
            listMessages.Items[listMessages.Items.Count - 1].Selected = true;
            //listMessages.Select();
        }

        private void DisplayMessage(ChatClassLibrary.Message message)
        {
            if (this.InvokeRequired)
            {   // Required for updating the GUI from different thread.
                this.Invoke((MethodInvoker) delegate { DisplayMessage(message); });
                return;
            }

            string sender = "-";
            if (message.IsSenderNull) sender = "<server>";
            else if (message.SenderId == client.ClientId) sender = "<me>";
            else sender = message.SenderId.ToString();

            string[] entry = new string[3]
            {
                message.TimeReceived.TimeOfDay.ToString(),
                sender,
                message.Text
            };
            listMessages.Items.Add(new ListViewItem(entry));

            listMessages.SelectedIndices.Clear();
            listMessages.Items[listMessages.Items.Count - 1].Selected = true;
            //listMessages.Select();
        }
    }
}
