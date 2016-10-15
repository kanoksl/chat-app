namespace ChatClient
{
    partial class ChatWindow
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.listChat = new System.Windows.Forms.ListBox();
            this.btnConnect = new System.Windows.Forms.Button();
            this.btnSend = new System.Windows.Forms.Button();
            this.tbxUsername = new System.Windows.Forms.TextBox();
            this.tbxMessage = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.tbxServerAddress = new System.Windows.Forms.TextBox();
            this.btnDisconnect = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.tbxFilePath = new System.Windows.Forms.TextBox();
            this.btnUploadFile = new System.Windows.Forms.Button();
            this.btnFileBrowse = new System.Windows.Forms.Button();
            this.pgbUploadProgress = new System.Windows.Forms.ProgressBar();
            this.SuspendLayout();
            // 
            // listChat
            // 
            this.listChat.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.listChat.FormattingEnabled = true;
            this.listChat.Location = new System.Drawing.Point(12, 77);
            this.listChat.Name = "listChat";
            this.listChat.Size = new System.Drawing.Size(440, 407);
            this.listChat.TabIndex = 0;
            // 
            // btnConnect
            // 
            this.btnConnect.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnConnect.Location = new System.Drawing.Point(377, 12);
            this.btnConnect.Name = "btnConnect";
            this.btnConnect.Size = new System.Drawing.Size(75, 24);
            this.btnConnect.TabIndex = 1;
            this.btnConnect.Text = "Connect";
            this.btnConnect.UseVisualStyleBackColor = true;
            this.btnConnect.Click += new System.EventHandler(this.btnConnect_Click);
            // 
            // btnSend
            // 
            this.btnSend.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSend.Location = new System.Drawing.Point(377, 490);
            this.btnSend.Name = "btnSend";
            this.btnSend.Size = new System.Drawing.Size(75, 24);
            this.btnSend.TabIndex = 2;
            this.btnSend.Text = "Send";
            this.btnSend.UseVisualStyleBackColor = true;
            this.btnSend.Click += new System.EventHandler(this.btnSend_Click);
            // 
            // tbxUsername
            // 
            this.tbxUsername.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.tbxUsername.Location = new System.Drawing.Point(252, 13);
            this.tbxUsername.Name = "tbxUsername";
            this.tbxUsername.Size = new System.Drawing.Size(119, 22);
            this.tbxUsername.TabIndex = 3;
            // 
            // tbxMessage
            // 
            this.tbxMessage.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbxMessage.Location = new System.Drawing.Point(12, 492);
            this.tbxMessage.Name = "tbxMessage";
            this.tbxMessage.Size = new System.Drawing.Size(359, 22);
            this.tbxMessage.TabIndex = 4;
            this.tbxMessage.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.tbxMessage_KeyPress);
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(185, 18);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(61, 13);
            this.label1.TabIndex = 5;
            this.label1.Text = "Username:";
            // 
            // tbxServerAddress
            // 
            this.tbxServerAddress.Location = new System.Drawing.Point(252, 43);
            this.tbxServerAddress.Name = "tbxServerAddress";
            this.tbxServerAddress.Size = new System.Drawing.Size(119, 22);
            this.tbxServerAddress.TabIndex = 6;
            // 
            // btnDisconnect
            // 
            this.btnDisconnect.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnDisconnect.Location = new System.Drawing.Point(377, 42);
            this.btnDisconnect.Name = "btnDisconnect";
            this.btnDisconnect.Size = new System.Drawing.Size(75, 24);
            this.btnDisconnect.TabIndex = 7;
            this.btnDisconnect.Text = "Disconnect";
            this.btnDisconnect.UseVisualStyleBackColor = true;
            this.btnDisconnect.Click += new System.EventHandler(this.btnDisconnect_Click);
            // 
            // label2
            // 
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(205, 48);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(41, 13);
            this.label2.TabIndex = 8;
            this.label2.Text = "Server:";
            // 
            // tbxFilePath
            // 
            this.tbxFilePath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbxFilePath.Location = new System.Drawing.Point(12, 548);
            this.tbxFilePath.Name = "tbxFilePath";
            this.tbxFilePath.Size = new System.Drawing.Size(312, 22);
            this.tbxFilePath.TabIndex = 10;
            // 
            // btnUploadFile
            // 
            this.btnUploadFile.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnUploadFile.Location = new System.Drawing.Point(377, 546);
            this.btnUploadFile.Name = "btnUploadFile";
            this.btnUploadFile.Size = new System.Drawing.Size(75, 24);
            this.btnUploadFile.TabIndex = 9;
            this.btnUploadFile.Text = "Upload File";
            this.btnUploadFile.UseVisualStyleBackColor = true;
            this.btnUploadFile.Click += new System.EventHandler(this.btnUploadFile_Click);
            // 
            // btnFileBrowse
            // 
            this.btnFileBrowse.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnFileBrowse.Location = new System.Drawing.Point(330, 546);
            this.btnFileBrowse.Name = "btnFileBrowse";
            this.btnFileBrowse.Size = new System.Drawing.Size(41, 24);
            this.btnFileBrowse.TabIndex = 11;
            this.btnFileBrowse.Text = "...";
            this.btnFileBrowse.UseVisualStyleBackColor = true;
            this.btnFileBrowse.Click += new System.EventHandler(this.btnFileBrowse_Click);
            // 
            // pgbUploadProgress
            // 
            this.pgbUploadProgress.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pgbUploadProgress.Location = new System.Drawing.Point(12, 576);
            this.pgbUploadProgress.Name = "pgbUploadProgress";
            this.pgbUploadProgress.Size = new System.Drawing.Size(440, 13);
            this.pgbUploadProgress.TabIndex = 12;
            // 
            // ChatWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(464, 601);
            this.Controls.Add(this.pgbUploadProgress);
            this.Controls.Add(this.btnFileBrowse);
            this.Controls.Add(this.tbxFilePath);
            this.Controls.Add(this.btnUploadFile);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.btnDisconnect);
            this.Controls.Add(this.tbxServerAddress);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.tbxMessage);
            this.Controls.Add(this.tbxUsername);
            this.Controls.Add(this.btnSend);
            this.Controls.Add(this.btnConnect);
            this.Controls.Add(this.listChat);
            this.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Name = "ChatWindow";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Chat";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListBox listChat;
        private System.Windows.Forms.Button btnConnect;
        private System.Windows.Forms.Button btnSend;
        private System.Windows.Forms.TextBox tbxUsername;
        private System.Windows.Forms.TextBox tbxMessage;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox tbxServerAddress;
        private System.Windows.Forms.Button btnDisconnect;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox tbxFilePath;
        private System.Windows.Forms.Button btnUploadFile;
        private System.Windows.Forms.Button btnFileBrowse;
        private System.Windows.Forms.ProgressBar pgbUploadProgress;
    }
}

