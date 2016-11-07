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
            this.panControls = new System.Windows.Forms.Panel();
            this.lblTitle = new System.Windows.Forms.Label();
            this.panClient = new Marzersoft.Themes.ThemedPanel();
            this.btnClientCancel = new Marzersoft.Themes.ThemedButton();
            this.btnClientConnect = new Marzersoft.Themes.ThemedButton();
            this.label1 = new System.Windows.Forms.Label();
            this.tbClientAddress = new System.Windows.Forms.TextBox();
            this.dlgServerOpenExisting = new System.Windows.Forms.OpenFileDialog();
            this.dlgServerCreateNew = new System.Windows.Forms.SaveFileDialog();
            this.lblStatus = new System.Windows.Forms.Label();
            this.panBody = new System.Windows.Forms.Panel();
            this.panSplash = new System.Windows.Forms.Panel();
            this.lblAbout = new System.Windows.Forms.Label();
            this.lblVersion = new System.Windows.Forms.Label();
            this.panCaption = new Marzersoft.Themes.ThemedCaptionBar();
            this.panMenu.SuspendLayout();
            this.panControls.SuspendLayout();
            this.panClient.SuspendLayout();
            this.panBody.SuspendLayout();
            this.panSplash.SuspendLayout();
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
            this.btnClient.Location = new System.Drawing.Point(0, 106);
            this.btnClient.Margin = new System.Windows.Forms.Padding(0, 2, 0, 0);
            this.btnClient.Name = "btnClient";
            this.btnClient.Size = new System.Drawing.Size(267, 48);
            this.btnClient.TabIndex = 2;
            this.btnClient.Text = "Join someone else\'s session";
            this.btnClient.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnClient.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnClient.UseVisualStyleBackColor = true;
            this.btnClient.Click += new System.EventHandler(this.btnClient_Click);
            // 
            // panMenu
            // 
            this.panMenu.Controls.Add(this.panControls);
            this.panMenu.Controls.Add(this.lblTitle);
            this.panMenu.Location = new System.Drawing.Point(31, 3);
            this.panMenu.Name = "panMenu";
            this.panMenu.Size = new System.Drawing.Size(279, 272);
            this.panMenu.TabIndex = 3;
            // 
            // panControls
            // 
            this.panControls.Controls.Add(this.btnServerNew);
            this.panControls.Controls.Add(this.btnServerExisting);
            this.panControls.Controls.Add(this.btnClient);
            this.panControls.Location = new System.Drawing.Point(6, 105);
            this.panControls.Margin = new System.Windows.Forms.Padding(2);
            this.panControls.Name = "panControls";
            this.panControls.Size = new System.Drawing.Size(267, 158);
            this.panControls.TabIndex = 7;
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
            // panClient
            // 
            this.panClient.Controls.Add(this.btnClientCancel);
            this.panClient.Controls.Add(this.btnClientConnect);
            this.panClient.Controls.Add(this.label1);
            this.panClient.Controls.Add(this.tbClientAddress);
            this.panClient.Location = new System.Drawing.Point(349, 82);
            this.panClient.Margin = new System.Windows.Forms.Padding(0);
            this.panClient.Name = "panClient";
            this.panClient.Size = new System.Drawing.Size(267, 48);
            this.panClient.TabIndex = 4;
            this.panClient.Visible = false;
            // 
            // btnClientCancel
            // 
            this.btnClientCancel.FlatAppearance.BorderSize = 0;
            this.btnClientCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnClientCancel.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnClientCancel.Location = new System.Drawing.Point(239, 0);
            this.btnClientCancel.Margin = new System.Windows.Forms.Padding(0);
            this.btnClientCancel.Name = "btnClientCancel";
            this.btnClientCancel.Size = new System.Drawing.Size(28, 48);
            this.btnClientCancel.TabIndex = 102;
            this.btnClientCancel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnClientCancel.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnClientCancel.UseVisualStyleBackColor = true;
            this.btnClientCancel.Click += new System.EventHandler(this.btnClientCancel_Click);
            // 
            // btnClientConnect
            // 
            this.btnClientConnect.FlatAppearance.BorderSize = 0;
            this.btnClientConnect.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnClientConnect.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnClientConnect.Location = new System.Drawing.Point(211, 0);
            this.btnClientConnect.Margin = new System.Windows.Forms.Padding(0);
            this.btnClientConnect.Name = "btnClientConnect";
            this.btnClientConnect.Size = new System.Drawing.Size(28, 48);
            this.btnClientConnect.TabIndex = 101;
            this.btnClientConnect.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnClientConnect.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnClientConnect.UseVisualStyleBackColor = true;
            this.btnClientConnect.Click += new System.EventHandler(this.btnClientConnect_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(1, 16);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(48, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "Address:";
            // 
            // tbClientAddress
            // 
            this.tbClientAddress.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.tbClientAddress.Location = new System.Drawing.Point(54, 13);
            this.tbClientAddress.Name = "tbClientAddress";
            this.tbClientAddress.Size = new System.Drawing.Size(154, 20);
            this.tbClientAddress.TabIndex = 100;
            this.tbClientAddress.Text = "127.0.0.1";
            this.tbClientAddress.TextChanged += new System.EventHandler(this.tbClientAddress_TextChanged);
            this.tbClientAddress.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.tbClientAddress_KeyPress);
            // 
            // dlgServerOpenExisting
            // 
            this.dlgServerOpenExisting.DefaultExt = "txt";
            this.dlgServerOpenExisting.Filter = "Plain-text files|*.txt";
            this.dlgServerOpenExisting.Title = "Select an existing file to collaboratively edit";
            // 
            // dlgServerCreateNew
            // 
            this.dlgServerCreateNew.DefaultExt = "txt";
            this.dlgServerCreateNew.Filter = "Plain-text files|*.txt";
            this.dlgServerCreateNew.Title = "Select a new file to create collaboratively";
            // 
            // lblStatus
            // 
            this.lblStatus.Location = new System.Drawing.Point(369, 159);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(267, 28);
            this.lblStatus.TabIndex = 2;
            this.lblStatus.Text = "Connecting...";
            this.lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // panBody
            // 
            this.panBody.Controls.Add(this.panSplash);
            this.panBody.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panBody.Location = new System.Drawing.Point(0, 32);
            this.panBody.Name = "panBody";
            this.panBody.Size = new System.Drawing.Size(837, 368);
            this.panBody.TabIndex = 9;
            // 
            // panSplash
            // 
            this.panSplash.Controls.Add(this.lblAbout);
            this.panSplash.Controls.Add(this.lblStatus);
            this.panSplash.Controls.Add(this.panMenu);
            this.panSplash.Controls.Add(this.lblVersion);
            this.panSplash.Controls.Add(this.panClient);
            this.panSplash.Location = new System.Drawing.Point(27, 19);
            this.panSplash.Name = "panSplash";
            this.panSplash.Size = new System.Drawing.Size(798, 313);
            this.panSplash.TabIndex = 5;
            // 
            // lblAbout
            // 
            this.lblAbout.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblAbout.Location = new System.Drawing.Point(0, 275);
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
            this.lblVersion.Location = new System.Drawing.Point(668, 275);
            this.lblVersion.Margin = new System.Windows.Forms.Padding(0);
            this.lblVersion.Name = "lblVersion";
            this.lblVersion.Size = new System.Drawing.Size(130, 30);
            this.lblVersion.TabIndex = 4;
            this.lblVersion.Text = "label2";
            this.lblVersion.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // panCaption
            // 
            this.panCaption.Dock = System.Windows.Forms.DockStyle.Top;
            this.panCaption.Location = new System.Drawing.Point(0, 0);
            this.panCaption.Margin = new System.Windows.Forms.Padding(0);
            this.panCaption.Name = "panCaption";
            this.panCaption.Size = new System.Drawing.Size(837, 32);
            this.panCaption.TabIndex = 8;
            // 
            // EditorForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(837, 400);
            this.Controls.Add(this.panBody);
            this.Controls.Add(this.panCaption);
            this.MinimumSize = new System.Drawing.Size(500, 400);
            this.Name = "EditorForm";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.Text = "OTEX Editor";
            this.TextFlourishes = false;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.EditorForm_FormClosing);
            this.panMenu.ResumeLayout(false);
            this.panControls.ResumeLayout(false);
            this.panClient.ResumeLayout(false);
            this.panClient.PerformLayout();
            this.panBody.ResumeLayout(false);
            this.panSplash.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion
        private Marzersoft.Themes.ThemedButton btnServerNew;
        private Marzersoft.Themes.ThemedButton btnServerExisting;
        private Marzersoft.Themes.ThemedButton btnClient;
        private System.Windows.Forms.Panel panMenu;
        private System.Windows.Forms.Label lblTitle;
        private Marzersoft.Themes.ThemedPanel panClient;
        private System.Windows.Forms.TextBox tbClientAddress;
        private Marzersoft.Themes.ThemedButton btnClientCancel;
        private Marzersoft.Themes.ThemedButton btnClientConnect;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.OpenFileDialog dlgServerOpenExisting;
        private System.Windows.Forms.SaveFileDialog dlgServerCreateNew;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Panel panControls;
        private Marzersoft.Themes.ThemedCaptionBar panCaption;
        private System.Windows.Forms.Panel panBody;
        private System.Windows.Forms.Panel panSplash;
        private System.Windows.Forms.Label lblAbout;
        private System.Windows.Forms.Label lblVersion;
    }
}

