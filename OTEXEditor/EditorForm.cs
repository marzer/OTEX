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
using Marzersoft.Themes;
using System.Net;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.Threading;
using System.IO;

namespace OTEX
{
    public partial class EditorForm : MainForm
    {
        /////////////////////////////////////////////////////////////////////
        // PROPERTIES/VARIABLES
        /////////////////////////////////////////////////////////////////////

        private FastColoredTextBox tbEditor = null;
        private Server otexServer = null;
        private Client otexClient = null;
        private volatile Thread clientConnectingThread = null;

        private bool EditingServerAddressMode
        {
            get { return panClient.Visible; }
            set
            {
                panClient.Visible = value;
                btnClient.Visible = !value;
            }
        }

        private bool PendingConnectionMode
        {
            get { return lblStatus.Visible; }
            set
            {
                lblStatus.Visible = value;
                panControls.Visible = lblAbout.Visible = lblVersion.Visible = !value;
            }
        }

        private bool EditorMode
        {
            get { return tbEditor.Visible; }
            set
            {
                tbEditor.Visible = value;
                panSplash.Visible = !value;
            }
        }

        /////////////////////////////////////////////////////////////////////
        // CONSTRUCTOR
        /////////////////////////////////////////////////////////////////////

        public EditorForm()
        {
            InitializeComponent();
            if (IsDesignMode)
                return;

            //title
            lblTitle.Font = App.Theme.Titles.Large.Regular;

            //splash panel
            panSplash.Dock = DockStyle.Fill;

            //colours
            btnServerNew.Accent = 2;
            btnServerExisting.Accent = 3;
            panClient.BackColor = App.Theme.Background.Light.Colour;
            lblAbout.ForeColor = lblVersion.ForeColor = App.Theme.Background.Light.Colour;

            //about link
            lblAbout.Cursor = Cursors.Hand;
            lblAbout.Font = App.Theme.Controls.Normal.Underline;
            lblAbout.MouseEnter += (s, e) => { lblAbout.ForeColor = App.Theme.Foreground.Mid.Colour; };
            lblAbout.MouseLeave += (s, e) => { lblAbout.ForeColor = App.Theme.Background.Light.Colour; };
            lblAbout.Click += (s, e) => { App.Website.LaunchWebsite(); };

            //version label
            lblVersion.Text = "v" + App.AssemblyVersion.ToString();

            //client connection panel
            btnClient.TextAlign = btnServerNew.TextAlign = btnServerExisting.TextAlign = ContentAlignment.MiddleCenter;
            panClient.Parent = panControls;
            panClient.Location = btnClient.Location;
            btnClientConnect.Image = App.Images.Resource("tick");
            btnClientCancel.Image = App.Images.Resource("close");
            btnClientConnect.ImageAlign = btnClientCancel.ImageAlign = ContentAlignment.MiddleCenter;
            tbClientAddress.Font = App.Theme.Monospaced.Normal.Regular;
            tbClientAddress.BackColor = App.Theme.Background.Light.Colour;
            tbClientAddress.ForeColor = App.Theme.Foreground.BaseColour;

            //'connecting' status label
            lblStatus.Parent = panMenu;

            //edit text box
            tbEditor = new FastColoredTextBox();
            tbEditor.Parent = panBody;
            tbEditor.Dock = DockStyle.Fill;
            tbEditor.BackBrush = App.Theme.Background.Mid.Brush;
            tbEditor.IndentBackColor = App.Theme.Background.Dark.Colour;
            tbEditor.ServiceLinesColor = App.Theme.Background.Light.Colour;
            tbEditor.LineNumberColor = App.Theme.Accent1.Mid.Colour;
            tbEditor.Font = new Font(App.Theme.Monospaced.Normal.Regular.FontFamily, 11.0f);
            tbEditor.TabLength = 4;

            //set initial visibilities
            EditorMode = false;
            PendingConnectionMode = false;
            EditingServerAddressMode = false;

            //form styles
            WindowStyles &= ~(WindowStyles.ThickFrame | WindowStyles.DialogFrame);
            TextFlourishes = false;
            Text = App.Name;

            //create server
            otexServer = new Server();
            otexServer.OnInternalException += (s, e) =>
            {
                Debugger.W("Server: {0}: {1}", e.InnerException.GetType().FullName, e.InnerException.Message);
            };
            otexServer.OnStarted += (s) =>
            {
                Debugger.I("Server: started for {0} on port {1}", s.FilePath, s.Port);
            };
            otexServer.OnClientConnected += (s, id) =>
            {
                Debugger.I("Server: Client {0} connected.", id);
            };
            otexServer.OnClientDisconnected += (s, id) =>
            {
                Debugger.I("Server: Client {0} disconnected.", id);
            };
            otexServer.OnStopped += (s) =>
            {
                Debugger.I("Server: stopped.");
            };
            otexServer.OnFileSynchronized += (s) =>
            {
                Debugger.I("Server: File synchronized.");
            };

            //create client
            otexClient = new Client();
            otexClient.OnInternalException += (c, e) =>
            {
                Debugger.W("Client: {0}: {1}", e.InnerException.GetType().FullName, e.InnerException.Message);
            };
            otexClient.OnConnected += (c) =>
            {
                Debugger.I("Client: connected to {0}:{1}.", c.ServerAddress, c.ServerPort);
            };
            otexClient.OnDisconnected += (c) =>
            {
                Debugger.I("Client: disconnected.");
            };
        }

