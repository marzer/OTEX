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
using System.Linq;
using DiffPlex;
using OTEX.Packets;

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
        private ServerListener otexServerListener = null;
        private volatile Thread clientConnectingThread = null;
        private volatile bool closing = false;
        private volatile string previousText = null;
        private static readonly Differ differ = new Differ();
        private volatile bool processingRemoteChanges = false;
        private FlyoutForm passwordForm = null;

        private bool MainMenuMode
        {
            get { return panMenuPage.Visible; }
            set
            {
                panMenuPage.Visible = value;
                panServerBrowserPage.Visible = !value;
                tbEditor.Visible = !value;
                if (value)
                    PositionMenuPanel();
            }
        }

        private bool ServerBrowserMode
        {
            get { return panServerBrowserPage.Visible; }
            set
            {
                panServerBrowserPage.Visible = value;
                panMenuPage.Visible = !value;
                tbEditor.Visible = !value;
            }
        }

        private bool PendingConnectionMode
        {
            get { return lblStatus.Visible; }
            set
            {
                lblStatus.Visible = value;
                panMenuButtons.Visible = lblAbout.Visible = !value;
            }
        }

        private bool EditorMode
        {
            get { return tbEditor.Visible; }
            set
            {
                tbEditor.Visible = value;
                panMenuPage.Visible = !value;
                panServerBrowserPage.Visible = !value;
                
                
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
            panMenuPage.Dock = DockStyle.Fill;

            //colours
            btnServerNew.Accent = btnServerExisting.Accent = 2;
            btnServerTemporary.Accent = 3;
            lblAbout.ForeColor = lblVersion.ForeColor = App.Theme.Background.Light.Colour;

            //about link
            lblAbout.Cursor = Cursors.Hand;
            lblAbout.Font = App.Theme.Controls.Normal.Underline;
            lblAbout.MouseEnter += (s, e) => { lblAbout.ForeColor = App.Theme.Foreground.Mid.Colour; };
            lblAbout.MouseLeave += (s, e) => { lblAbout.ForeColor = App.Theme.Background.Light.Colour; };
            lblAbout.Click += (s, e) => { App.Website.LaunchWebsite(); };

            //version label
            lblVersion.Text = "v" + Marzersoft.Text.REGEX_VERSION_REPEATING_ZEROES.Replace(App.AssemblyVersion.ToString(),"");
            if (lblVersion.Text.IndexOf('.') == -1)
                lblVersion.Text += ".0";

            //client connection/server browser panel
            btnClient.TextAlign = btnServerNew.TextAlign = btnServerExisting.TextAlign
                = btnServerTemporary.TextAlign = ContentAlignment.MiddleCenter;
            btnClientConnect.Image = App.Images.Resource("tick");
            btnClientCancel.Image = App.Images.Resource("previous");
            btnClientConnect.ImageAlign = btnClientCancel.ImageAlign = ContentAlignment.MiddleCenter;
            tbClientAddress.Font = tbClientPassword.Font = tbServerPassword.Font
                = App.Theme.Monospaced.Normal.Regular;
            tbClientAddress.BackColor = tbClientPassword.BackColor = tbServerPassword.BackColor
                = App.Theme.Background.Light.Colour;
            tbClientAddress.ForeColor = tbClientPassword.ForeColor = tbServerPassword.ForeColor
                = App.Theme.Foreground.BaseColour;
            lblManualEntry.Font = lblServerBrowser.Font = App.Theme.Controls.Large.Regular;
            panServerBrowserPage.Dock = DockStyle.Fill;
            dgvServers.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dgvServers.Columns[1].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dgvServers.ShowCellToolTips = true;

            //'connecting' status label
            lblStatus.Parent = panMenu;

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

            //form styles
            FormBorderStyle = FormBorderStyle.None;
            TextFlourishes = false;
            Text = App.Name;
            CustomTitleBar = true;
            ResizeHandleOverride = true;

            //settings menu
            var button = AddCustomTitleBarButton();
            button.Colour = App.Theme.Accent3.Light.Colour;
            button.Image = App.Images.Resource("settings");

            // CREATE TEXT EDITOR (handles diff calculation) ////////////////////
            tbEditor = new FastColoredTextBox();
            tbEditor.Parent = this;
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

            // CREATE OTEX SERVER ///////////////////////////////////////////////
            otexServer = new Server();
            otexServer.OnThreadException += (s, e) =>
            {
                Logger.W("Server: {0}: {1}", e.InnerException.GetType().FullName, e.InnerException.Message);
            };
            otexServer.OnStarted += (s) =>
            {
                Logger.I("Server: started for {0} on port {1}", s.FilePath, s.Port);
            };
            otexServer.OnClientConnected += (s, id) =>
            {
                Logger.I("Server: Client {0} connected.", id);
            };
            otexServer.OnClientDisconnected += (s, id) =>
            {
                Logger.I("Server: Client {0} disconnected.", id);
            };
            otexServer.OnStopped += (s) =>
            {
                Logger.I("Server: stopped.");
            };
            otexServer.OnFileSynchronized += (s) =>
            {
                Logger.I("Server: File synchronized.");
            };

            // CREATE OTEX CLIENT ///////////////////////////////////////////////
            otexClient = new Client();
            otexClient.OnThreadException += (c, e) =>
            {
                Logger.W("Client: {0}: {1}", e.InnerException.GetType().FullName, e.InnerException.Message);
            };
            otexClient.OnConnected += (c) =>
            {
                Logger.I("Client: connected to {0}:{1}.", c.ServerAddress, c.ServerPort);
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
                    
                    //set up autoscroll prevention
                    var selection = tbEditor.Selection;
                    var selectionStart = Math.Min(tbEditor.PlaceToPosition(selection.Start),
                        tbEditor.PlaceToPosition(selection.End));
                    var selectionEnd = Math.Max(tbEditor.PlaceToPosition(selection.Start),
                        tbEditor.PlaceToPosition(selection.End));

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
                Logger.I("Client: disconnected {0}.", serverSide ? "(connection closed by server)" : "");

                if (!closing)
                {
                    this.Execute(() =>
                    {
                        PendingConnectionMode = false;
                        MainMenuMode = true;
                        Text = App.Name;
                    });
                }
            };

            // CREATE OTEX SERVER LISTENER //////////////////////////////////////
            try
            {
                otexServerListener = new ServerListener();
                otexServerListener.OnThreadException += (sl, e) =>
                {
                    Logger.W("ServerListener: {0}: {1}", e.InnerException.GetType().FullName, e.InnerException.Message);
                };
                otexServerListener.OnServerAdded += (sl, s) =>
                {
                    Logger.I("ServerListener: new server {0}: {1}", s.ID, s.EndPoint);
                    this.Execute(() =>
                    {
                        var row = dgvServers.AddRow(s.Name, s.TemporaryDocument ? "Yes" : "", s.EndPoint.Address,
                            s.EndPoint.Port, s.RequiresPassword ? "Yes" : "", string.Format("{0} / {1}",s.ClientCount, s.MaxClients), 0);
                        row.Tag = s;
                        s.Tag = row;
                    });

                    s.OnUpdated += (sd) =>
                    {
                        Logger.I("ServerDescription: {0} updated.", sd.ID);
                        this.Execute(() =>
                        {
                            (s.Tag as DataGridViewRow).Update(s.Name, s.TemporaryDocument ? "Yes" : "", s.EndPoint.Address,
                                s.EndPoint.Port, s.RequiresPassword ? "Yes" : "", string.Format("{0} / {1}", s.ClientCount, s.MaxClients), 0);
                        });
                    };

                    s.OnInactive += (sd) =>
                    {
                        Logger.I("ServerDescription: {0} inactive.", sd.ID);
                        this.Execute(() =>
                        {
                            var row = (s.Tag as DataGridViewRow);
                            row.DataGridView.Rows.Remove(row);
                            row.Tag = null;
                            s.Tag = null;
                        });
                    };
                };
            }
            catch (Exception exc)
            {
                Logger.ErrorMessage("An error occurred while creating the server listener:\n\n{0}",
                    exc.Message);
                return;
            }
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
                Logger.ErrorMessage("An error occurred while starting the server:\n\n{0}",
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
                Logger.ErrorMessage("An error occurred while connecting:\n\n{0}",
                    exc.Message);
                return;
            }

            //started ok, toggle ui to Editor
            EditorMode = true;
            Text = string.Format("Hosting {0} - {1}",
                otexClient.ServerFilePath.Length == 0 ? "a temporary document" : Path.GetFileName(otexClient.ServerFilePath),
                App.Name);
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

        private bool StartClientMode(string addressString, string passwordString)
        {
            //sanity check
            if (addressString.Length == 0)
            {
                Logger.ErrorMessage("Server address cannot be blank.");
                return false;
            }

            string originalAddressString = addressString;
            long port = 55555;
            IPAddress address = null;
            IPHostEntry hostEntry = null;

            //parse address
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
                Logger.ErrorMessage("An error occurred while parsing address:\n\n{0}", exc.Message);
                return false;
            }

            //all failed
            if (address == null)
            {
                Logger.ErrorMessage("Failed to parse a valid address from {0}.", originalAddressString);
                return false;
            }

            //validate port
            if (port < 1024 || port > 65535)
            {
                Logger.ErrorMessage("Port must be between 1024 and 65535 (inclusive).");
                return false;
            }

            return StartClientMode(new IPEndPoint(address, (int)port), passwordString);
        }

        private bool StartClientMode(IPEndPoint endPoint, string passwordString)
        {
            //validate port
            if (endPoint.Port < 1024 || endPoint.Port > 65535)
            {
                Logger.ErrorMessage("Port must be between 1024 and 65535 (inclusive).");
                return false;
            }

            //validate password
            var originalPasswordLength = passwordString.Length;
            passwordString = (passwordString ?? "").Trim();
            if (originalPasswordLength > 0 && passwordString.Length == 0)
            {
                Logger.ErrorMessage("Passwords cannot be entirely whitespace.");
                return false;
            }
            Password password = null;
            if (passwordString.Length > 0)
            {
                try
                {
                    password = new Password(passwordString);
                }
                catch (Exception exc)
                {
                    Logger.ErrorMessage("An error occurred while parsing password:\n\n{0}", exc.Message);
                    return false;
                }
            }

            //connect to server
            lblStatus.Text = string.Format("Connecting to {0}...", endPoint);
            PendingConnectionMode = true;
            clientConnectingThread = new Thread((o) =>
            {
                object[] objs = o as object[];
                var ep = objs[0] as IPEndPoint;
                var pwd = objs[1] as Password;
                try
                {
                    otexClient.Connect(ep, pwd);
                }
                catch (Exception exc)
                {
                    Logger.ErrorMessage("An error occurred while connecting to {0}:\n\n{1}",
                    ep, exc.Message);
                    this.Execute(() => { PendingConnectionMode = false; });
                    clientConnectingThread = null;
                    return;
                }

                //started ok, toggle ui to Editor
                this.Execute(() =>
                {
                    PendingConnectionMode = false;
                    EditorMode = true;
                    Text = string.Format("Editing {0} ({1}) - {2}",
                        otexClient.ServerFilePath.Length == 0 ? "a temporary document" : Path.GetFileName(otexClient.ServerFilePath),
                        ep, App.Name);
                });

                clientConnectingThread = null;
            });
            clientConnectingThread.IsBackground = false;
            clientConnectingThread.Start(new object[] { endPoint, password  });
            return true;
        }

        /////////////////////////////////////////////////////////////////////
        // UI EVENTS
        /////////////////////////////////////////////////////////////////////

        private void PositionMenuPanel()
        {
            if (panMenu.Visible)
            {
                panMenu.Location = new Point(
                    (panMenu.Parent.ClientRectangle.Size.Width / 2) - (panMenu.Size.Width / 2),
                    (panMenu.Parent.ClientRectangle.Size.Height / 2) - (panMenu.Size.Height / 2));
                lblStatus.Bounds = lblStatus.Bounds.AlignCenter(panMenuButtons.Bounds.Center());
            }
        }

        protected override void OnFirstShown(EventArgs e)
        {
            base.OnFirstShown(e);
            if (IsDesignMode)
                return;
            PendingConnectionMode = false;
            MainMenuMode = true;
            Refresh();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (IsDesignMode)
                return;
            PositionMenuPanel();
        }

        private void btnClient_Click(object sender, EventArgs e)
        {
            ServerBrowserMode = true;
        }

        private void btnClientCancel_Click(object sender, EventArgs e)
        {
            MainMenuMode = true;
        }

        private void btnServerNew_Click(object sender, EventArgs e)
        {
            if (dlgServerCreateNew.ShowDialog() == DialogResult.OK)
                StartServerMode(new Server.StartParams()
                    { FilePath = dlgServerCreateNew.FileName, EditMode = false,
                    Public = true });
        }

        private void btnServerExisting_Click(object sender, EventArgs e)
        {
            if (dlgServerOpenExisting.ShowDialog() == DialogResult.OK)
                StartServerMode(new Server.StartParams()
                    { FilePath = dlgServerOpenExisting.FileName, EditMode = true,
                    Public = true
                });
        }

        private void btnServerTemporary_Click(object sender, EventArgs e)
        {
            StartServerMode(new Server.StartParams()
                { FilePath = "", Public = true });
        }

        private void btnClientConnect_Click(object sender, EventArgs e)
        {
            StartClientMode(tbClientAddress.Text.Trim(), tbClientPassword.Text.Trim());
        }

        private void EditorForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (otexServer.Running && otexServer.ClientCount > 1 &&
                !Logger.WarningQuestion("You are currently running in server mode. "
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
            if (otexServerListener != null)
            {
                otexServerListener.Dispose();
                otexServerListener = null;
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
            if (passwordForm != null)
            {
                passwordForm.Dispose();
                passwordForm = null;
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

        private void dgvServers_CellContentDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex < 0 || e.RowIndex < 0)
                return;
            var sd = (dgvServers[e.ColumnIndex, e.RowIndex].OwningRow.Tag as ServerDescription);
            if (sd != null)
            {
                if (sd.RequiresPassword)
                {
                    if (passwordForm == null)
                        passwordForm = new FlyoutForm(panServerPassword);
                    passwordForm.Tag = sd;
                    passwordForm.Flyout();
                }
                else
                    StartClientMode(sd.EndPoint, "");
            }
        }

        private void tbServerPassword_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (passwordForm == null || passwordForm.Tag == null)
                return;

            if (e.KeyChar == '\n' || e.KeyChar == '\r')
            {
                if (tbServerPassword.Text.Length > 0)
                {
                    var pw = tbServerPassword.Text;
                    tbServerPassword.Text = "";
                    if (StartClientMode((passwordForm.Tag as ServerDescription).EndPoint, pw))
                        this.Activate();
                }
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
