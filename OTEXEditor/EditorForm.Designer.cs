namespace OTEX.Editor
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
            this.components = new System.ComponentModel.Container();
            this.btnHost = new Marzersoft.Themes.ThemedRadioButton();
            this.btnJoin = new Marzersoft.Themes.ThemedRadioButton();
            this.panJoin = new System.Windows.Forms.Panel();
            this.label1 = new System.Windows.Forms.Label();
            this.lblJoinManual = new Marzersoft.Themes.ThemedLabel();
            this.lblJoinPublic = new Marzersoft.Themes.ThemedLabel();
            this.dgvServers = new Marzersoft.Themes.ThemedDataGridView();
            this.colServerName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colServerAddress = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colServerPort = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colPassword = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.colUserCount = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colPing = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.label2 = new System.Windows.Forms.Label();
            this.tbClientPassword = new Marzersoft.Themes.ThemedTextBox();
            this.btnClientConnect = new Marzersoft.Themes.ThemedButton();
            this.tbClientAddress = new Marzersoft.Controls.IPEndPointTextBox();
            this.dlgHostExisting = new System.Windows.Forms.OpenFileDialog();
            this.dlgHostNew = new System.Windows.Forms.SaveFileDialog();
            this.lblConnectingStatus = new Marzersoft.Themes.ThemedLabel();
            this.panServerPassword = new System.Windows.Forms.Panel();
            this.labServerPassword = new Marzersoft.Themes.ThemedLabel();
            this.tbServerPassword = new Marzersoft.Themes.ThemedTextBox();
            this.panHost = new System.Windows.Forms.Panel();
            this.btnHostDelete = new Marzersoft.Themes.ThemedButton();
            this.btnHostTemporary = new Marzersoft.Themes.ThemedButton();
            this.btnHostExisting = new Marzersoft.Themes.ThemedButton();
            this.btnHostNew = new Marzersoft.Themes.ThemedButton();
            this.lbDocuments = new Marzersoft.Themes.ThemedListBox();
            this.lblHostSettings = new Marzersoft.Themes.ThemedLabel();
            this.btnHostStart = new Marzersoft.Themes.ThemedButton();
            this.lblHostDocuments = new Marzersoft.Themes.ThemedLabel();
            this.lblAbout = new System.Windows.Forms.Label();
            this.lblDebug = new System.Windows.Forms.Label();
            this.panConnectingPage = new System.Windows.Forms.Panel();
            this.panConnectingContent = new System.Windows.Forms.Panel();
            this.btnConnectingReconnect = new Marzersoft.Themes.ThemedButton();
            this.btnConnectingBack = new Marzersoft.Themes.ThemedButton();
            this.panSettings = new System.Windows.Forms.Panel();
            this.label6 = new System.Windows.Forms.Label();
            this.cbTheme = new Marzersoft.Themes.ThemedComboBox();
            this.nudLineLength = new Marzersoft.Themes.ThemedNumericUpDown();
            this.label5 = new System.Windows.Forms.Label();
            this.cbLineLength = new System.Windows.Forms.CheckBox();
            this.label4 = new System.Windows.Forms.Label();
            this.cbClientColour = new Marzersoft.Controls.ColourComboBox();
            this.nudClientUpdateInterval = new Marzersoft.Themes.ThemedNumericUpDown();
            this.label3 = new System.Windows.Forms.Label();
            this.splitter = new Marzersoft.Themes.ThemedSplitContainer();
            this.sideSplitter = new Marzersoft.Themes.ThemedSplitContainer();
            this.lblSideBar = new Marzersoft.Themes.ThemedLabel();
            this.cmUsers = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.showSelectionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.adminSeparator = new System.Windows.Forms.ToolStripSeparator();
            this.adminToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.readonlyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.kickToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.banToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.panEditors = new Marzersoft.Themes.ThemedPanel();
            this.menuSplitter = new Marzersoft.Themes.ThemedSplitContainer();
            this.toolTips = new System.Windows.Forms.ToolTip(this.components);
            this.tbHostTempName = new Marzersoft.Themes.ThemedTextBox();
            this.panHostTempName = new System.Windows.Forms.Panel();
            this.lblHostTempName = new Marzersoft.Themes.ThemedLabel();
            this.flpUsers = new OTEX.Editor.UserList();
            this.panJoin.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvServers)).BeginInit();
            this.panServerPassword.SuspendLayout();
            this.panHost.SuspendLayout();
            this.panConnectingPage.SuspendLayout();
            this.panConnectingContent.SuspendLayout();
            this.panSettings.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudLineLength)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudClientUpdateInterval)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.splitter)).BeginInit();
            this.splitter.Panel2.SuspendLayout();
            this.splitter.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.sideSplitter)).BeginInit();
            this.sideSplitter.Panel1.SuspendLayout();
            this.sideSplitter.SuspendLayout();
            this.cmUsers.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.menuSplitter)).BeginInit();
            this.menuSplitter.Panel1.SuspendLayout();
            this.menuSplitter.SuspendLayout();
            this.panHostTempName.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnHost
            // 
            this.btnHost.Accent = ((uint)(1u));
            this.btnHost.Appearance = System.Windows.Forms.Appearance.Button;
            this.btnHost.BackColor = System.Drawing.SystemColors.Control;
            this.btnHost.Checked = true;
            this.btnHost.FlatAppearance.BorderSize = 0;
            this.btnHost.FlatAppearance.MouseDownBackColor = System.Drawing.SystemColors.Highlight;
            this.btnHost.FlatAppearance.MouseOverBackColor = System.Drawing.SystemColors.ControlLight;
            this.btnHost.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnHost.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnHost.FontSize = 1;
            this.btnHost.ForeColor = System.Drawing.SystemColors.ControlText;
            this.btnHost.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnHost.Location = new System.Drawing.Point(10, 10);
            this.btnHost.Margin = new System.Windows.Forms.Padding(10, 10, 0, 10);
            this.btnHost.Name = "btnHost";
            this.btnHost.Size = new System.Drawing.Size(132, 40);
            this.btnHost.TabIndex = 0;
            this.btnHost.TabStop = true;
            this.btnHost.Text = "Host";
            this.btnHost.TextImageRelation = System.Windows.Forms.TextImageRelation.TextBeforeImage;
            this.toolTips.SetToolTip(this.btnHost, "Host an OTEX session using documents on your local computer.");
            this.btnHost.UseVisualStyleBackColor = false;
            // 
            // btnJoin
            // 
            this.btnJoin.Accent = ((uint)(2u));
            this.btnJoin.Appearance = System.Windows.Forms.Appearance.Button;
            this.btnJoin.FlatAppearance.BorderSize = 0;
            this.btnJoin.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnJoin.FontSize = 1;
            this.btnJoin.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnJoin.Location = new System.Drawing.Point(152, 10);
            this.btnJoin.Margin = new System.Windows.Forms.Padding(10, 10, 0, 10);
            this.btnJoin.Name = "btnJoin";
            this.btnJoin.Size = new System.Drawing.Size(128, 40);
            this.btnJoin.TabIndex = 2;
            this.btnJoin.Text = "Join";
            this.btnJoin.TextImageRelation = System.Windows.Forms.TextImageRelation.TextBeforeImage;
            this.toolTips.SetToolTip(this.btnJoin, "Join an existing OTEX session hosted elsewhere,");
            this.btnJoin.UseVisualStyleBackColor = true;
            // 
            // panJoin
            // 
            this.panJoin.Controls.Add(this.label1);
            this.panJoin.Controls.Add(this.lblJoinManual);
            this.panJoin.Controls.Add(this.lblJoinPublic);
            this.panJoin.Controls.Add(this.dgvServers);
            this.panJoin.Controls.Add(this.label2);
            this.panJoin.Controls.Add(this.tbClientPassword);
            this.panJoin.Controls.Add(this.btnClientConnect);
            this.panJoin.Controls.Add(this.tbClientAddress);
            this.panJoin.Location = new System.Drawing.Point(557, 207);
            this.panJoin.Margin = new System.Windows.Forms.Padding(0);
            this.panJoin.Name = "panJoin";
            this.panJoin.Padding = new System.Windows.Forms.Padding(10);
            this.panJoin.Size = new System.Drawing.Size(380, 188);
            this.panJoin.TabIndex = 4;
            this.panJoin.Visible = false;
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.Location = new System.Drawing.Point(15, 127);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(81, 20);
            this.label1.TabIndex = 108;
            this.label1.Text = "Address:";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblJoinManual
            // 
            this.lblJoinManual.Accent = ((uint)(0u));
            this.lblJoinManual.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.lblJoinManual.FontSize = 1;
            this.lblJoinManual.Location = new System.Drawing.Point(30, 90);
            this.lblJoinManual.Margin = new System.Windows.Forms.Padding(0);
            this.lblJoinManual.Name = "lblJoinManual";
            this.lblJoinManual.Size = new System.Drawing.Size(340, 28);
            this.lblJoinManual.TabIndex = 107;
            this.lblJoinManual.Text = "Enter session address manually";
            this.lblJoinManual.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
            // 
            // lblJoinPublic
            // 
            this.lblJoinPublic.Accent = ((uint)(0u));
            this.lblJoinPublic.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblJoinPublic.FontSize = 1;
            this.lblJoinPublic.Location = new System.Drawing.Point(10, 10);
            this.lblJoinPublic.Margin = new System.Windows.Forms.Padding(0);
            this.lblJoinPublic.Name = "lblJoinPublic";
            this.lblJoinPublic.Size = new System.Drawing.Size(360, 28);
            this.lblJoinPublic.TabIndex = 106;
            this.lblJoinPublic.Text = "Public sessions";
            this.lblJoinPublic.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
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
            this.colServerAddress,
            this.colServerPort,
            this.colPassword,
            this.colUserCount,
            this.colPing});
            this.dgvServers.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
            this.dgvServers.Location = new System.Drawing.Point(10, 47);
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
            this.dgvServers.Size = new System.Drawing.Size(360, 29);
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
            this.label2.Location = new System.Drawing.Point(15, 155);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(81, 20);
            this.label2.TabIndex = 104;
            this.label2.Text = "Password:";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // tbClientPassword
            // 
            this.tbClientPassword.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.tbClientPassword.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.tbClientPassword.Location = new System.Drawing.Point(102, 155);
            this.tbClientPassword.Margin = new System.Windows.Forms.Padding(0);
            this.tbClientPassword.MaxLength = 32;
            this.tbClientPassword.Name = "tbClientPassword";
            this.tbClientPassword.Size = new System.Drawing.Size(178, 20);
            this.tbClientPassword.TabIndex = 2002;
            this.tbClientPassword.UseSystemPasswordChar = true;
            // 
            // btnClientConnect
            // 
            this.btnClientConnect.Accent = ((uint)(0u));
            this.btnClientConnect.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnClientConnect.FlatAppearance.BorderSize = 0;
            this.btnClientConnect.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnClientConnect.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnClientConnect.Location = new System.Drawing.Point(290, 127);
            this.btnClientConnect.Margin = new System.Windows.Forms.Padding(0);
            this.btnClientConnect.Name = "btnClientConnect";
            this.btnClientConnect.Size = new System.Drawing.Size(80, 48);
            this.btnClientConnect.TabIndex = 2003;
            this.btnClientConnect.Text = "Connect";
            this.btnClientConnect.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnClientConnect.TextImageRelation = System.Windows.Forms.TextImageRelation.TextBeforeImage;
            this.btnClientConnect.UseVisualStyleBackColor = true;
            this.btnClientConnect.Click += new System.EventHandler(this.btnClientConnect_Click);
            // 
            // tbClientAddress
            // 
            this.tbClientAddress.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.tbClientAddress.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.tbClientAddress.Location = new System.Drawing.Point(102, 127);
            this.tbClientAddress.Margin = new System.Windows.Forms.Padding(0);
            this.tbClientAddress.MaxLength = 512;
            this.tbClientAddress.Name = "tbClientAddress";
            this.tbClientAddress.Size = new System.Drawing.Size(178, 20);
            this.tbClientAddress.TabIndex = 2001;
            this.tbClientAddress.Text = "127.0.0.1";
            this.tbClientAddress.TextChanged += new System.EventHandler(this.tbClientAddress_TextChanged);
            this.tbClientAddress.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.tbClientAddress_KeyPress);
            // 
            // dlgHostExisting
            // 
            this.dlgHostExisting.Multiselect = true;
            this.dlgHostExisting.Title = "Select existing document(s) to edit";
            // 
            // dlgHostNew
            // 
            this.dlgHostNew.Title = "Choose where to save the new document";
            // 
            // lblConnectingStatus
            // 
            this.lblConnectingStatus.Accent = ((uint)(0u));
            this.lblConnectingStatus.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblConnectingStatus.Location = new System.Drawing.Point(14, -39);
            this.lblConnectingStatus.Margin = new System.Windows.Forms.Padding(0);
            this.lblConnectingStatus.Name = "lblConnectingStatus";
            this.lblConnectingStatus.Size = new System.Drawing.Size(274, 137);
            this.lblConnectingStatus.TabIndex = 2;
            this.lblConnectingStatus.Text = "Connecting...";
            this.lblConnectingStatus.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // panServerPassword
            // 
            this.panServerPassword.Controls.Add(this.labServerPassword);
            this.panServerPassword.Controls.Add(this.tbServerPassword);
            this.panServerPassword.Location = new System.Drawing.Point(699, 12);
            this.panServerPassword.Name = "panServerPassword";
            this.panServerPassword.Size = new System.Drawing.Size(204, 64);
            this.panServerPassword.TabIndex = 6;
            this.panServerPassword.Visible = false;
            // 
            // labServerPassword
            // 
            this.labServerPassword.Accent = ((uint)(0u));
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
            this.tbServerPassword.Margin = new System.Windows.Forms.Padding(0);
            this.tbServerPassword.MaxLength = 32;
            this.tbServerPassword.Name = "tbServerPassword";
            this.tbServerPassword.Size = new System.Drawing.Size(185, 20);
            this.tbServerPassword.TabIndex = 2006;
            this.tbServerPassword.TabStop = false;
            this.tbServerPassword.UseSystemPasswordChar = true;
            this.tbServerPassword.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.tbServerPassword_KeyPress);
            // 
            // panHost
            // 
            this.panHost.Controls.Add(this.btnHostDelete);
            this.panHost.Controls.Add(this.btnHostTemporary);
            this.panHost.Controls.Add(this.btnHostExisting);
            this.panHost.Controls.Add(this.btnHostNew);
            this.panHost.Controls.Add(this.lbDocuments);
            this.panHost.Controls.Add(this.lblHostSettings);
            this.panHost.Controls.Add(this.btnHostStart);
            this.panHost.Controls.Add(this.lblHostDocuments);
            this.panHost.Location = new System.Drawing.Point(32, 170);
            this.panHost.Name = "panHost";
            this.panHost.Padding = new System.Windows.Forms.Padding(10);
            this.panHost.Size = new System.Drawing.Size(482, 385);
            this.panHost.TabIndex = 5;
            // 
            // btnHostDelete
            // 
            this.btnHostDelete.Accent = ((uint)(0u));
            this.btnHostDelete.FlatAppearance.BorderSize = 0;
            this.btnHostDelete.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnHostDelete.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnHostDelete.Location = new System.Drawing.Point(10, 179);
            this.btnHostDelete.Margin = new System.Windows.Forms.Padding(0);
            this.btnHostDelete.Name = "btnHostDelete";
            this.btnHostDelete.Size = new System.Drawing.Size(28, 28);
            this.btnHostDelete.TabIndex = 2009;
            this.btnHostDelete.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnHostDelete.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.toolTips.SetToolTip(this.btnHostDelete, "Remove the selected document(s)");
            this.btnHostDelete.UseVisualStyleBackColor = true;
            this.btnHostDelete.Visible = false;
            this.btnHostDelete.Click += new System.EventHandler(this.btnHostDelete_Click);
            // 
            // btnHostTemporary
            // 
            this.btnHostTemporary.Accent = ((uint)(0u));
            this.btnHostTemporary.FlatAppearance.BorderSize = 0;
            this.btnHostTemporary.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnHostTemporary.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnHostTemporary.Location = new System.Drawing.Point(10, 82);
            this.btnHostTemporary.Margin = new System.Windows.Forms.Padding(0);
            this.btnHostTemporary.Name = "btnHostTemporary";
            this.btnHostTemporary.Size = new System.Drawing.Size(28, 28);
            this.btnHostTemporary.TabIndex = 2008;
            this.btnHostTemporary.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnHostTemporary.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.toolTips.SetToolTip(this.btnHostTemporary, "Add a temporary document (not saved on disk; will be lost when the session is ter" +
        "minated)");
            this.btnHostTemporary.UseVisualStyleBackColor = true;
            this.btnHostTemporary.Click += new System.EventHandler(this.btnHostTemporary_Click);
            // 
            // btnHostExisting
            // 
            this.btnHostExisting.Accent = ((uint)(0u));
            this.btnHostExisting.FlatAppearance.BorderSize = 0;
            this.btnHostExisting.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnHostExisting.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnHostExisting.Location = new System.Drawing.Point(10, 117);
            this.btnHostExisting.Margin = new System.Windows.Forms.Padding(0);
            this.btnHostExisting.Name = "btnHostExisting";
            this.btnHostExisting.Size = new System.Drawing.Size(28, 28);
            this.btnHostExisting.TabIndex = 2007;
            this.btnHostExisting.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnHostExisting.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.toolTips.SetToolTip(this.btnHostExisting, "Add existing documents");
            this.btnHostExisting.UseVisualStyleBackColor = true;
            this.btnHostExisting.Click += new System.EventHandler(this.btnHostExisting_Click);
            // 
            // btnHostNew
            // 
            this.btnHostNew.Accent = ((uint)(0u));
            this.btnHostNew.FlatAppearance.BorderSize = 0;
            this.btnHostNew.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnHostNew.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnHostNew.Location = new System.Drawing.Point(10, 47);
            this.btnHostNew.Margin = new System.Windows.Forms.Padding(0);
            this.btnHostNew.Name = "btnHostNew";
            this.btnHostNew.Size = new System.Drawing.Size(28, 28);
            this.btnHostNew.TabIndex = 2006;
            this.btnHostNew.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnHostNew.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.toolTips.SetToolTip(this.btnHostNew, "Create a new document");
            this.btnHostNew.UseVisualStyleBackColor = true;
            this.btnHostNew.Click += new System.EventHandler(this.btnHostNew_Click);
            // 
            // lbDocuments
            // 
            this.lbDocuments.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.lbDocuments.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.lbDocuments.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawVariable;
            this.lbDocuments.FormattingEnabled = true;
            this.lbDocuments.IntegralHeight = false;
            this.lbDocuments.Location = new System.Drawing.Point(45, 47);
            this.lbDocuments.Margin = new System.Windows.Forms.Padding(0);
            this.lbDocuments.Name = "lbDocuments";
            this.lbDocuments.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
            this.lbDocuments.Size = new System.Drawing.Size(185, 328);
            this.lbDocuments.TabIndex = 108;
            this.lbDocuments.SelectedIndexChanged += new System.EventHandler(this.lbDocuments_SelectedIndexChanged);
            // 
            // lblHostSettings
            // 
            this.lblHostSettings.Accent = ((uint)(0u));
            this.lblHostSettings.FontSize = 1;
            this.lblHostSettings.Location = new System.Drawing.Point(239, 10);
            this.lblHostSettings.Margin = new System.Windows.Forms.Padding(0);
            this.lblHostSettings.Name = "lblHostSettings";
            this.lblHostSettings.Size = new System.Drawing.Size(220, 28);
            this.lblHostSettings.TabIndex = 2005;
            this.lblHostSettings.Text = "Session Configuration";
            this.lblHostSettings.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
            // 
            // btnHostStart
            // 
            this.btnHostStart.Accent = ((uint)(0u));
            this.btnHostStart.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnHostStart.Enabled = false;
            this.btnHostStart.FlatAppearance.BorderSize = 0;
            this.btnHostStart.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnHostStart.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnHostStart.Location = new System.Drawing.Point(379, 327);
            this.btnHostStart.Margin = new System.Windows.Forms.Padding(0);
            this.btnHostStart.Name = "btnHostStart";
            this.btnHostStart.Size = new System.Drawing.Size(80, 48);
            this.btnHostStart.TabIndex = 2004;
            this.btnHostStart.Text = "Start";
            this.btnHostStart.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnHostStart.TextImageRelation = System.Windows.Forms.TextImageRelation.TextBeforeImage;
            this.toolTips.SetToolTip(this.btnHostStart, "Start hosting an OTEX session");
            this.btnHostStart.UseVisualStyleBackColor = true;
            this.btnHostStart.Click += new System.EventHandler(this.btnHostStart_Click);
            // 
            // lblHostDocuments
            // 
            this.lblHostDocuments.Accent = ((uint)(0u));
            this.lblHostDocuments.FontSize = 1;
            this.lblHostDocuments.Location = new System.Drawing.Point(10, 10);
            this.lblHostDocuments.Margin = new System.Windows.Forms.Padding(0);
            this.lblHostDocuments.Name = "lblHostDocuments";
            this.lblHostDocuments.Size = new System.Drawing.Size(220, 28);
            this.lblHostDocuments.TabIndex = 107;
            this.lblHostDocuments.Text = "Documents";
            this.lblHostDocuments.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
            // 
            // lblAbout
            // 
            this.lblAbout.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblAbout.Cursor = System.Windows.Forms.Cursors.Hand;
            this.lblAbout.Location = new System.Drawing.Point(0, 236);
            this.lblAbout.Margin = new System.Windows.Forms.Padding(0);
            this.lblAbout.Name = "lblAbout";
            this.lblAbout.Size = new System.Drawing.Size(264, 30);
            this.lblAbout.TabIndex = 0;
            this.lblAbout.TabStop = true;
            this.lblAbout.Text = "About";
            this.lblAbout.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblDebug
            // 
            this.lblDebug.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblDebug.Cursor = System.Windows.Forms.Cursors.Hand;
            this.lblDebug.Location = new System.Drawing.Point(0, 201);
            this.lblDebug.Margin = new System.Windows.Forms.Padding(0);
            this.lblDebug.Name = "lblDebug";
            this.lblDebug.Size = new System.Drawing.Size(264, 30);
            this.lblDebug.TabIndex = 5;
            this.lblDebug.TabStop = true;
            this.lblDebug.Text = "<guid>";
            this.lblDebug.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // panConnectingPage
            // 
            this.panConnectingPage.Controls.Add(this.panConnectingContent);
            this.panConnectingPage.Controls.Add(this.lblConnectingStatus);
            this.panConnectingPage.Location = new System.Drawing.Point(934, 12);
            this.panConnectingPage.Name = "panConnectingPage";
            this.panConnectingPage.Size = new System.Drawing.Size(300, 158);
            this.panConnectingPage.TabIndex = 7;
            this.panConnectingPage.Resize += new System.EventHandler(this.panConnectingPage_Resize);
            // 
            // panConnectingContent
            // 
            this.panConnectingContent.Controls.Add(this.btnConnectingReconnect);
            this.panConnectingContent.Controls.Add(this.btnConnectingBack);
            this.panConnectingContent.Location = new System.Drawing.Point(21, 109);
            this.panConnectingContent.Name = "panConnectingContent";
            this.panConnectingContent.Size = new System.Drawing.Size(239, 49);
            this.panConnectingContent.TabIndex = 2006;
            // 
            // btnConnectingReconnect
            // 
            this.btnConnectingReconnect.Accent = ((uint)(0u));
            this.btnConnectingReconnect.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnConnectingReconnect.FlatAppearance.BorderSize = 0;
            this.btnConnectingReconnect.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnConnectingReconnect.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnConnectingReconnect.Location = new System.Drawing.Point(159, 1);
            this.btnConnectingReconnect.Margin = new System.Windows.Forms.Padding(0);
            this.btnConnectingReconnect.Name = "btnConnectingReconnect";
            this.btnConnectingReconnect.Size = new System.Drawing.Size(80, 48);
            this.btnConnectingReconnect.TabIndex = 2006;
            this.btnConnectingReconnect.Text = "Retry";
            this.btnConnectingReconnect.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnConnectingReconnect.TextImageRelation = System.Windows.Forms.TextImageRelation.TextBeforeImage;
            this.btnConnectingReconnect.UseVisualStyleBackColor = true;
            this.btnConnectingReconnect.Visible = false;
            this.btnConnectingReconnect.Click += new System.EventHandler(this.btnConnectingReconnect_Click);
            // 
            // btnConnectingBack
            // 
            this.btnConnectingBack.Accent = ((uint)(0u));
            this.btnConnectingBack.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnConnectingBack.FlatAppearance.BorderSize = 0;
            this.btnConnectingBack.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnConnectingBack.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnConnectingBack.Location = new System.Drawing.Point(0, 1);
            this.btnConnectingBack.Margin = new System.Windows.Forms.Padding(0);
            this.btnConnectingBack.Name = "btnConnectingBack";
            this.btnConnectingBack.Size = new System.Drawing.Size(80, 48);
            this.btnConnectingBack.TabIndex = 2005;
            this.btnConnectingBack.Text = "Back";
            this.btnConnectingBack.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnConnectingBack.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.toolTips.SetToolTip(this.btnConnectingBack, "Back to the main menu");
            this.btnConnectingBack.UseVisualStyleBackColor = true;
            this.btnConnectingBack.Visible = false;
            this.btnConnectingBack.Click += new System.EventHandler(this.btnConnectingBack_Click);
            // 
            // panSettings
            // 
            this.panSettings.Controls.Add(this.label6);
            this.panSettings.Controls.Add(this.lblAbout);
            this.panSettings.Controls.Add(this.lblDebug);
            this.panSettings.Controls.Add(this.cbTheme);
            this.panSettings.Controls.Add(this.nudLineLength);
            this.panSettings.Controls.Add(this.label5);
            this.panSettings.Controls.Add(this.cbLineLength);
            this.panSettings.Controls.Add(this.label4);
            this.panSettings.Controls.Add(this.cbClientColour);
            this.panSettings.Controls.Add(this.nudClientUpdateInterval);
            this.panSettings.Controls.Add(this.label3);
            this.panSettings.Location = new System.Drawing.Point(945, 207);
            this.panSettings.Name = "panSettings";
            this.panSettings.Size = new System.Drawing.Size(264, 266);
            this.panSettings.TabIndex = 8;
            // 
            // label6
            // 
            this.label6.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label6.Location = new System.Drawing.Point(0, 148);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(166, 24);
            this.label6.TabIndex = 117;
            this.label6.Text = "Visual style:";
            this.label6.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // cbTheme
            // 
            this.cbTheme.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.cbTheme.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbTheme.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cbTheme.Location = new System.Drawing.Point(169, 151);
            this.cbTheme.Margin = new System.Windows.Forms.Padding(0);
            this.cbTheme.Name = "cbTheme";
            this.cbTheme.Size = new System.Drawing.Size(86, 21);
            this.cbTheme.TabIndex = 116;
            this.cbTheme.SelectedIndexChanged += new System.EventHandler(this.cbTheme_SelectedIndexChanged);
            // 
            // nudLineLength
            // 
            this.nudLineLength.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.nudLineLength.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.nudLineLength.Increment = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.nudLineLength.Location = new System.Drawing.Point(201, 117);
            this.nudLineLength.Margin = new System.Windows.Forms.Padding(0);
            this.nudLineLength.Maximum = new decimal(new int[] {
            200,
            0,
            0,
            0});
            this.nudLineLength.Minimum = new decimal(new int[] {
            60,
            0,
            0,
            0});
            this.nudLineLength.Name = "nudLineLength";
            this.nudLineLength.Size = new System.Drawing.Size(54, 20);
            this.nudLineLength.TabIndex = 115;
            this.nudLineLength.Value = new decimal(new int[] {
            100,
            0,
            0,
            0});
            this.nudLineLength.ValueChanged += new System.EventHandler(this.nudLineLength_ValueChanged);
            // 
            // label5
            // 
            this.label5.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label5.Location = new System.Drawing.Point(0, 114);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(195, 22);
            this.label5.TabIndex = 114;
            this.label5.Text = "Ruler offset (characters):";
            this.label5.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // cbLineLength
            // 
            this.cbLineLength.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cbLineLength.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.cbLineLength.Checked = true;
            this.cbLineLength.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbLineLength.Location = new System.Drawing.Point(0, 79);
            this.cbLineLength.Name = "cbLineLength";
            this.cbLineLength.Size = new System.Drawing.Size(255, 24);
            this.cbLineLength.TabIndex = 113;
            this.cbLineLength.Text = "Draw line-length guide ruler?";
            this.cbLineLength.UseVisualStyleBackColor = true;
            this.cbLineLength.CheckedChanged += new System.EventHandler(this.cbLineLength_CheckedChanged);
            // 
            // label4
            // 
            this.label4.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label4.Location = new System.Drawing.Point(0, 41);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(195, 21);
            this.label4.TabIndex = 112;
            this.label4.Text = "User colour:";
            this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // cbClientColour
            // 
            this.cbClientColour.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.cbClientColour.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.cbClientColour.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbClientColour.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cbClientColour.FormattingEnabled = true;
            this.cbClientColour.Location = new System.Drawing.Point(201, 44);
            this.cbClientColour.Margin = new System.Windows.Forms.Padding(0);
            this.cbClientColour.Name = "cbClientColour";
            this.cbClientColour.ShowNames = false;
            this.cbClientColour.Size = new System.Drawing.Size(54, 21);
            this.cbClientColour.TabIndex = 111;
            this.toolTips.SetToolTip(this.cbClientColour, "Your user colour. Remote users will see your highlight ranges marked out using th" +
        "is colour.");
            this.cbClientColour.SelectedIndexChanged += new System.EventHandler(this.cbClientColour_SelectedIndexChanged);
            // 
            // nudClientUpdateInterval
            // 
            this.nudClientUpdateInterval.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.nudClientUpdateInterval.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.nudClientUpdateInterval.DecimalPlaces = 1;
            this.nudClientUpdateInterval.Increment = new decimal(new int[] {
            5,
            0,
            0,
            65536});
            this.nudClientUpdateInterval.Location = new System.Drawing.Point(201, 12);
            this.nudClientUpdateInterval.Margin = new System.Windows.Forms.Padding(0);
            this.nudClientUpdateInterval.Maximum = new decimal(new int[] {
            50,
            0,
            0,
            65536});
            this.nudClientUpdateInterval.Minimum = new decimal(new int[] {
            5,
            0,
            0,
            65536});
            this.nudClientUpdateInterval.Name = "nudClientUpdateInterval";
            this.nudClientUpdateInterval.Size = new System.Drawing.Size(54, 20);
            this.nudClientUpdateInterval.TabIndex = 110;
            this.toolTips.SetToolTip(this.nudClientUpdateInterval, "How frequently your local client sends requests to (and recieves update responses" +
        " from) the server. Lower values require more bandwidth and system resources.");
            this.nudClientUpdateInterval.Value = new decimal(new int[] {
            10,
            0,
            0,
            65536});
            this.nudClientUpdateInterval.ValueChanged += new System.EventHandler(this.nudClientUpdateInterval_ValueChanged);
            // 
            // label3
            // 
            this.label3.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label3.Location = new System.Drawing.Point(0, 9);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(198, 20);
            this.label3.TabIndex = 109;
            this.label3.Text = "Update interval (seconds):";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // splitter
            // 
            this.splitter.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
            this.splitter.IsSplitterFixed = true;
            this.splitter.Location = new System.Drawing.Point(32, 21);
            this.splitter.Margin = new System.Windows.Forms.Padding(0);
            this.splitter.Name = "splitter";
            // 
            // splitter.Panel2
            // 
            this.splitter.Panel2.Controls.Add(this.sideSplitter);
            this.splitter.Size = new System.Drawing.Size(392, 100);
            this.splitter.SplitterDistance = 120;
            this.splitter.SplitterWidth = 1;
            this.splitter.TabIndex = 9;
            this.splitter.TabStop = false;
            // 
            // sideSplitter
            // 
            this.sideSplitter.Dock = System.Windows.Forms.DockStyle.Fill;
            this.sideSplitter.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.sideSplitter.IsSplitterFixed = true;
            this.sideSplitter.Location = new System.Drawing.Point(0, 0);
            this.sideSplitter.Margin = new System.Windows.Forms.Padding(0);
            this.sideSplitter.Name = "sideSplitter";
            this.sideSplitter.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // sideSplitter.Panel1
            // 
            this.sideSplitter.Panel1.Controls.Add(this.lblSideBar);
            this.sideSplitter.Panel1.Padding = new System.Windows.Forms.Padding(2, 4, 0, 0);
            this.sideSplitter.Panel1.Paint += new System.Windows.Forms.PaintEventHandler(this.themedSplitContainer1_Panel1_Paint);
            // 
            // sideSplitter.Panel2
            // 
            this.sideSplitter.Panel2.Padding = new System.Windows.Forms.Padding(2, 0, 0, 0);
            this.sideSplitter.Size = new System.Drawing.Size(271, 100);
            this.sideSplitter.SplitterDistance = 34;
            this.sideSplitter.SplitterWidth = 1;
            this.sideSplitter.TabIndex = 0;
            this.sideSplitter.TabStop = false;
            // 
            // lblSideBar
            // 
            this.lblSideBar.Accent = ((uint)(0u));
            this.lblSideBar.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblSideBar.FontSize = 1;
            this.lblSideBar.Location = new System.Drawing.Point(2, 4);
            this.lblSideBar.Margin = new System.Windows.Forms.Padding(0);
            this.lblSideBar.Name = "lblSideBar";
            this.lblSideBar.Size = new System.Drawing.Size(269, 30);
            this.lblSideBar.TabIndex = 108;
            this.lblSideBar.Text = "<sidebar>";
            this.lblSideBar.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
            // 
            // cmUsers
            // 
            this.cmUsers.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.showSelectionsToolStripMenuItem,
            this.adminSeparator,
            this.adminToolStripMenuItem});
            this.cmUsers.Name = "cmUsers";
            this.cmUsers.Size = new System.Drawing.Size(155, 54);
            this.cmUsers.Opening += new System.ComponentModel.CancelEventHandler(this.cmUsers_Opening);
            // 
            // showSelectionsToolStripMenuItem
            // 
            this.showSelectionsToolStripMenuItem.Checked = true;
            this.showSelectionsToolStripMenuItem.CheckOnClick = true;
            this.showSelectionsToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.showSelectionsToolStripMenuItem.Name = "showSelectionsToolStripMenuItem";
            this.showSelectionsToolStripMenuItem.Size = new System.Drawing.Size(154, 22);
            this.showSelectionsToolStripMenuItem.Text = "Show Selection";
            // 
            // adminSeparator
            // 
            this.adminSeparator.Name = "adminSeparator";
            this.adminSeparator.Size = new System.Drawing.Size(151, 6);
            // 
            // adminToolStripMenuItem
            // 
            this.adminToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.readonlyToolStripMenuItem,
            this.kickToolStripMenuItem,
            this.banToolStripMenuItem});
            this.adminToolStripMenuItem.Name = "adminToolStripMenuItem";
            this.adminToolStripMenuItem.Size = new System.Drawing.Size(154, 22);
            this.adminToolStripMenuItem.Text = "Admin";
            // 
            // readonlyToolStripMenuItem
            // 
            this.readonlyToolStripMenuItem.CheckOnClick = true;
            this.readonlyToolStripMenuItem.Name = "readonlyToolStripMenuItem";
            this.readonlyToolStripMenuItem.Size = new System.Drawing.Size(128, 22);
            this.readonlyToolStripMenuItem.Text = "Read-only";
            // 
            // kickToolStripMenuItem
            // 
            this.kickToolStripMenuItem.Name = "kickToolStripMenuItem";
            this.kickToolStripMenuItem.Size = new System.Drawing.Size(128, 22);
            this.kickToolStripMenuItem.Text = "Kick";
            this.kickToolStripMenuItem.Click += new System.EventHandler(this.kickToolStripMenuItem_Click);
            // 
            // banToolStripMenuItem
            // 
            this.banToolStripMenuItem.Name = "banToolStripMenuItem";
            this.banToolStripMenuItem.Size = new System.Drawing.Size(128, 22);
            this.banToolStripMenuItem.Text = "Ban";
            this.banToolStripMenuItem.Click += new System.EventHandler(this.kickToolStripMenuItem_Click);
            // 
            // panEditors
            // 
            this.panEditors.Location = new System.Drawing.Point(525, 12);
            this.panEditors.Margin = new System.Windows.Forms.Padding(0);
            this.panEditors.Name = "panEditors";
            this.panEditors.Size = new System.Drawing.Size(33, 37);
            this.panEditors.TabIndex = 11;
            // 
            // menuSplitter
            // 
            this.menuSplitter.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.menuSplitter.IsSplitterFixed = true;
            this.menuSplitter.Location = new System.Drawing.Point(557, 456);
            this.menuSplitter.Margin = new System.Windows.Forms.Padding(0);
            this.menuSplitter.Name = "menuSplitter";
            this.menuSplitter.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // menuSplitter.Panel1
            // 
            this.menuSplitter.Panel1.Controls.Add(this.btnHost);
            this.menuSplitter.Panel1.Controls.Add(this.btnJoin);
            this.menuSplitter.Size = new System.Drawing.Size(560, 234);
            this.menuSplitter.SplitterDistance = 60;
            this.menuSplitter.SplitterWidth = 1;
            this.menuSplitter.TabIndex = 12;
            this.menuSplitter.TabStop = false;
            // 
            // tbHostTempName
            // 
            this.tbHostTempName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbHostTempName.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.tbHostTempName.Location = new System.Drawing.Point(10, 25);
            this.tbHostTempName.Margin = new System.Windows.Forms.Padding(0);
            this.tbHostTempName.MaxLength = 32;
            this.tbHostTempName.Name = "tbHostTempName";
            this.tbHostTempName.Size = new System.Drawing.Size(185, 20);
            this.tbHostTempName.TabIndex = 2006;
            this.tbHostTempName.TabStop = false;
            this.toolTips.SetToolTip(this.tbHostTempName, "A description of your temporary document.  Cannot be blank, and must be at most 3" +
        "2 characters. Put a file extension at the end to invoke syntax highlighting for " +
        "the associated language.");
            this.tbHostTempName.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.tbHostTempName_KeyPress);
            // 
            // panHostTempName
            // 
            this.panHostTempName.Controls.Add(this.lblHostTempName);
            this.panHostTempName.Controls.Add(this.tbHostTempName);
            this.panHostTempName.Location = new System.Drawing.Point(575, 114);
            this.panHostTempName.Name = "panHostTempName";
            this.panHostTempName.Size = new System.Drawing.Size(204, 64);
            this.panHostTempName.TabIndex = 2007;
            this.panHostTempName.Visible = false;
            // 
            // lblHostTempName
            // 
            this.lblHostTempName.Accent = ((uint)(0u));
            this.lblHostTempName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblHostTempName.Location = new System.Drawing.Point(10, 0);
            this.lblHostTempName.Margin = new System.Windows.Forms.Padding(0);
            this.lblHostTempName.Name = "lblHostTempName";
            this.lblHostTempName.Size = new System.Drawing.Size(185, 20);
            this.lblHostTempName.TabIndex = 2005;
            this.lblHostTempName.Text = "Temp document description:";
            this.lblHostTempName.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
            // 
            // flpUsers
            // 
            this.flpUsers.AutoScroll = true;
            this.flpUsers.ContextMenuStrip = this.cmUsers;
            this.flpUsers.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flpUsers.Location = new System.Drawing.Point(480, 12);
            this.flpUsers.Margin = new System.Windows.Forms.Padding(0);
            this.flpUsers.Name = "flpUsers";
            this.flpUsers.Size = new System.Drawing.Size(34, 37);
            this.flpUsers.TabIndex = 10;
            this.flpUsers.WrapContents = false;
            // 
            // EditorForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1372, 676);
            this.Controls.Add(this.panHostTempName);
            this.Controls.Add(this.panHost);
            this.Controls.Add(this.panSettings);
            this.Controls.Add(this.menuSplitter);
            this.Controls.Add(this.panEditors);
            this.Controls.Add(this.flpUsers);
            this.Controls.Add(this.splitter);
            this.Controls.Add(this.panConnectingPage);
            this.Controls.Add(this.panServerPassword);
            this.Controls.Add(this.panJoin);
            this.MinimumSize = new System.Drawing.Size(500, 400);
            this.Name = "EditorForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "OTEX Editor";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.EditorForm_FormClosing);
            this.panJoin.ResumeLayout(false);
            this.panJoin.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvServers)).EndInit();
            this.panServerPassword.ResumeLayout(false);
            this.panServerPassword.PerformLayout();
            this.panHost.ResumeLayout(false);
            this.panConnectingPage.ResumeLayout(false);
            this.panConnectingContent.ResumeLayout(false);
            this.panSettings.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.nudLineLength)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudClientUpdateInterval)).EndInit();
            this.splitter.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitter)).EndInit();
            this.splitter.ResumeLayout(false);
            this.sideSplitter.Panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.sideSplitter)).EndInit();
            this.sideSplitter.ResumeLayout(false);
            this.cmUsers.ResumeLayout(false);
            this.menuSplitter.Panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.menuSplitter)).EndInit();
            this.menuSplitter.ResumeLayout(false);
            this.panHostTempName.ResumeLayout(false);
            this.panHostTempName.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion
        private Marzersoft.Themes.ThemedRadioButton btnHost;
        private Marzersoft.Themes.ThemedRadioButton btnJoin;
        private System.Windows.Forms.Panel panJoin;
        private Marzersoft.Controls.IPEndPointTextBox tbClientAddress;
        private Marzersoft.Themes.ThemedButton btnClientConnect;
        private System.Windows.Forms.OpenFileDialog dlgHostExisting;
        private System.Windows.Forms.SaveFileDialog dlgHostNew;
        private Marzersoft.Themes.ThemedLabel lblConnectingStatus;
        private System.Windows.Forms.Panel panHost;
        private System.Windows.Forms.Label lblAbout;
        private System.Windows.Forms.Label label2;
        private Marzersoft.Themes.ThemedTextBox tbClientPassword;
        private Marzersoft.Themes.ThemedDataGridView dgvServers;
        private Marzersoft.Themes.ThemedLabel lblJoinPublic;
        private Marzersoft.Themes.ThemedLabel lblJoinManual;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Panel panServerPassword;
        private Marzersoft.Themes.ThemedLabel labServerPassword;
        private Marzersoft.Themes.ThemedTextBox tbServerPassword;
        private System.Windows.Forms.Panel panConnectingPage;
        private System.Windows.Forms.Panel panConnectingContent;
        private Marzersoft.Themes.ThemedButton btnConnectingReconnect;
        private Marzersoft.Themes.ThemedButton btnConnectingBack;
        private System.Windows.Forms.Panel panSettings;
        private Marzersoft.Themes.ThemedNumericUpDown nudClientUpdateInterval;
        private System.Windows.Forms.Label label3;
        private Marzersoft.Controls.ColourComboBox cbClientColour;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.CheckBox cbLineLength;
        private Marzersoft.Themes.ThemedNumericUpDown nudLineLength;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private Marzersoft.Themes.ThemedComboBox cbTheme;
        private Marzersoft.Themes.ThemedSplitContainer splitter;
        private Marzersoft.Themes.ThemedSplitContainer sideSplitter;
        private Marzersoft.Themes.ThemedLabel lblSideBar;
        private UserList flpUsers;
        private System.Windows.Forms.ContextMenuStrip cmUsers;
        private System.Windows.Forms.ToolStripMenuItem showSelectionsToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator adminSeparator;
        private System.Windows.Forms.ToolStripMenuItem adminToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem readonlyToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem kickToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem banToolStripMenuItem;
        private System.Windows.Forms.Label lblDebug;
        internal Marzersoft.Themes.ThemedPanel panEditors;
        private System.Windows.Forms.DataGridViewTextBoxColumn colServerName;
        private System.Windows.Forms.DataGridViewTextBoxColumn colServerAddress;
        private System.Windows.Forms.DataGridViewTextBoxColumn colServerPort;
        private System.Windows.Forms.DataGridViewCheckBoxColumn colPassword;
        private System.Windows.Forms.DataGridViewTextBoxColumn colUserCount;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPing;
        private Marzersoft.Themes.ThemedSplitContainer menuSplitter;
        private Marzersoft.Themes.ThemedLabel lblHostDocuments;
        private Marzersoft.Themes.ThemedButton btnHostStart;
        private Marzersoft.Themes.ThemedListBox lbDocuments;
        private Marzersoft.Themes.ThemedLabel lblHostSettings;
        private Marzersoft.Themes.ThemedButton btnHostTemporary;
        private Marzersoft.Themes.ThemedButton btnHostExisting;
        private Marzersoft.Themes.ThemedButton btnHostNew;
        private Marzersoft.Themes.ThemedButton btnHostDelete;
        private System.Windows.Forms.ToolTip toolTips;
        private System.Windows.Forms.Panel panHostTempName;
        private Marzersoft.Themes.ThemedLabel lblHostTempName;
        private Marzersoft.Themes.ThemedTextBox tbHostTempName;
    }
}

