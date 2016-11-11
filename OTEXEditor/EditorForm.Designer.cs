namespace OTEX
{
    partial class EditorForm
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
            this.btnServerExisting = new Marzersoft.Themes.ThemedButton();
            this.btnServerNew = new Marzersoft.Themes.ThemedButton();
            this.btnClient = new Marzersoft.Themes.ThemedButton();
            this.panMenu = new System.Windows.Forms.Panel();
            this.panMenuButtons = new System.Windows.Forms.Panel();
            this.btnServerTemporary = new Marzersoft.Themes.ThemedButton();
            this.lblTitle = new System.Windows.Forms.Label();
            this.panServerBrowserPage = new System.Windows.Forms.Panel();
            this.label1 = new System.Windows.Forms.Label();
            this.lblManualEntry = new System.Windows.Forms.Label();
            this.lblServerBrowser = new System.Windows.Forms.Label();
            this.dgvServers = new Marzersoft.Themes.ThemedDataGridView();
            this.colServerName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colTemporary = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.colServerAddress = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colServerPort = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colPassword = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.colUserCount = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colPing = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.label2 = new System.Windows.Forms.Label();
            this.tbClientPassword = new System.Windows.Forms.TextBox();
            this.btnClientCancel = new Marzersoft.Themes.ThemedButton();
            this.btnClientConnect = new Marzersoft.Themes.ThemedButton();
            this.tbClientAddress = new System.Windows.Forms.TextBox();
            this.dlgServerOpenExisting = new System.Windows.Forms.OpenFileDialog();
            this.dlgServerCreateNew = new System.Windows.Forms.SaveFileDialog();
            this.lblStatus = new System.Windows.Forms.Label();
            this.panServerPassword = new System.Windows.Forms.Panel();
            this.labServerPassword = new System.Windows.Forms.Label();
            this.tbServerPassword = new System.Windows.Forms.TextBox();
            this.panMenuPage = new System.Windows.Forms.Panel();
            this.lblAbout = new System.Windows.Forms.Label();
            this.lblVersion = new System.Windows.Forms.Label();
            this.panMenu.SuspendLayout();
            this.panMenuButtons.SuspendLayout();
            this.panServerBrowserPage.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvServers)).BeginInit();
            this.panServerPassword.SuspendLayout();
            this.panMenuPage.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnServerExisting
            // 
            this.btnServerExisting.FlatAppearance.BorderSize = 0;
            this.btnServerExisting.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnServerExisting.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnServerExisting.Location = new System.Drawing.Point(0, 53);
            this.btnServerExisting.Margin = new System.Windows.Forms.Padding(0, 2, 0, 2);
            this.btnServerExisting.Name = "btnServerExisting";
            this.btnServerExisting.Size = new System.Drawing.Size(267, 48);
            this.btnServerExisting.TabIndex = 1;
            this.btnServerExisting.Text = "Host an existing document";
            this.btnServerExisting.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnServerExisting.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnServerExisting.UseVisualStyleBackColor = true;
            this.btnServerExisting.Click += new System.EventHandler(this.btnServerExisting_Click);
            // 
            // btnServerNew
            // 
            this.btnServerNew.FlatAppearance.BorderSize = 0;
            this.btnServerNew.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnServerNew.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnServerNew.Location = new System.Drawing.Point(0, 0);
            this.btnServerNew.Margin = new System.Windows.Forms.Padding(0, 0, 0, 2);
            this.btnServerNew.Name = "btnServerNew";
            this.btnServerNew.Size = new System.Drawing.Size(267, 48);
            this.btnServerNew.TabIndex = 0;
            this.btnServerNew.Text = "Host a new document";
            this.btnServerNew.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnServerNew.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnServerNew.UseVisualStyleBackColor = true;
            this.btnServerNew.Click += new System.EventHandler(this.btnServerNew_Click);
            // 
            // btnClient
            // 
            this.btnClient.FlatAppearance.BorderSize = 0;
            this.btnClient.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnClient.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnClient.Location = new System.Drawing.Point(0, 159);
            this.btnClient.Margin = new System.Windows.Forms.Padding(0, 2, 0, 0);
            this.btnClient.Name = "btnClient";
            this.btnClient.Size = new System.Drawing.Size(267, 48);
            this.btnClient.TabIndex = 2;
            this.btnClient.Text = "Edit someone else\'s document";
            this.btnClient.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnClient.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnClient.UseVisualStyleBackColor = true;
            this.btnClient.Click += new System.EventHandler(this.btnClient_Click);
            // 
            // panMenu
            // 
            this.panMenu.Controls.Add(this.panMenuButtons);
            this.panMenu.Controls.Add(this.lblTitle);
            this.panMenu.Location = new System.Drawing.Point(152, 14);
            this.panMenu.Name = "panMenu";
            this.panMenu.Size = new System.Drawing.Size(279, 337);
            this.panMenu.TabIndex = 3;
            // 
            // panMenuButtons
            // 
            this.panMenuButtons.Controls.Add(this.btnServerTemporary);
            this.panMenuButtons.Controls.Add(this.btnServerNew);
            this.panMenuButtons.Controls.Add(this.btnServerExisting);
            this.panMenuButtons.Controls.Add(this.btnClient);
            this.panMenuButtons.Location = new System.Drawing.Point(6, 105);
            this.panMenuButtons.Margin = new System.Windows.Forms.Padding(2);
            this.panMenuButtons.Name = "panMenuButtons";
            this.panMenuButtons.Size = new System.Drawing.Size(267, 217);
            this.panMenuButtons.TabIndex = 7;
            // 
            // btnServerTemporary
            // 
            this.btnServerTemporary.FlatAppearance.BorderSize = 0;
            this.btnServerTemporary.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnServerTemporary.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnServerTemporary.Location = new System.Drawing.Point(0, 106);
            this.btnServerTemporary.Margin = new System.Windows.Forms.Padding(0, 2, 0, 0);
            this.btnServerTemporary.Name = "btnServerTemporary";
            this.btnServerTemporary.Size = new System.Drawing.Size(267, 48);
            this.btnServerTemporary.TabIndex = 3;
            this.btnServerTemporary.Text = "Host temporary document";
            this.btnServerTemporary.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnServerTemporary.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnServerTemporary.UseVisualStyleBackColor = true;
            this.btnServerTemporary.Click += new System.EventHandler(this.btnServerTemporary_Click);
            // 
            // lblTitle
            // 
            this.lblTitle.Location = new System.Drawing.Point(6, 4);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(267, 74);
            this.lblTitle.TabIndex = 3;
            this.lblTitle.Text = "OTEX Editor";
            this.lblTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // panServerBrowserPage
            // 
            this.panServerBrowserPage.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panServerBrowserPage.Controls.Add(this.label1);
            this.panServerBrowserPage.Controls.Add(this.lblManualEntry);
            this.panServerBrowserPage.Controls.Add(this.lblServerBrowser);
            this.panServerBrowserPage.Controls.Add(this.dgvServers);
            this.panServerBrowserPage.Controls.Add(this.label2);
            this.panServerBrowserPage.Controls.Add(this.tbClientPassword);
            this.panServerBrowserPage.Controls.Add(this.btnClientCancel);
            this.panServerBrowserPage.Controls.Add(this.btnClientConnect);
            this.panServerBrowserPage.Controls.Add(this.tbClientAddress);
            this.panServerBrowserPage.Location = new System.Drawing.Point(15, 22);
            this.panServerBrowserPage.Margin = new System.Windows.Forms.Padding(0);
            this.panServerBrowserPage.Name = "panServerBrowserPage";
            this.panServerBrowserPage.Size = new System.Drawing.Size(471, 391);
            this.panServerBrowserPage.TabIndex = 4;
            this.panServerBrowserPage.Visible = false;
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.Location = new System.Drawing.Point(137, 329);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(60, 20);
            this.label1.TabIndex = 108;
            this.label1.Text = "Address:";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblManualEntry
            // 
            this.lblManualEntry.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.lblManualEntry.Location = new System.Drawing.Point(131, 292);
            this.lblManualEntry.Margin = new System.Windows.Forms.Padding(0);
            this.lblManualEntry.Name = "lblManualEntry";
            this.lblManualEntry.Size = new System.Drawing.Size(327, 28);
            this.lblManualEntry.TabIndex = 107;
            this.lblManualEntry.Text = "Enter server manually";
            this.lblManualEntry.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
            // 
            // lblServerBrowser
            // 
            this.lblServerBrowser.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblServerBrowser.Location = new System.Drawing.Point(11, 6);
            this.lblServerBrowser.Name = "lblServerBrowser";
            this.lblServerBrowser.Size = new System.Drawing.Size(447, 28);
            this.lblServerBrowser.TabIndex = 106;
            this.lblServerBrowser.Text = "Public servers";
            this.lblServerBrowser.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
            // 
            // dgvServers
            // 
            this.dgvServers.AllowUserToAddRows = false;
            this.dgvServers.AllowUserToDeleteRows = false;
            this.dgvServers.AllowUserToResizeColumns = false;
            this.dgvServers.AllowUserToResizeRows = false;
            this.dgvServers.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dgvServers.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.dgvServers.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.None;
            this.dgvServers.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
            this.dgvServers.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.dgvServers.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colServerName,
            this.colTemporary,
            this.colServerAddress,
            this.colServerPort,
            this.colPassword,
            this.colUserCount,
            this.colPing});
            this.dgvServers.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
            this.dgvServers.Location = new System.Drawing.Point(11, 37);
            this.dgvServers.Margin = new System.Windows.Forms.Padding(0);
            this.dgvServers.MultiSelect = false;
            this.dgvServers.Name = "dgvServers";
            this.dgvServers.ReadOnly = true;
            this.dgvServers.RowHeadersVisible = false;
            this.dgvServers.RowHeadersWidthSizeMode = System.Windows.Forms.DataGridViewRowHeadersWidthSizeMode.DisableResizing;
            this.dgvServers.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.dgvServers.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvServers.ShowCellErrors = false;
            this.dgvServers.ShowCellToolTips = false;
            this.dgvServers.ShowEditingIcon = false;
            this.dgvServers.ShowRowErrors = false;
            this.dgvServers.Size = new System.Drawing.Size(447, 252);
            this.dgvServers.TabIndex = 2000;
            this.dgvServers.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvServers_CellContentDoubleClick);
            // 
            // colServerName
            // 
            this.colServerName.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.colServerName.HeaderText = "Name";
            this.colServerName.Name = "colServerName";
            this.colServerName.ReadOnly = true;
            this.colServerName.ToolTipText = "The name of this server.";
            // 
            // colTemporary
            // 
            this.colTemporary.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.ColumnHeader;
            this.colTemporary.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.colTemporary.HeaderText = "Temporary?";
            this.colTemporary.Name = "colTemporary";
            this.colTemporary.ReadOnly = true;
            this.colTemporary.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            this.colTemporary.ToolTipText = "Is the document hosted by this server a temporary one? Temporary documents are no" +
    "t backed by files, and will be lost when the server shuts down.";
            this.colTemporary.Width = 88;
            // 
            // colServerAddress
            // 
            this.colServerAddress.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.colServerAddress.HeaderText = "Address";
            this.colServerAddress.Name = "colServerAddress";
            this.colServerAddress.ReadOnly = true;
            this.colServerAddress.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            this.colServerAddress.ToolTipText = "The IP address of this server.";
            this.colServerAddress.Width = 70;
            // 
            // colServerPort
            // 
            this.colServerPort.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.ColumnHeader;
            this.colServerPort.HeaderText = "Port";
            this.colServerPort.Name = "colServerPort";
            this.colServerPort.ReadOnly = true;
            this.colServerPort.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            this.colServerPort.ToolTipText = "The port used by this server.";
            this.colServerPort.Width = 51;
            // 
            // colPassword
            // 
            this.colPassword.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.ColumnHeader;
            this.colPassword.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.colPassword.HeaderText = "Password?";
            this.colPassword.Name = "colPassword";
            this.colPassword.ReadOnly = true;
            this.colPassword.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            this.colPassword.ToolTipText = "Does this server require a password?";
            this.colPassword.Width = 84;
            // 
            // colUserCount
            // 
            this.colUserCount.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.ColumnHeader;
            this.colUserCount.HeaderText = "Users";
            this.colUserCount.Name = "colUserCount";
            this.colUserCount.ReadOnly = true;
            this.colUserCount.ToolTipText = "The number of users currently connected to this server.";
            this.colUserCount.Width = 59;
            // 
            // colPing
            // 
            this.colPing.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.ColumnHeader;
            this.colPing.HeaderText = "Ping";
            this.colPing.Name = "colPing";
            this.colPing.ReadOnly = true;
            this.colPing.ToolTipText = "The last recorded round-trip-time (RTT) to this server, in milliseconds.";
            this.colPing.Width = 53;
            // 
            // label2
            // 
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.label2.Location = new System.Drawing.Point(137, 357);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(60, 20);
            this.label2.TabIndex = 104;
            this.label2.Text = "Password:";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // tbClientPassword
            // 
            this.tbClientPassword.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.tbClientPassword.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.tbClientPassword.Location = new System.Drawing.Point(203, 357);
            this.tbClientPassword.MaxLength = 32;
            this.tbClientPassword.Name = "tbClientPassword";
            this.tbClientPassword.Size = new System.Drawing.Size(166, 20);
            this.tbClientPassword.TabIndex = 2002;
            this.tbClientPassword.UseSystemPasswordChar = true;
            // 
            // btnClientCancel
            // 
            this.btnClientCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnClientCancel.FlatAppearance.BorderSize = 0;
            this.btnClientCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnClientCancel.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnClientCancel.Location = new System.Drawing.Point(11, 331);
            this.btnClientCancel.Margin = new System.Windows.Forms.Padding(0);
            this.btnClientCancel.Name = "btnClientCancel";
            this.btnClientCancel.Size = new System.Drawing.Size(80, 48);
            this.btnClientCancel.TabIndex = 2004;
            this.btnClientCancel.Text = "Back";
            this.btnClientCancel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnClientCancel.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnClientCancel.UseVisualStyleBackColor = true;
            this.btnClientCancel.Click += new System.EventHandler(this.btnClientCancel_Click);
            // 
            // btnClientConnect
            // 
            this.btnClientConnect.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnClientConnect.FlatAppearance.BorderSize = 0;
            this.btnClientConnect.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnClientConnect.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnClientConnect.Location = new System.Drawing.Point(378, 331);
            this.btnClientConnect.Margin = new System.Windows.Forms.Padding(0);
            this.btnClientConnect.Name = "btnClientConnect";
            this.btnClientConnect.Size = new System.Drawing.Size(80, 48);
            this.btnClientConnect.TabIndex = 2003;
            this.btnClientConnect.Text = "Connect";
            this.btnClientConnect.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnClientConnect.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnClientConnect.UseVisualStyleBackColor = true;
            this.btnClientConnect.Click += new System.EventHandler(this.btnClientConnect_Click);
            // 
            // tbClientAddress
            // 
            this.tbClientAddress.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.tbClientAddress.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.tbClientAddress.Location = new System.Drawing.Point(203, 329);
            this.tbClientAddress.MaxLength = 256;
            this.tbClientAddress.Name = "tbClientAddress";
            this.tbClientAddress.Size = new System.Drawing.Size(166, 20);
            this.tbClientAddress.TabIndex = 2001;
            this.tbClientAddress.Text = "127.0.0.1";
            this.tbClientAddress.TextChanged += new System.EventHandler(this.tbClientAddress_TextChanged);
            this.tbClientAddress.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.tbClientAddress_KeyPress);
            // 
            // dlgServerOpenExisting
            // 
            this.dlgServerOpenExisting.Title = "Select an existing file to collaboratively edit";
            // 
            // dlgServerCreateNew
            // 
            this.dlgServerCreateNew.Title = "Select a new file to create collaboratively";
            // 
            // lblStatus
            // 
            this.lblStatus.Location = new System.Drawing.Point(176, 357);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(267, 28);
            this.lblStatus.TabIndex = 2;
            this.lblStatus.Text = "Connecting...";
            this.lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // panServerPassword
            // 
            this.panServerPassword.Controls.Add(this.labServerPassword);
            this.panServerPassword.Controls.Add(this.tbServerPassword);
            this.panServerPassword.Location = new System.Drawing.Point(642, 22);
            this.panServerPassword.Name = "panServerPassword";
            this.panServerPassword.Size = new System.Drawing.Size(204, 56);
            this.panServerPassword.TabIndex = 6;
            this.panServerPassword.Visible = false;
            // 
            // labServerPassword
            // 
            this.labServerPassword.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labServerPassword.Location = new System.Drawing.Point(10, 0);
            this.labServerPassword.Margin = new System.Windows.Forms.Padding(0);
            this.labServerPassword.Name = "labServerPassword";
            this.labServerPassword.Size = new System.Drawing.Size(185, 20);
            this.labServerPassword.TabIndex = 2005;
            this.labServerPassword.Text = "Enter password:";
            this.labServerPassword.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
            // 
            // tbServerPassword
            // 
            this.tbServerPassword.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbServerPassword.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.tbServerPassword.Location = new System.Drawing.Point(10, 25);
            this.tbServerPassword.MaxLength = 32;
            this.tbServerPassword.Name = "tbServerPassword";
            this.tbServerPassword.Size = new System.Drawing.Size(185, 20);
            this.tbServerPassword.TabIndex = 2006;
            this.tbServerPassword.UseSystemPasswordChar = true;
            this.tbServerPassword.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.tbServerPassword_KeyPress);
            // 
            // panMenuPage
            // 
            this.panMenuPage.Controls.Add(this.panMenu);
            this.panMenuPage.Controls.Add(this.lblAbout);
            this.panMenuPage.Controls.Add(this.lblStatus);
            this.panMenuPage.Controls.Add(this.lblVersion);
            this.panMenuPage.Location = new System.Drawing.Point(628, 121);
            this.panMenuPage.Name = "panMenuPage";
            this.panMenuPage.Size = new System.Drawing.Size(512, 345);
            this.panMenuPage.TabIndex = 5;
            // 
            // lblAbout
            // 
            this.lblAbout.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblAbout.Location = new System.Drawing.Point(0, 313);
            this.lblAbout.Margin = new System.Windows.Forms.Padding(0);
            this.lblAbout.Name = "lblAbout";
            this.lblAbout.Size = new System.Drawing.Size(100, 30);
            this.lblAbout.TabIndex = 0;
            this.lblAbout.TabStop = true;
            this.lblAbout.Text = "About";
            this.lblAbout.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblVersion
            // 
            this.lblVersion.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.lblVersion.Location = new System.Drawing.Point(410, 313);
            this.lblVersion.Margin = new System.Windows.Forms.Padding(0);
            this.lblVersion.Name = "lblVersion";
            this.lblVersion.Size = new System.Drawing.Size(100, 30);
            this.lblVersion.TabIndex = 4;
            this.lblVersion.Text = "label2";
            this.lblVersion.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // EditorForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1074, 478);
            this.Controls.Add(this.panMenuPage);
            this.Controls.Add(this.panServerPassword);
            this.Controls.Add(this.panServerBrowserPage);
            this.MinimumSize = new System.Drawing.Size(500, 400);
            this.Name = "EditorForm";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.Text = "OTEX Editor";
            this.TextFlourishes = false;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.EditorForm_FormClosing);
            this.panMenu.ResumeLayout(false);
            this.panMenuButtons.ResumeLayout(false);
            this.panServerBrowserPage.ResumeLayout(false);
            this.panServerBrowserPage.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvServers)).EndInit();
            this.panServerPassword.ResumeLayout(false);
            this.panServerPassword.PerformLayout();
            this.panMenuPage.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion
        private Marzersoft.Themes.ThemedButton btnServerNew;
        private Marzersoft.Themes.ThemedButton btnServerExisting;
        private Marzersoft.Themes.ThemedButton btnClient;
        private System.Windows.Forms.Panel panMenu;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Panel panServerBrowserPage;
        private System.Windows.Forms.TextBox tbClientAddress;
        private Marzersoft.Themes.ThemedButton btnClientCancel;
        private Marzersoft.Themes.ThemedButton btnClientConnect;
        private System.Windows.Forms.OpenFileDialog dlgServerOpenExisting;
        private System.Windows.Forms.SaveFileDialog dlgServerCreateNew;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Panel panMenuButtons;
        private System.Windows.Forms.Panel panMenuPage;
        private System.Windows.Forms.Label lblAbout;
        private System.Windows.Forms.Label lblVersion;
        private Marzersoft.Themes.ThemedButton btnServerTemporary;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox tbClientPassword;
        private Marzersoft.Themes.ThemedDataGridView dgvServers;
        private System.Windows.Forms.Label lblServerBrowser;
        private System.Windows.Forms.Label lblManualEntry;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.DataGridViewTextBoxColumn colServerName;
        private System.Windows.Forms.DataGridViewCheckBoxColumn colTemporary;
        private System.Windows.Forms.DataGridViewTextBoxColumn colServerAddress;
        private System.Windows.Forms.DataGridViewTextBoxColumn colServerPort;
        private System.Windows.Forms.DataGridViewCheckBoxColumn colPassword;
        private System.Windows.Forms.DataGridViewTextBoxColumn colUserCount;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPing;
        private System.Windows.Forms.Panel panServerPassword;
        private System.Windows.Forms.Label labServerPassword;
        private System.Windows.Forms.TextBox tbServerPassword;
    }
}

