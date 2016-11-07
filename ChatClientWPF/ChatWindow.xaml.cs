using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
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
using ChatClassLibrary.Protocols;

namespace ChatClientWPF
{
    public partial class ChatWindow : Window
    {
        /// <summary>
        /// The ID of the chatroom in case of group chat, or the ID of other
        /// client in case of private chat.
        /// </summary>
        public Guid ChatId { get; set; }

        private string chatName;

        public string ChatName
        {
            get { return chatName; }
            set
            {
                chatName = value;
                this.Title = "LoliChat - " + chatName
                    + (PrivateMode ? " [Private Chat]" : " [Group Chat]");
            }
        }

        public bool PrivateMode { get; set; }

        public MessageClient ClientService { get; set; }

        private ObservableCollection<MessageLine> ChatHistory { get; set; }

        public ChatWindow(Guid roomId, MessageClient clientService, bool privateChat = false)
        {
            InitializeComponent();

            this.ChatId = roomId;
            this.ClientService = clientService;
            this.PrivateMode = privateChat;

            this.ChatHistory = new ObservableCollection<MessageLine>();
            listView_Chat.ItemsSource = ChatHistory;

            this.imgUserProfile.Source = clientService != null && clientService.Connected ?
                    new BitmapImage(new Uri(this.ClientService.ProfileImagePath)) : null;

            lblFileDownload.Text = "";
            tbxFilePath.Text = "";

            tab_Files.Visibility = privateChat ? Visibility.Collapsed : Visibility.Visible;
            tab_More.Visibility = privateChat ? Visibility.Collapsed : Visibility.Visible;
        }

        public void LoadMemberList()
        {
            if (this.PrivateMode)
                return;

            try
            {
                listBox_Members.ItemsSource = null;
                listBox_Members.ItemsSource = ClientService.KnownChatrooms[this.ChatId].MembersInfo;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }
        }

        public void DisplayMessage(Message message)
        {
            if (string.IsNullOrEmpty(message.Text)) return;

            Guid senderId = message.SenderId;
            string senderName = "-";

            if (senderId == ProtocolSettings.NullId || message.Type == MessageType.Control)
                senderName = "<SERVER>";
            else if (senderId == ClientService.ClientId)
                senderName = "<YOU>";
            else
                senderName = ClientService.GetClientName(senderId);
            int senderImage = senderId.ToByteArray()[0];

            this.Dispatcher.Invoke(() =>
            {   // Called from different thread.
                ChatHistory.Add(new MessageLine(message.TimeSent, senderName, message.Text, senderImage));

                listView_Chat.SelectedIndex = ChatHistory.Count - 1;
                listView_Chat.ScrollIntoView(listView_Chat.SelectedItem);
            });
        }

        private void btnChatSend_Click(object sender, RoutedEventArgs e)
        {
            string messageText = tbxChatInput.Text;
            if (messageText.Length == 0)
                return;

            ClientService?.SendMessage(messageText, this.ChatId, this.PrivateMode);

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

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            this.Visibility = Visibility.Hidden;
        }

        private void btnLeaveChatroom_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult confirm = MessageBox.Show("Are you sure you want to leave this chatroom?\n"
                + "If the chatroom becomes empty, it will be removed from the server, along with the shared files.", "Leave Chatroom", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes)
                return;

            ClientService?.RequestLeaveChatroom(this.ChatId);
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //if (((TabControl) sender).SelectedIndex == 2)
            //{
            //    this.LoadMemberList();
            //}
        }

        private void File_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;