        /////////////////////////////////////////////////////////////////////
        // SERVER MODE
        /////////////////////////////////////////////////////////////////////

        private void StartServerMode(string filename, bool editMode)
        {
            //start server
            try
            {
                otexServer.Start(filename, editMode);
            }
            catch (Exception exc)
            {
                Debugger.ErrorMessage("An error occurred while starting the server:\n\n{0}: {1}",
                    exc.GetType().Name, exc.Message);
                return;
            }

            //start client
            try
            {
                otexClient.Connect(IPAddress.Loopback);
            }
            catch (Exception exc)
            {
                Debugger.ErrorMessage("An error occurred while connecting:\n\n{0}: {1}",
                    exc.GetType().Name, exc.Message);
                return;
            }

            //started ok, toggle ui to Editor
            EditingServerAddressMode = false;
            EditorMode = true;
            Text = string.Format("Editing {0} (host mode)", Path.GetFileName(otexClient.ServerFilePath));
        }

        /////////////////////////////////////////////////////////////////////
        // CLIENT MODE
        /////////////////////////////////////////////////////////////////////

        private static readonly Regex REGEX_PORT
            = new Regex(@"[:]([0-9]{1,20})$", RegexOptions.Compiled);

        private void StartClientMode(string addressString)
        {
            //parse port
            long port = 55555;
            string originalAddressString = addressString;
            Match m = REGEX_PORT.Match(addressString);
            if (m.Success)
            {
                addressString = REGEX_PORT.Replace(addressString, "").Trim();
                port = long.Parse(m.Groups[1].Value);
                if (port < 1024 || port > 65535)
                {
                    Debugger.ErrorMessage("Server port must be between 1024 and 65535 (inclusive).");
                    return;
                }
            }

            //parse address
            if (addressString.Length == 0)
            {
                Debugger.ErrorMessage("Server address cannot be blank.");
                return;
            }
            IPAddress address;
            if (!IPAddress.TryParse(addressString, out address))
            {
                try
                {
                    IPHostEntry hostEntry = Dns.GetHostEntry(addressString);
                    if (hostEntry.AddressList.Length == 0)
                    {
                        Debugger.ErrorMessage("Could not resolve IP for hostname {0}.", addressString);
                        return;
                    }
                    address = hostEntry.AddressList[0];
                }
                catch (Exception exc)
                {
                    Debugger.ErrorMessage("An error occurred while resolving IP for hostname {0}:\n\n{1}: {2}",
                        addressString, exc.GetType().Name, exc.Message);
                    return;
                }
            }

            //connect to server
            lblStatus.Text = string.Format("Connecting to {0}...", originalAddressString);
            PendingConnectionMode = true;
            clientConnectingThread = new Thread((o) =>
            {
                object[] objs = o as object[];
                var addr = objs[0] as IPAddress;
                var prt = (ushort)objs[1];
                var pwd = objs[2] as Password;
                var saddr = objs[3] as string;
                try
                {
                    otexClient.Connect(addr, prt, pwd);
                }
                catch (Exception exc)
                {
                    Debugger.ErrorMessage("An error occurred while connecting to {0} ({1}:{2}):\n\n{3}: {4}",
                    saddr, addr, prt, exc.GetType().Name, exc.Message);
                    this.Execute(() => { PendingConnectionMode = false; });
                    clientConnectingThread = null;
                    return;
                }

                //started ok, toggle ui to Editor
                this.Execute(() =>
                {
                    EditingServerAddressMode = false;
                    EditorMode = true;
                    Text = string.Format("Editing {0} ({1})", Path.GetFileName(otexClient.ServerFilePath), saddr);
                });

                clientConnectingThread = null;
            });
            clientConnectingThread.IsBackground = false;
            clientConnectingThread.Start(new object[] { address, (ushort)port, null, originalAddressString });
        }

