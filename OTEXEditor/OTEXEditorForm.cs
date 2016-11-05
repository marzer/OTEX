using Marzersoft;
using Marzersoft.Forms;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using FastColoredTextBoxNS;

namespace OTEX
{
    public partial class OTEXEditorForm : MainForm
    {
        private FastColoredTextBox tbEditor = null;
        private OTEXServer server = null;

        public OTEXEditorForm()
        {
            InitializeComponent();
            if (IsDesignMode)
                return;

            //title
            lblTitle.Font = App.Theme.Titles.Large.Regular;

            //client connection panel
            btnClient.TextAlign = btnServerNew.TextAlign = btnServerExisting.TextAlign = ContentAlignment.MiddleCenter;
            panClient.Parent = panControls;
            panClient.Location = btnClient.Location;
            panClient.Visible = false;
            btnClientConnect.Image = App.Images.Resource("tick");
            btnClientCancel.Image = App.Images.Resource("close");

            //'connecting' panel
            panConnecting.Parent = panSplash;
            panConnecting.Visible = false;

            //edit text box
            tbEditor = new FastColoredTextBox();
            tbEditor.Parent = this;
            tbEditor.Dock = DockStyle.Fill;
            tbEditor.Visible = false;
        }

        private void PositionSplashPanel()
        {
            if (panSplash.Visible)
            {
                panSplash.Location = new Point(
                    (ClientRectangle.Size.Width / 2) - (panSplash.Size.Width / 2),
                    (ClientRectangle.Size.Height / 2) - (panSplash.Size.Height / 2));
            }
        }

        protected override void OnFirstShown(EventArgs e)
        {
            base.OnFirstShown(e);
            PositionSplashPanel();
        }

        private void OTEXEditorForm_Resize(object sender, EventArgs e)
        {
            PositionSplashPanel();
        }

        private void btnClient_Click(object sender, EventArgs e)
        {
            btnClient.Visible = false;
            panClient.Visible = true;
        }

        private void btnClientCancel_Click(object sender, EventArgs e)
        {
            panClient.Visible = false;
            btnClient.Visible = true;
        }

        private void btnServerNew_Click(object sender, EventArgs e)
        {
            if (dlgServerCreateNew.ShowDialog() == DialogResult.OK)
            {
                panControls.Visible = false;
                panConnecting.Location = panSplash.RectangleToClient(panControls.RectangleToScreen(btnClient.Bounds)).Location;
                panConnecting.Visible = true;
                //tbEditor.Visible = true;
            }
        }

        private void btnServerExisting_Click(object sender, EventArgs e)
        {
            if (dlgServerOpenExisting.ShowDialog() == DialogResult.OK)
            {
                panControls.Visible = false;
                panConnecting.Location = panSplash.RectangleToClient(panControls.RectangleToScreen(btnClient.Bounds)).Location;
                panConnecting.Visible = true;
                //tbEditor.Visible = true;
            }
        }

        private void btnClientConnect_Click(object sender, EventArgs e)
        {
            panControls.Visible = false;
            panConnecting.Location = panSplash.RectangleToClient(panControls.RectangleToScreen(btnClient.Bounds)).Location;
            panConnecting.Visible = true;
            //tbEditor.Visible = true;
        }
    }
}
