using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using ChatClassLibrary;

namespace ChatClientWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class ChatWindow : Window
    {
        public Guid ChatroomId { get; set; }

        private string chatroomName;

        public string ChatroomName
        {
            get { return chatroomName; }
            set
            {
                chatroomName = value;
                this.Title = "Chat# - " + chatroomName;
            }
        }


        public MessageClient ClientService { get; set; }

        private ObservableCollection<MessageLine> ChatHistory { get; set; }

        public ChatWindow(Guid roomId, MessageClient clientService)
        {
            InitializeComponent();

            this.ChatroomId = roomId;
            this.ClientService = clientService;

            this.ChatHistory = new ObservableCollection<MessageLine>();
            listView_Chat.ItemsSource = ChatHistory;

            //List<MessageLine> chatHistory = new List<MessageLine>()
            //{
            //    new MessageLine(DateTime.Now, "Dog", "Hello I'm a dog."),
            //    new MessageLine(DateTime.Now, "Cat", "Hello I'm a cat."),
            //    new MessageLine(DateTime.Now, "-", "<test message>")
            //};
            //listView_Chat.ItemsSource = chatHistory;
        }


        private Dictionary<Guid, string> clientNamesCache = new Dictionary<Guid, string>();
        public void DisplayMessage(Message message)
        {
            Guid senderId = message.SenderId;
            string senderName = "-";

            if (senderId == Message.NullID)
                senderName = "<SERVER>";
            else if (senderId == ClientService.ClientId)
                senderName = "<YOU>";
            else if (clientNamesCache.ContainsKey(senderId))
                senderName = clientNamesCache[senderId];
            else
            {
                foreach (var client in ClientService.KnownClients)
                {
                    if (senderId != client.ClientId)
                        continue;
                    senderName = client.DisplayName;
                    clientNamesCache.Add(senderId, senderName);
                    break;
                }
            }

            this.Dispatcher.Invoke(() =>
            {   // Called from different thread.
                ChatHistory.Add(new MessageLine(message.TimeSent, senderName, message.Text));
            });
        }

        private class MessageLine
        {
            public DateTime Time { get; set; }
            public string Sender { get; set; }
            public string MessageText { get; set; }
            public MessageLine(DateTime time, string sender, string text)
            {
                this.Time = time;
                this.Sender = sender;
                this.MessageText = text;
            }
        }

        private void btnChatSend_Click(object sender, RoutedEventArgs e)
        {
            string messageText = tbxChatInput.Text.Trim();
            ClientService?.SendMessage(messageText, this.ChatroomId);

            tbxChatInput.Text = "";
            tbxChatInput.Focus();
        }

        private void tbxChatInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                btnChatSend_Click(sender, e);
                e.Handled = true;
            }
        }
    }
}