        /////////////////////////////////////////////////////////////////////
        // EVENTS
        /////////////////////////////////////////////////////////////////////

        private void PositionSplashPanel()
        {
            if (panMenu.Visible)
            {
                panMenu.Location = new Point(
                    (panBody.ClientRectangle.Size.Width / 2) - (panMenu.Size.Width / 2),
                    (panBody.ClientRectangle.Size.Height / 2) - (panMenu.Size.Height / 2));
                lblStatus.Bounds = lblStatus.Bounds.AlignCenter(panControls.Bounds.Center());
            }
        }

        protected override void OnFirstShown(EventArgs e)
        {
            base.OnFirstShown(e);
            if (IsDesignMode)
                return;
            PositionSplashPanel();
            Refresh();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (IsDesignMode)
                return;
            PositionSplashPanel();
        }

        private void btnClient_Click(object sender, EventArgs e)
        {
            EditingServerAddressMode = true;
        }

        private void btnClientCancel_Click(object sender, EventArgs e)
        {
            EditingServerAddressMode = false;
        }

        private void btnServerNew_Click(object sender, EventArgs e)
        {
            if (dlgServerCreateNew.ShowDialog() == DialogResult.OK)
                StartServerMode(dlgServerCreateNew.FileName, false);
        }

        private void btnServerExisting_Click(object sender, EventArgs e)
        {
            if (dlgServerOpenExisting.ShowDialog() == DialogResult.OK)
                StartServerMode(dlgServerOpenExisting.FileName, true);
        }

        private void btnClientConnect_Click(object sender, EventArgs e)
        {
            StartClientMode(tbClientAddress.Text.Trim());
        }

        private void EditorForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (otexServer.Running && otexServer.ClientCount > 1 &&
                !Debugger.WarningQuestion("You are currently running in server mode. "
                + "Closing the application will disconnect the other {0} connected users.\n\nClose OTEX Editor?", otexServer.ClientCount-1))
                e.Cancel = true;
        }

        protected override void OnClosed(EventArgs e)
        {
            if (clientConnectingThread != null)
            {
                clientConnectingThread.Join();
                clientConnectingThread = null;
            }

            if (otexClient != null)
            {
                otexClient.Dispose();
                otexClient = null;
            }
            if (otexServer != null)
            {
                otexServer.Dispose();
                otexServer = null;
            }
            base.OnClosed(e);
        }

        private void tbClientAddress_TextChanged(object sender, EventArgs e)
        {
            btnClientConnect.Enabled = tbClientAddress.Text.Trim().Length > 0;
        }

        private void tbClientAddress_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\n' || e.KeyChar == '\r')
            {
                btnClientConnect_Click(this, null);
                e.Handled = true;
            }
        }

        /////////////////////////////////////////////////////////////////////
        // IMPORTS
        /////////////////////////////////////////////////////////////////////

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ReleaseCapture();
    }
}
