namespace OTEX
{
    partial class OTEXEditorForm
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
            this.panSplash = new System.Windows.Forms.Panel();
            this.panControls = new System.Windows.Forms.Panel();
            this.lblTitle = new System.Windows.Forms.Label();
            this.panClient = new Marzersoft.Themes.ThemedPanel();
            this.btnClientCancel = new Marzersoft.Themes.ThemedButton();
            this.btnClientConnect = new Marzersoft.Themes.ThemedButton();
            this.label1 = new System.Windows.Forms.Label();
            this.tbClientAddress = new System.Windows.Forms.TextBox();
            this.dlgServerOpenExisting = new System.Windows.Forms.OpenFileDialog();
            this.dlgServerCreateNew = new System.Windows.Forms.SaveFileDialog();
            this.panConnecting = new System.Windows.Forms.Panel();
            this.pbConnecting = new System.Windows.Forms.ProgressBar();
            this.label2 = new System.Windows.Forms.Label();
            this.panSplash.SuspendLayout();
            this.panControls.SuspendLayout();
            this.panClient.SuspendLayout();
            this.panConnecting.SuspendLayout();
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
            this.btnServerExisting.TabIndex = 0;
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
            this.btnServerNew.TabIndex = 1;
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
            // panSplash
            // 
            this.panSplash.Controls.Add(this.panControls);
            this.panSplash.Controls.Add(this.lblTitle);
            this.panSplash.Location = new System.Drawing.Point(105, 57);
            this.panSplash.Name = "panSplash";
            this.panSplash.Size = new System.Drawing.Size(279, 272);
            this.panSplash.TabIndex = 3;
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
            this.panClient.Location = new System.Drawing.Point(447, 171);
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
            this.btnClientCancel.Location = new System.Drawing.Point(237, 0);
            this.btnClientCancel.Margin = new System.Windows.Forms.Padding(0);
            this.btnClientCancel.Name = "btnClientCancel";
            this.btnClientCancel.Size = new System.Drawing.Size(28, 48);
            this.btnClientCancel.TabIndex = 5;
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
            this.btnClientConnect.Location = new System.Drawing.Point(204, 0);
            this.btnClientConnect.Margin = new System.Windows.Forms.Padding(0);
            this.btnClientConnect.Name = "btnClientConnect";
            this.btnClientConnect.Size = new System.Drawing.Size(28, 48);
            this.btnClientConnect.TabIndex = 4;
            this.btnClientConnect.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnClientConnect.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnClientConnect.UseVisualStyleBackColor = true;
            this.btnClientConnect.Click += new System.EventHandler(this.btnClientConnect_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(0, 4);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(45, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "Address";
            // 
            // tbClientAddress
            // 
            this.tbClientAddress.Location = new System.Drawing.Point(3, 20);
            this.tbClientAddress.Name = "tbClientAddress";
            this.tbClientAddress.Size = new System.Drawing.Size(198, 20);
            this.tbClientAddress.TabIndex = 0;
            this.tbClientAddress.Text = "127.0.0.1";
            // 
            // dlgServerOpenExisting
            // 
            this.dlgServerOpenExisting.DefaultExt = "txt";
            this.dlgServerOpenExisting.FileName = "dlgServerOpenExisting";
            this.dlgServerOpenExisting.Filter = "Plain-text files|*.txt";
            this.dlgServerOpenExisting.Title = "Select an existing file to collaboratively edit";
            // 
            // dlgServerCreateNew
            // 
            this.dlgServerCreateNew.DefaultExt = "txt";
            this.dlgServerCreateNew.Filter = "Plain-text files|*.txt";
            this.dlgServerCreateNew.Title = "Select a new file to create collaboratively";
            // 
            // panConnecting
            // 
            this.panConnecting.Controls.Add(this.pbConnecting);
            this.panConnecting.Controls.Add(this.label2);
            this.panConnecting.Location = new System.Drawing.Point(445, 61);
            this.panConnecting.Margin = new System.Windows.Forms.Padding(0);
            this.panConnecting.Name = "panConnecting";
            this.panConnecting.Size = new System.Drawing.Size(267, 48);
            this.panConnecting.TabIndex = 6;
            // 
            // pbConnecting
            // 
            this.pbConnecting.Location = new System.Drawing.Point(0, 20);
            this.pbConnecting.MarqueeAnimationSpeed = 10;
            this.pbConnecting.Name = "pbConnecting";
            this.pbConnecting.Size = new System.Drawing.Size(267, 23);
            this.pbConnecting.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
            this.pbConnecting.TabIndex = 3;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(0, 4);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(70, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "Connecting...";
            // 
            // OTEXEditorForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(837, 365);
            this.Controls.Add(this.panConnecting);
            this.Controls.Add(this.panClient);
            this.Controls.Add(this.panSplash);
            this.MinimumSize = new System.Drawing.Size(500, 400);
            this.Name = "OTEXEditorForm";
            this.Text = "Form1 - Program (Administrator) (Debug build)";
            this.Resize += new System.EventHandler(this.OTEXEditorForm_Resize);
            this.panSplash.ResumeLayout(false);
            this.panControls.ResumeLayout(false);
            this.panClient.ResumeLayout(false);
            this.panClient.PerformLayout();
            this.panConnecting.ResumeLayout(false);
            this.panConnecting.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion
        private Marzersoft.Themes.ThemedButton btnServerNew;
        private Marzersoft.Themes.ThemedButton btnServerExisting;
        private Marzersoft.Themes.ThemedButton btnClient;
        private System.Windows.Forms.Panel panSplash;
        private System.Windows.Forms.Label lblTitle;
        private Marzersoft.Themes.ThemedPanel panClient;
        private System.Windows.Forms.TextBox tbClientAddress;
        private Marzersoft.Themes.ThemedButton btnClientCancel;
        private Marzersoft.Themes.ThemedButton btnClientConnect;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.OpenFileDialog dlgServerOpenExisting;
        private System.Windows.Forms.SaveFileDialog dlgServerCreateNew;
        private System.Windows.Forms.Panel panConnecting;
        private System.Windows.Forms.ProgressBar pbConnecting;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Panel panControls;
    }
}

