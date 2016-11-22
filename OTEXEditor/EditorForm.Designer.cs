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
            this.tbClientPassword = new Marzersoft.Themes.ThemedTextBox();
            this.btnClientCancel = new Marzersoft.Themes.ThemedButton();
            this.btnClientConnect = new Marzersoft.Themes.ThemedButton();
            this.tbClientAddress = new Marzersoft.Controls.IPEndPointTextBox();
            this.dlgServerOpenExisting = new System.Windows.Forms.OpenFileDialog();
            this.dlgServerCreateNew = new System.Windows.Forms.SaveFileDialog();
            this.lblConnectingStatus = new System.Windows.Forms.Label();
            this.panServerPassword = new System.Windows.Forms.Panel();
            this.labServerPassword = new System.Windows.Forms.Label();
            this.tbServerPassword = new Marzersoft.Themes.ThemedTextBox();
            this.panMenuPage = new System.Windows.Forms.Panel();
            this.lblAbout = new System.Windows.Forms.Label();
            this.lblVersion = new System.Windows.Forms.Label();
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
            this.panMenu.SuspendLayout();
            this.panMenuButtons.SuspendLayout();
            this.panServerBrowserPage.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvServers)).BeginInit();
            this.panServerPassword.SuspendLayout();
            this.panMenuPage.SuspendLayout();
            this.panConnectingPage.SuspendLayout();
            this.panConnectingContent.SuspendLayout();
            this.panSettings.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudLineLength)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudClientUpdateInterval)).BeginInit();
            this.SuspendLayout();
            // 
            // btnServerExisting
            // 
            this.btnServerExisting.Accent = ((uint)(2u));
            this.btnServerExisting.AccentMode = false;
            this.btnServerExisting.BackColor = System.Drawing.SystemColors.Control;
            this.btnServerExisting.FlatAppearance.BorderSize = 0;
            this.btnServerExisting.FlatAppearance.MouseDownBackColor = System.Drawing.SystemColors.Highlight;
            this.btnServerExisting.FlatAppearance.MouseOverBackColor = System.Drawing.SystemColors.ControlLight;
            this.btnServerExisting.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnServerExisting.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnServerExisting.ForeColor = System.Drawing.SystemColors.ControlText;
            this.btnServerExisting.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnServerExisting.Location = new System.Drawing.Point(0, 53);
            this.btnServerExisting.Margin = new System.Windows.Forms.Padding(0, 2, 0, 2);
            this.btnServerExisting.Name = "btnServerExisting";
            this.btnServerExisting.Size = new System.Drawing.Size(267, 48);
            this.btnServerExisting.TabIndex = 1;
            this.btnServerExisting.Text = "Host an existing document";
            this.btnServerExisting.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnServerExisting.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnServerExisting.UseVisualStyleBackColor = false;
            this.btnServerExisting.Click += new System.EventHandler(this.btnServerExisting_Click);
            // 
            // btnServerNew
            // 
            this.btnServerNew.Accent = ((uint)(2u));
            this.btnServerNew.AccentMode = false;
            this.btnServerNew.BackColor = System.Drawing.SystemColors.Control;
            this.btnServerNew.FlatAppearance.BorderSize = 0;
            this.btnServerNew.FlatAppearance.MouseDownBackColor = System.Drawing.SystemColors.Highlight;
            this.btnServerNew.FlatAppearance.MouseOverBackColor = System.Drawing.SystemColors.ControlLight;
            this.btnServerNew.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnServerNew.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnServerNew.ForeColor = System.Drawing.SystemColors.ControlText;
            this.btnServerNew.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnServerNew.Location = new System.Drawing.Point(0, 0);
            this.btnServerNew.Margin = new System.Windows.Forms.Padding(0, 0, 0, 2);
            this.btnServerNew.Name = "btnServerNew";
            this.btnServerNew.Size = new System.Drawing.Size(267, 48);
            this.btnServerNew.TabIndex = 0;
            this.btnServerNew.Text = "Host a new document";
            this.btnServerNew.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnServerNew.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnServerNew.UseVisualStyleBackColor = false;
            this.btnServerNew.Click += new System.EventHandler(this.btnServerNew_Click);
            // 
            // btnClient
            // 
            this.btnClient.Accent = ((uint)(0u));
            this.btnClient.AccentMode = false;
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
            this.panMenu.Location = new System.Drawing.Point(21, 5);
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
            this.btnServerTemporary.Accent = ((uint)(1u));
            this.btnServerTemporary.AccentMode = false;
            this.btnServerTemporary.BackColor = System.Drawing.SystemColors.Control;
            this.btnServerTemporary.FlatAppearance.BorderSize = 0;
            this.btnServerTemporary.FlatAppearance.MouseDownBackColor = System.Drawing.SystemColors.Highlight;
            this.btnServerTemporary.FlatAppearance.MouseOverBackColor = System.Drawing.SystemColors.ControlLight;
            this.btnServerTemporary.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnServerTemporary.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnServerTemporary.ForeColor = System.Drawing.SystemColors.ControlText;
            this.btnServerTemporary.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnServerTemporary.Location = new System.Drawing.Point(0, 106);
            this.btnServerTemporary.Margin = new System.Windows.Forms.Padding(0, 2, 0, 0);
            this.btnServerTemporary.Name = "btnServerTemporary";
            this.btnServerTemporary.Size = new System.Drawing.Size(267, 48);
            this.btnServerTemporary.TabIndex = 3;
            this.btnServerTemporary.Text = "Host temporary document";
            this.btnServerTemporary.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnServerTemporary.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnServerTemporary.UseVisualStyleBackColor = false;
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
            this.panServerBrowserPage.Controls.Add(this.label1);
            this.panServerBrowserPage.Controls.Add(this.lblManualEntry);
            this.panServerBrowserPage.Controls.Add(this.lblServerBrowser);
            this.panServerBrowserPage.Controls.Add(this.dgvServers);
            this.panServerBrowserPage.Controls.Add(this.label2);
            this.panServerBrowserPage.Controls.Add(this.tbClientPassword);
            this.panServerBrowserPage.Controls.Add(this.btnClientCancel);
            this.panServerBrowserPage.Controls.Add(this.btnClientConnect);
            this.panServerBrowserPage.Controls.Add(this.tbClientAddress);
            this.panServerBrowserPage.Location = new System.Drawing.Point(9, 15);
            this.panServerBrowserPage.Margin = new System.Windows.Forms.Padding(0);
            this.panServerBrowserPage.Name = "panServerBrowserPage";
            this.panServerBrowserPage.Size = new System.Drawing.Size(471, 188);
            this.panServerBrowserPage.TabIndex = 4;
            this.panServerBrowserPage.Visible = false;
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.Location = new System.Drawing.Point(116, 126);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(81, 20);
            this.label1.TabIndex = 108;
            this.label1.Text = "Address:";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblManualEntry
            // 
            this.lblManualEntry.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.lblManualEntry.Location = new System.Drawing.Point(131, 89);
            this.lblManualEntry.Margin = new System.Windows.Forms.Padding(0);
            this.lblManualEntry.Name = "lblManualEntry";
            this.lblManualEntry.Size = new System.Drawing.Size(327, 28);
            this.lblManualEntry.TabIndex = 107;
            this.lblManualEntry.Text = "Enter document address manually";
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
            this.lblServerBrowser.Text = "Public documents";
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
            this.dgvServers.Size = new System.Drawing.Size(447, 49);
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
            this.label2.Location = new System.Drawing.Point(116, 154);
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
            this.tbClientPassword.Location = new System.Drawing.Point(203, 154);
            this.tbClientPassword.Margin = new System.Windows.Forms.Padding(0);
            this.tbClientPassword.MaxLength = 32;
            this.tbClientPassword.Name = "tbClientPassword";
            this.tbClientPassword.Size = new System.Drawing.Size(166, 20);
            this.tbClientPassword.TabIndex = 2002;
            this.tbClientPassword.UseSystemPasswordChar = true;
            // 
            // btnClientCancel
            // 
            this.btnClientCancel.Accent = ((uint)(0u));
            this.btnClientCancel.AccentMode = false;
            this.btnClientCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnClientCancel.FlatAppearance.BorderSize = 0;
            this.btnClientCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnClientCancel.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnClientCancel.Location = new System.Drawing.Point(11, 128);
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
            this.btnClientConnect.Accent = ((uint)(0u));
            this.btnClientConnect.AccentMode = false;
            this.btnClientConnect.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnClientConnect.FlatAppearance.BorderSize = 0;
            this.btnClientConnect.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnClientConnect.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnClientConnect.Location = new System.Drawing.Point(378, 128);
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
            this.tbClientAddress.Location = new System.Drawing.Point(203, 126);
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
            // lblConnectingStatus
            // 
            this.lblConnectingStatus.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblConnectingStatus.Location = new System.Drawing.Point(14, -39);
            this.lblConnectingStatus.Name = "lblConnectingStatus";
            this.lblConnectingStatus.Size = new System.Drawing.Size(587, 137);
            this.lblConnectingStatus.TabIndex = 2;
            this.lblConnectingStatus.Text = "Connecting...";
            this.lblConnectingStatus.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // panServerPassword
            // 
            this.panServerPassword.Controls.Add(this.labServerPassword);
            this.panServerPassword.Controls.Add(this.tbServerPassword);
            this.panServerPassword.Location = new System.Drawing.Point(494, 15);
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
            this.tbServerPassword.Margin = new System.Windows.Forms.Padding(0);
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
            this.panMenuPage.Controls.Add(this.lblVersion);
            this.panMenuPage.Location = new System.Drawing.Point(873, 15);
            this.panMenuPage.Name = "panMenuPage";
            this.panMenuPage.Size = new System.Drawing.Size(345, 345);
            this.panMenuPage.TabIndex = 5;
            this.panMenuPage.Resize += new System.EventHandler(this.panMenuPage_Resize);
            // 
            // lblAbout
            // 
            this.lblAbout.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblAbout.Cursor = System.Windows.Forms.Cursors.Hand;
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
            this.lblVersion.Location = new System.Drawing.Point(243, 313);
            this.lblVersion.Margin = new System.Windows.Forms.Padding(0);
            this.lblVersion.Name = "lblVersion";
            this.lblVersion.Size = new System.Drawing.Size(100, 30);
            this.lblVersion.TabIndex = 4;
            this.lblVersion.Text = "label2";
            this.lblVersion.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // panConnectingPage
            // 
            this.panConnectingPage.Controls.Add(this.panConnectingContent);
            this.panConnectingPage.Controls.Add(this.lblConnectingStatus);
            this.panConnectingPage.Location = new System.Drawing.Point(63, 219);
            this.panConnectingPage.Name = "panConnectingPage";
            this.panConnectingPage.Size = new System.Drawing.Size(613, 179);
            this.panConnectingPage.TabIndex = 7;
            this.panConnectingPage.Resize += new System.EventHandler(this.panConnectingPage_Resize);
            // 
            // panConnectingContent
            // 
            this.panConnectingContent.Controls.Add(this.btnConnectingReconnect);
            this.panConnectingContent.Controls.Add(this.btnConnectingBack);
            this.panConnectingContent.Location = new System.Drawing.Point(165, 103);
            this.panConnectingContent.Name = "panConnectingContent";
            this.panConnectingContent.Size = new System.Drawing.Size(239, 49);
            this.panConnectingContent.TabIndex = 2006;
            // 
            // btnConnectingReconnect
            // 
            this.btnConnectingReconnect.Accent = ((uint)(0u));
            this.btnConnectingReconnect.AccentMode = false;
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
            this.btnConnectingBack.AccentMode = false;
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
            this.btnConnectingBack.UseVisualStyleBackColor = true;
            this.btnConnectingBack.Visible = false;
            this.btnConnectingBack.Click += new System.EventHandler(this.btnConnectingBack_Click);
            // 
            // panSettings
            // 
            this.panSettings.Controls.Add(this.label6);
            this.panSettings.Controls.Add(this.cbTheme);
            this.panSettings.Controls.Add(this.nudLineLength);
            this.panSettings.Controls.Add(this.label5);
            this.panSettings.Controls.Add(this.cbLineLength);
            this.panSettings.Controls.Add(this.label4);
            this.panSettings.Controls.Add(this.cbClientColour);
            this.panSettings.Controls.Add(this.nudClientUpdateInterval);
            this.panSettings.Controls.Add(this.label3);
            this.panSettings.Location = new System.Drawing.Point(548, 114);
            this.panSettings.Name = "panSettings";
            this.panSettings.Size = new System.Drawing.Size(245, 185);
            this.panSettings.TabIndex = 8;
            // 
            // label6
            // 
            this.label6.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label6.Location = new System.Drawing.Point(7, 151);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(109, 24);
            this.label6.TabIndex = 117;
            this.label6.Text = "Visual style:";
            this.label6.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // cbTheme
            // 
            this.cbTheme.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbTheme.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cbTheme.Location = new System.Drawing.Point(131, 151);
            this.cbTheme.Margin = new System.Windows.Forms.Padding(0);
            this.cbTheme.Name = "cbTheme";
            this.cbTheme.Size = new System.Drawing.Size(105, 21);
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
            this.nudLineLength.Location = new System.Drawing.Point(182, 117);
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
            this.label5.Location = new System.Drawing.Point(7, 115);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(169, 24);
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
            this.cbLineLength.Location = new System.Drawing.Point(7, 82);
            this.cbLineLength.Name = "cbLineLength";
            this.cbLineLength.Size = new System.Drawing.Size(229, 24);
            this.cbLineLength.TabIndex = 113;
            this.cbLineLength.Text = "Draw line-length guide ruler?";
            this.cbLineLength.UseVisualStyleBackColor = true;
            this.cbLineLength.CheckedChanged += new System.EventHandler(this.cbLineLength_CheckedChanged);
            // 
            // label4
            // 
            this.label4.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label4.Location = new System.Drawing.Point(7, 45);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(169, 24);
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
            this.cbClientColour.Location = new System.Drawing.Point(182, 44);
            this.cbClientColour.Margin = new System.Windows.Forms.Padding(0);
            this.cbClientColour.Name = "cbClientColour";
            this.cbClientColour.ShowNames = false;
            this.cbClientColour.Size = new System.Drawing.Size(54, 21);
            this.cbClientColour.TabIndex = 111;
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
            this.nudClientUpdateInterval.Location = new System.Drawing.Point(182, 12);
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
            this.label3.Location = new System.Drawing.Point(7, 10);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(169, 24);
            this.label3.TabIndex = 109;
            this.label3.Text = "Update interval (seconds):";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // EditorForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1074, 478);
            this.Controls.Add(this.panSettings);
            this.Controls.Add(this.panConnectingPage);
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
            this.panConnectingPage.ResumeLayout(false);
            this.panConnectingContent.ResumeLayout(false);
            this.panSettings.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.nudLineLength)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudClientUpdateInterval)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion
        private Marzersoft.Themes.ThemedButton btnServerNew;
        private Marzersoft.Themes.ThemedButton btnServerExisting;
        private Marzersoft.Themes.ThemedButton btnClient;
        private System.Windows.Forms.Panel panMenu;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Panel panServerBrowserPage;
        private Marzersoft.Controls.IPEndPointTextBox tbClientAddress;
        private Marzersoft.Themes.ThemedButton btnClientCancel;
        private Marzersoft.Themes.ThemedButton btnClientConnect;
        private System.Windows.Forms.OpenFileDialog dlgServerOpenExisting;
        private System.Windows.Forms.SaveFileDialog dlgServerCreateNew;
        private System.Windows.Forms.Label lblConnectingStatus;
        private System.Windows.Forms.Panel panMenuButtons;
        private System.Windows.Forms.Panel panMenuPage;
        private System.Windows.Forms.Label lblAbout;
        private System.Windows.Forms.Label lblVersion;
        private Marzersoft.Themes.ThemedButton btnServerTemporary;
        private System.Windows.Forms.Label label2;
        private Marzersoft.Themes.ThemedTextBox tbClientPassword;
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
    }
}

