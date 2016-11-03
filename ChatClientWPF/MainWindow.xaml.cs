﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using ChatClassLibrary;

namespace ChatClientWPF
{
    public partial class MainWindow : Window
    {
        public MessageClient ClientService { get; set; }

        private Dictionary<Guid, ChatWindow> ChatWindows { get; set; }

        public MainWindow()
        {
            InitializeComponent();

            this.ChatWindows = new Dictionary<Guid, ChatWindow>();

            _UpdateGUI();
        }

        private void _Connect()
        {
            if (ClientService != null && ClientService.Connected)
            {   // Already connected.
                return;
            }

            IPAddress serverIP;
            if (IPAddress.TryParse(tbxServerAddress.Text.Trim(), out serverIP))
            {
                ClientService = new MessageClient(serverIP);
                ClientService.DisplayName = tbxUsername.Text.Trim();

                // Initialize event handlers.
                ClientService.ConnectionEstablished += ClientService_ConnectionEstablished;
                ClientService.ConnectionFailed += ClientService_ConnectionFailed;
                ClientService.ServerDisconnected += ClientService_ServerDisconnected;
                ClientService.ClientDisconnected += ClientService_ClientDisconnected;

                ClientService.MessageReceived += ClientService_MessageReceived;
                ClientService.MessageSent += ClientService_MessageSent;
                ClientService.MessageReceivingingFailed += ClientService_MessageReceivingingFailed;
                ClientService.MessageSendingFailed += ClientService_MessageSendingFailed;

                ClientService.KnownChatroomsUpdated += ClientService_KnownChatroomsUpdated;
                ClientService.KnownClientsUpdated += ClientService_KnownClientsUpdated;

                ClientService.ClientJoinedChatroom += ClientService_ClientJoinedChatroom;
                ClientService.ClientLeftChatroom += ClientService_ClientLeftChatroom;

                ClientService.ConnectToServer();
            }
            else
            {   // Invalid IP address.
                ClientService_ConnectionFailed(this, null);
            }
        }

        private void ClientService_ClientLeftChatroom(object sender, ChatroomEventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {   // Called from different thread.
                if (ChatWindows.ContainsKey(e.ChatroomId))
                {
                    ChatWindows[e.ChatroomId].Close();
                    ChatWindows.Remove(e.ChatroomId);
                }
            });
        }

        private void ClientService_ClientJoinedChatroom(object sender, ChatroomEventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {   // Called from different thread.
                if (!ChatWindows.ContainsKey(e.ChatroomId))
                {
                    ChatWindow newWindow = new ChatWindow(e.ChatroomId, this.ClientService);
                    newWindow.ChatroomName = e.ChatroomName;
                    ChatWindows.Add(e.ChatroomId, newWindow);
                    newWindow.Show();
                }

                _DisplayMessage(new Message
                {
                    Type = MessageType.Control,
                    SenderId = Message.NullID,
                    TargetId = e.ChatroomId,
                    TimeSent = DateTime.Now,
                    Text = "You are now a member of this chatroom."
                });
            });
        }

        private void ClientService_KnownClientsUpdated(object sender, EventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {   // Called from different thread.
                listBox_Users.ItemsSource = null;
                listBox_Users.ItemsSource = ClientService.KnownClients;

                //foreach (var roomId in ClientService.JoinedChatrooms)
                //    ChatWindows[roomId].ChatroomName = ClientService.GetChatroomName(roomId);
            });
        }

        private void ClientService_KnownChatroomsUpdated(object sender, EventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {   // Called from different thread.
                listBox_Chatrooms.ItemsSource = null;
                listBox_Chatrooms.ItemsSource = ClientService.KnownChatrooms;

                foreach (var roomId in ClientService.JoinedChatrooms)
                    ChatWindows[roomId].ChatroomName = ClientService.GetChatroomName(roomId);
            });
        }

        private void ClientService_MessageSendingFailed(object sender, MessageEventArgs e)
        {
            return;
            throw new NotImplementedException();
        }

        private void ClientService_MessageReceivingingFailed(object sender, MessageEventArgs e)
        {
            return;
            throw new NotImplementedException();
        }

        private void ClientService_MessageSent(object sender, MessageEventArgs e)
        {
            return;
            throw new NotImplementedException();
        }

        private void ClientService_MessageReceived(object sender, MessageEventArgs e)
        {
            _DisplayMessage(e.Message);
        }

        private void ClientService_ClientDisconnected(object sender, ConnectionEventArgs e)
        {
            _UpdateGUI();
        }

        private void ClientService_ServerDisconnected(object sender, ConnectionEventArgs e)
        {
            _UpdateGUI(true);
        }

        private void ClientService_ConnectionFailed(object sender, ConnectionEventArgs e)
        {
            _UpdateGUI(true);
        }

        private void ClientService_ConnectionEstablished(object sender, ConnectionEventArgs e)
        {
            ClientService.BeginReceive();
            _UpdateGUI(true);
        }

        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            _Connect();
        }

        private void btnDisconnect_Click(object sender, RoutedEventArgs e)
        {
            ClientService.Disconnect();
        }

        private void _DisplayMessage(Message message)
        {   // Must display in the correct chatroom.
            if (ChatWindows.ContainsKey(message.TargetId))
                ChatWindows[message.TargetId].DisplayMessage(message);
            else
                Console.WriteLine(message.ToString());
        }

        private void _UpdateGUI(bool showConnectionLabel = false)
        {
            this.Dispatcher.Invoke(() =>
            {
                bool connected = ClientService?.Connected ?? false;

                lblConnectionStatus1.Visibility = showConnectionLabel && connected
                    ? Visibility.Visible : Visibility.Collapsed;
                lblConnectionStatus2.Visibility = showConnectionLabel && !connected
                    ? Visibility.Visible : Visibility.Collapsed;

                btnDisconnect.Visibility = connected ? Visibility.Visible : Visibility.Collapsed;

                tbxServerAddress.IsEnabled = !connected;
                tbxUsername.IsEnabled = !connected;
                btnConnect.IsEnabled = !connected;

                tab_Users.IsEnabled = connected;
                tab_Chatrooms.IsEnabled = connected;

                if (!connected)
                    tab_Connect.IsSelected = true;
            });
        }

        private void listBox_Users_ItemMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount >= 2)
            {
                MessageBox.Show(listBox_Users.SelectedItem.ToString());
            }
        }

        private void listBox_Chatrooms_ItemMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount >= 2)
            {
                //MessageBox.Show(listBox_Chatrooms.SelectedItem.ToString());

                var roomInfo = (MessageClient.ChatroomInfo) listBox_Chatrooms.SelectedItem;

                if (ChatWindows.ContainsKey(roomInfo.ChatroomId))
                {
                    ChatWindows[roomInfo.ChatroomId].Visibility = Visibility.Visible;
                    ChatWindows[roomInfo.ChatroomId].Activate();
                }
                else 
                    ClientService.RequestJoinChatroom(roomInfo.ChatroomId);
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void tbxUsername_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                btnConnect_Click(sender, e);
                e.Handled = true;
            }
        }
    }
}
