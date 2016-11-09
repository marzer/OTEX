using Marzersoft;
using Marzersoft.Forms;
using System;
using System.Drawing;
using System.Windows.Forms;
using FastColoredTextBoxNS;
using System.Net;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.Threading;
using System.IO;
using DiffPlex;

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
        private volatile bool closing = false;
        private volatile string previousText = null;
        private static readonly Differ differ = new Differ();
        private volatile bool processingRemoteChanges = false;

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
                if (!value)
                    PositionSplashPanel();
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
            lblVersion.Text = "v" + Marzersoft.Text.REGEX_VERSION_REPEATING_ZEROES.Replace(App.AssemblyVersion.ToString(),"");

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
            tbEditor.CurrentLineColor = App.Theme.Background.LightLight.Colour;
            tbEditor.SelectionColor = App.Theme.Accent1.Mid.Colour;
            tbEditor.Font = new Font(App.Theme.Monospaced.Normal.Regular.FontFamily, 11.0f);
            tbEditor.WordWrap = true;
            tbEditor.WordWrapAutoIndent = true;
            tbEditor.WordWrapMode = WordWrapMode.WordWrapControlWidth;
            tbEditor.TabLength = 4;
            tbEditor.LineInterval = 2;
            tbEditor.AllowMacroRecording = false;
            tbEditor.CaretColor = App.Theme.Accent1.LightLight.Colour;
            tbEditor.TextChanging += (sender, args) =>
            {
                if (processingRemoteChanges)
                    return;

                previousText = tbEditor.Text;
            };
            tbEditor.TextChanged += (sender, args) =>
            {
                if (processingRemoteChanges)
                    return;

                //do diff on two versions of text
                var currentText = tbEditor.Text;
                var diffs = differ.CreateCharacterDiffs(previousText, currentText, false, false);

                //report changes
                int position = 0;
                foreach (var diff in diffs.DiffBlocks)
                {
                    //skip unchanged characters
                    position = Math.Min(diff.InsertStartB, currentText.Length);

                    //process a deletion
                    if (diff.DeleteCountA > 0)
                        otexClient.Delete((uint)position, (uint)diff.DeleteCountA);

                    //process an insertion
                    if (position < (diff.InsertStartB + diff.InsertCountB))
                        otexClient.Insert((uint)position, currentText.Substring(position, diff.InsertCountB));
                }
            };

            //file dialog filters
            FileFilterFactory filterFactory = new FileFilterFactory();
            filterFactory.Add("Text files", "txt");
            filterFactory.Add("C# files", "cs");
            filterFactory.Add("C/C++ files", "cpp", "h", "hpp", "cxx", "cc", "c", "inl", "inc", "rc", "hxx");
            filterFactory.Add("Log files", "log");
            filterFactory.Add("Javacode files", "java");
            filterFactory.Add("Javascript files", "js");
            filterFactory.Add("Visual Basic files", "vb", "vbs");
            filterFactory.Add("Web files", "htm", "html", "xml", "css", "htaccess", "php");
            filterFactory.Add("XML files", "xml", "xsl", "xslt", "xsd", "dtd");
            filterFactory.Add("PHP scripts", "php");
            filterFactory.Add("SQL scripts", "sql");
            filterFactory.Add("Luascript files", "lua");
            filterFactory.Add("Shell scripts", "bat", "sh", "ps");
            filterFactory.Add("Settings files", "ini", "config", "cfg", "conf", "reg");
            filterFactory.Add("Shader files", "hlsl", "glsl", "fx", "csh", "cshader", "dsh", "dshader",
                "gsh", "gshader", "hlsli", "hsh", "hshader", "psh", "pshader", "vsh", "vshader");
            filterFactory.Apply(dlgServerCreateNew);
            filterFactory.Apply(dlgServerOpenExisting);

            //set initial visibilities
            EditorMode = false;
            PendingConnectionMode = false;
            EditingServerAddressMode = false;

            //form styles
            WindowStyles &= ~(WindowStyles.ThickFrame | WindowStyles.DialogFrame);
            //ControlBox = false;
            //FormBorderStyle = FormBorderStyle.SizableToolWindow;
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
                this.Execute(() =>
                {
                    processingRemoteChanges = true;
                    tbEditor.Text = "";
                    processingRemoteChanges = false;

                    string ext = Path.GetExtension(c.ServerFilePath).ToLower();
                    if (ext.Length > 0 && (ext = ext.Substring(1)).Length > 0)
                    {
                        switch (ext)
                        {
                            case "cs": tbEditor.Language = Language.CSharp; break;
                            case "htm": tbEditor.Language = Language.HTML; break;
                            case "html": tbEditor.Language = Language.HTML; break;
                            case "js": tbEditor.Language = Language.JS; break;
                            case "lua": tbEditor.Language = Language.Lua; break;
                            case "php": tbEditor.Language = Language.PHP; break;
                            case "sql": tbEditor.Language = Language.SQL; break;
                            case "vb": tbEditor.Language = Language.VB; break;
                            case "vbs": tbEditor.Language = Language.VB; break;
                            case "xml": tbEditor.Language = Language.XML; break;
                            default: tbEditor.Language = Language.Custom; break;
                        }
                    }
                    else
                        tbEditor.Language = Language.Custom;
                }, false);
            };
            otexClient.OnRemoteOperations += (c,operations) =>
            {
                this.Execute(() => //using Execute() ensures this happens on the main thread (editing is blocked)
                {
                    processingRemoteChanges = true;
                    foreach (var operation in operations)
                    {
                        if (operation.IsInsertion)
                        {
                            tbEditor.InsertTextAndRestoreSelection(
                                new Range(tbEditor, tbEditor.PositionToPlace(operation.Offset),
                                    tbEditor.PositionToPlace(operation.Offset)),
                                operation.Text, null);
                        }
                        else if (operation.IsDeletion)
                        {
                            tbEditor.InsertTextAndRestoreSelection(
                                new Range(tbEditor, tbEditor.PositionToPlace(operation.Offset),
                                    tbEditor.PositionToPlace(operation.Offset + operation.Length)),
                                "", null);
                        }
                    }
                    processingRemoteChanges = false;
                }, false);
            };
            otexClient.OnDisconnected += (c, serverSide) =>
            {
                Debugger.I("Client: disconnected {0}.", serverSide ? "(connection closed by server)" : "");

                if (!closing)
                {
                    this.Execute(() =>
                    {
                        PendingConnectionMode = false;
                        EditorMode = false;
                        Text = App.Name;
                    });
                }
            };
        }

        /////////////////////////////////////////////////////////////////////
        // SERVER MODE
        /////////////////////////////////////////////////////////////////////

        private void StartServerMode(Server.StartParams startParams)
        {
            //start server
            try
            {
                otexServer.Start(startParams);
            }
            catch (Exception exc)
            {
                Debugger.ErrorMessage("An error occurred while starting the server:\n\n{0}",
                    exc.Message);
                return;
            }

            //start client
            try
            {
                otexClient.Connect(IPAddress.Loopback);
            }
            catch (Exception exc)
            {
                Debugger.ErrorMessage("An error occurred while connecting:\n\n{0}",
                    exc.Message);
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

        private static readonly Regex REGEX_IPV4_AND_PORT = new Regex(
            @"^\s*"
            //ipv4 octets
            + @"(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?[.]"
            + @"25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?[.]"
            + @"25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?[.]"
            + @"25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)"
            //port (optional)
            + @"(?:[:]\s*([0-9]*))?"
            //
            + @"\s*$"
            , RegexOptions.Compiled);

        private static readonly Regex REGEX_IPV6 = new Regex(
            @"^\s*("
            //ipv6 octets
            + @"[a-fA-F0-9:]+"
            //ipv4 suffix (optional)
            + @"(?:[:]25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?[.]"
            + @"25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?[.]"
            + @"25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?[.]"
            + @"25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)?"
            //
            + @")\s*$"
            , RegexOptions.Compiled);

        private static readonly Regex REGEX_IPV6_AND_PORT = new Regex(
            @"^\s*\[\s*("
            //ipv6 octets
            + @"[a-fA-F0-9:]+"
            //ipv4 suffix (optional)
            + @"(?:[:]25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?[.]"
            + @"25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?[.]"
            + @"25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?[.]"
            + @"25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)?"
            //port (optional)
            + @")\s*\](?:[:]\s*([0-9]*))?\s*$"
            , RegexOptions.Compiled);

        private static readonly Regex REGEX_PORT = new Regex(@"\s*(?:[:]\s*([0-9]*))\s*$",
            RegexOptions.Compiled);

        private void StartClientMode(string addressString)
        {
            //sanity check
            if (addressString.Length == 0)
            {
                Debugger.ErrorMessage("Server address cannot be blank.");
                return;
            }

            string originalAddressString = addressString;
            long port = 55555;
            IPAddress address = null;
            IPHostEntry hostEntry = null;

            //parse
            try
            {
                //check for ipv4 address
                Match m = null; bool success = false;
                if (success = (m = REGEX_IPV4_AND_PORT.Match(addressString)).Success)
                {
                    address = IPAddress.Parse(m.Groups[1].Value);
                    if (m.Groups.Count >= 2 && m.Groups[2].Value != null)
                        port = long.Parse(m.Groups[2].Value);
                }

                //check for ipv6 address (without port)
                if (!success && (success = (m = REGEX_IPV6.Match(addressString)).Success))
                    address = IPAddress.Parse(m.Groups[1].Value);

                //check for ipv6 address in bracket notation (possibly with port)
                if (!success && (success = (m = REGEX_IPV6_AND_PORT.Match(addressString)).Success))
                {
                    address = IPAddress.Parse(m.Groups[1].Value);
                    if (m.Groups.Count >= 2 && m.Groups[2].Value != null)
                        port = long.Parse(m.Groups[2].Value);
                }

                //check hostnames
                if (!success)
                {
                    //remove port first
                    if ((m = REGEX_PORT.Match(addressString)).Success)
                    {
                        port = long.Parse(m.Groups[1].Value);
                        addressString = REGEX_PORT.Replace(addressString, "").Trim();
                    }

                    //try to resolve a hostname
                    success = (address = ((hostEntry = Dns.GetHostEntry(addressString)) != null
                        && hostEntry.AddressList.Length > 0) ? hostEntry.AddressList[0] : null) != null;
                }
            }
            catch (Exception exc)
            {
                Debugger.ErrorMessage("An error occurred while parsing address:\n\n{0}", exc.Message);
                return;
            }

            //all failed
            if (address == null)
            {
                Debugger.ErrorMessage("Failed to parse a valid address from {0}.", originalAddressString);
                return;
            }

            //check port
            if (port < 1024 || port > 65535)
            {
                Debugger.ErrorMessage("Port must be between 1024 and 65535 (inclusive).");
                return;
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
                    Debugger.ErrorMessage("An error occurred while connecting to {0} ({1}:{2}):\n\n{3}",
                    saddr, addr, prt, exc.Message);
                    this.Execute(() => { PendingConnectionMode = false; });
                    clientConnectingThread = null;
                    return;
                }

                //started ok, toggle ui to Editor
                this.Execute(() =>
                {
                    EditorMode = true;
                    Text = string.Format("Editing {0} ({1})", Path.GetFileName(otexClient.ServerFilePath), saddr);
                });

                clientConnectingThread = null;
            });
            clientConnectingThread.IsBackground = false;
            clientConnectingThread.Start(new object[] { address, (ushort)port, null, originalAddressString });
        }

        /////////////////////////////////////////////////////////////////////
        // UI EVENTS
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
                StartServerMode(new Server.StartParams()
                    { Path = dlgServerCreateNew.FileName, EditMode = false, Announce = true });
        }

        private void btnServerExisting_Click(object sender, EventArgs e)
        {
            if (dlgServerOpenExisting.ShowDialog() == DialogResult.OK)
                StartServerMode(new Server.StartParams()
                    { Path = dlgServerOpenExisting.FileName, EditMode = true, Announce = true });
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
            closing = true;
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