            this.tab_Files.IsSelected = true;
            var files = (string[]) e.Data.GetData(DataFormats.FileDrop);
            this.tbxFilePath.Text = files?[0];
        }

        private void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Choose a file to upload",
                Filter = "All Files (*.*)|*.*",
                CheckFileExists = true,
                InitialDirectory = @"D:\"
            };

            bool? result = dialog.ShowDialog();

            if (result == true)
            {
                this.tbxFilePath.Text = dialog.FileName;
            }
        }

        private bool _currentlyUploading = false;
        private Thread _ftpThread = null;
        private void _UpdateFileUploaderGUI(bool? uploadSuccess = null)
        {
            this.Dispatcher.Invoke(() =>
            {   // Called from different thread.
                tbxFilePath.IsEnabled = !_currentlyUploading;
                btnBrowse.IsEnabled = !_currentlyUploading;
                btnUpload.Content = _currentlyUploading ? "Cancel" : "Upload";
                if (uploadSuccess.HasValue)
                    pgbFileUpload.Value = uploadSuccess.Value ? pgbFileUpload.Maximum : 0;
            });
        }

        private void btnUpload_Click(object sender, RoutedEventArgs e)
        {
            ClientService.SendFileUploadRequest(this.ChatId);
        }

        public void BeginUpload(int receiverPort)
        {
            this.Dispatcher.Invoke(() =>
            {
                if (_currentlyUploading)
                {   // Cancel the upload.
                    this._ftpThread.Abort();
                    _currentlyUploading = false;
                    _UpdateFileUploaderGUI(false);
                    return;
                }

                if (!File.Exists(tbxFilePath.Text.Trim()))
                {
                    MessageBox.Show("Local file not found: " + tbxFilePath.Text.Trim(), "File Upload", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var progressReporter = new Progress<double>();
                progressReporter.ProgressChanged += (s, progress) =>
                {
                    pgbFileUpload.Value = (int) progress;
                };

                string filePath = tbxFilePath.Text.Trim();
                IPEndPoint serverEP = new IPEndPoint(
                    ClientService.ServerEndPoint.Address,
                    receiverPort);

                Thread ftpThread = new Thread(() =>
                {
                    _currentlyUploading = true;
                    _UpdateFileUploaderGUI();

                    string log;
                    bool success = FileProtocol.SendFile(filePath, serverEP,
                        ClientService.ClientId, this.ChatId, out log, progressReporter);

                    _currentlyUploading = false;
                    _UpdateFileUploaderGUI(success);

                    if (success)
                        MessageBox.Show("The file has finished uploading.", "File Upload", MessageBoxButton.OK, MessageBoxImage.Information);
                    else
                        MessageBox.Show(log, "File Upload", MessageBoxButton.OK, MessageBoxImage.Error);
                });
                this._ftpThread = ftpThread;
                ftpThread.Name = "FTP Upload Thread";
                ftpThread.Start();
            });
        }

        private ObservableCollection<FileListLine> _fileList = new ObservableCollection<FileListLine>();

        public void UpdateFileList(List<FileListLine> fileList)
        {
            this.Dispatcher.Invoke(() =>
            {   // Called from different thread.
                listView_Files.ItemsSource = null;
                _fileList.Clear();
                foreach (var file in fileList)
                    _fileList.Add(file);
                listView_Files.ItemsSource = _fileList;
            });
        }

        public void AddFileToList(FileListLine file)
        {
            this.Dispatcher.Invoke(() =>
            {   // Called from different thread.
                _fileList.Add(file);
                if (listView_Files.ItemsSource != _fileList)
                    listView_Files.ItemsSource = _fileList;
            });
        }

        private class MessageLine
        {
            public DateTime Time { get; set; }
            public string Sender { get; set; }
            public string MessageText { get; set; }
            public MessageLine(DateTime time, string sender, string text, int profileImage = 0)
            {
                this.Time = time;
                this.Sender = sender;
                this.MessageText = text;
                this.SenderImage = profileImage;
            }

            public int SenderImage { get; set; }
            public string SenderImagePath
                => System.IO.Path.GetFullPath(string.Format(".\\resource_images\\profile_{0:00}.png", this.SenderImage));
            public string MessageColor
                => Sender == "<SERVER>" ? "#CB000000"
                   : Sender == "<YOU>" ? "#FF109020" : "#FF102090";
            //public string BackColor
            //    => Sender == "<SERVER>" ? "#FFDEDEDE" : "Transparent";
        }

        private void btnDownloadFile_Click(object sender, RoutedEventArgs e)
        {
            string fileName = ((FileListLine) listView_Files.SelectedItem).FileName;
            string savePath;

            var dialog = new Microsoft.Win32.SaveFileDialog();
            dialog.Title = "Choose where to save the downloaded file";
            dialog.FileName = fileName;
            dialog.Filter = "All Files (*.*)|*.*";
            dialog.CheckFileExists = false;
            dialog.InitialDirectory = @"D:\";

            bool? result = dialog.ShowDialog();

            if (result == true)
            {
                savePath = dialog.FileName;
                if (File.Exists(savePath))
                    File.Delete(savePath);
            }
            else
            {   // User canceled.
                return;
            }

            IPAddress addr = Utility.GetIPv4Address();
//            IPAddress addr = IPAddress.Any;  // CANNOT USE THIS
            int port = Utility.FreeTcpPort();
            TcpListener ftpSocket = new TcpListener(addr, port);

            var progressReporter = new Progress<double>();
            progressReporter.ProgressChanged += (s, progress) =>
            {
                pgbFileDownload.Value = (int) progress;
            };

            Thread ftpThread = new Thread(() =>
            {
                Console.WriteLine("Started FTP Download Listener Thread");
                ftpSocket.Start();

//                while (true)
//                {
                Guid senderId;
                Guid targetId;
                string fileInfo; // Name, size, time, and hash.
                bool received;
                try
                {
                    received = FileProtocol.ReceiveFile(ftpSocket,
                        out senderId, out targetId, out fileInfo, savePath, progressReporter);
                }
                catch
                {
                    received = false;
                    fileInfo = "";
                    senderId = Guid.Empty;
                    targetId = Guid.Empty;
                }

                if (received)
                {
                    Console.WriteLine("File Transfer Finished.");
                    this.Dispatcher.Invoke(() =>
                    {
                        pgbFileDownload.Value = pgbFileDownload.Maximum;
                        lblFileDownload.Text = "Download finished: " + savePath;
                        MessageBox.Show("Download finished.", "File Download",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    });
//                        break;
                }
                else
                {
                    MessageBox.Show("Error occured while downloading. Please try again.", "File Download",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                }
//                }
                ftpSocket.Stop();
            });
            ftpThread.Name = "FTP Download Listener Thread";
            ftpThread.Start();

            StringBuilder sb = new StringBuilder();
            sb.Append(addr.ToString()).AppendLine();
            sb.Append(port).AppendLine();
            sb.Append(fileName);

            Message fileRequest = new Message
            {
                Type = MessageType.Control,
                ControlInfo = ControlInfo.RequestFileDownload,
                SenderId = ClientService.ClientId,
                TargetId = this.ChatId,
                Text = sb.ToString()
            };
            ChatProtocol.SendMessage(fileRequest, ClientService.NetworkStream);
        }

        private void btnRemoveFile_Click(object sender, RoutedEventArgs e)
        {
            string fileName = ((FileListLine) listView_Files.SelectedItem).FileName;
            MessageBoxResult confirm = MessageBox.Show("Request the server to remove this file?\n    " + fileName, "File Sharing", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes)
                return;

            Message removeRequest = new Message
            {
                Type = MessageType.Control,
                ControlInfo = ControlInfo.RequestFileRemove,
                SenderId = ClientService.ClientId,
                TargetId = this.ChatId,
                Text = fileName
            };
            ChatProtocol.SendMessage(removeRequest, ClientService.NetworkStream);
        }
    }

    public class FileListLine
    {
        public string FileName { get; set; }
        public long FileSize { get; set; }
        public long FileSizeKB { get; set; }
        public DateTime TimeUploaded { get; set; }
        public string UploaderName { get; set; }

        public FileListLine(string fileName, long fileSize, DateTime timeUploaded, string uploaderName)
        {
            this.FileName = fileName;
            this.FileSize = fileSize;
            this.FileSizeKB = (long) Math.Ceiling((double) fileSize / 1024);
            this.TimeUploaded = timeUploaded;
            this.UploaderName = uploaderName;
        }
    }
}
