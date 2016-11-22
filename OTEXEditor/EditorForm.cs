using Marzersoft;
using Marzersoft.Forms;
using System;
using System.Drawing;
using System.Windows.Forms;
using FastColoredTextBoxNS;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Marzersoft.Themes;
using Marzersoft.Controls;

namespace OTEX
{
    public partial class EditorForm : MainForm
    {
        /////////////////////////////////////////////////////////////////////
        // PROPERTIES/VARIABLES
        /////////////////////////////////////////////////////////////////////

        private EditorTextBox tbEditor = null;
        private Server otexServer = null;
        private Client otexClient = null;
        private ServerListener otexServerListener = null;
        private volatile Thread clientConnectingThread = null;
        private volatile bool closing = false;
        private FlyoutForm passwordForm = null, settingsForm = null;
        private volatile IPEndPoint lastConnectionEndpoint = null;
        private volatile Password lastConnectionPassword = null;
        private volatile bool lastConnectionReturnToServerBrowser = false;
        private TitleBarButton logoutButton = null, settingsButton;
        private volatile bool settingsLoaded = false;
        private Paginator paginator = null;
        private readonly User localUser;
        private readonly Dictionary<Guid, User> remoteUsers
            = new Dictionary<Guid, User>();
        
        /////////////////////////////////////////////////////////////////////
        // CONSTRUCTOR
        /////////////////////////////////////////////////////////////////////

        public EditorForm()
        {
            InitializeComponent();
            if (IsDesignMode)
                return;

            // CONFIGURE MAIN MENU /////////////////////////////////////////////////////////////////
            //button text alignment
            btnClient.TextAlign = btnServerNew.TextAlign = btnServerExisting.TextAlign
                = btnServerTemporary.TextAlign = ContentAlignment.MiddleCenter;
            //about label
            lblAbout.MouseEnter += (s, e) => { lblAbout.ForeColor = App.Theme.Foreground.Colour; };
            lblAbout.MouseLeave += (s, e) => { lblAbout.ForeColor = App.Theme.Foreground.LowContrast.Colour; };
            lblAbout.Click += (s, e) => { App.Website.LaunchWebsite(); };
            //version label
            lblVersion.Text = "v" + Marzersoft.Text.REGEX_VERSION_REPEATING_ZEROES.Replace(App.AssemblyVersion.ToString(),"");
            if (lblVersion.Text.IndexOf('.') == -1)
                lblVersion.Text += ".0";
            //file dialog filters
            FileFilterFactory filterFactory = new FileFilterFactory();
            filterFactory.Add("Text files", "txt");
            filterFactory.Add("C# files", "cs");
            filterFactory.Add("C/C++ files", "cpp", "h", "hpp", "cxx", "cc", "c", "inl", "inc", "rc", "hxx");
            filterFactory.Add("Log files", "log");
            filterFactory.Add("Java files", "java");
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

            // CONFIGURE SERVER BROWSER ////////////////////////////////////////////////////////////
            btnClientConnect.Image = App.Images.Resource("next");
            btnClientCancel.Image = App.Images.Resource("previous");
            dgvServers.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dgvServers.Columns[1].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;

            // CONFIGURE "CONNECTING" PAGE /////////////////////////////////////////////////////////
            lblConnectingStatus.Width = panConnectingPage.ClientSize.Width - 10;
            btnConnectingReconnect.Image = App.Images.Resource("refresh");
            btnConnectingBack.Image = App.Images.Resource("previous");

            // CONFIGURE FORM TITLEBAR /////////////////////////////////////////////////////////////
            //text
            Text = App.Name;
            //settings menu
            settingsForm = new FlyoutForm(panSettings);
            settingsForm.Accent = 1;
            settingsButton = AddCustomTitleBarButton();
            settingsButton.Click += (b) => { settingsForm.Flyout(PointToScreen(b.Bounds.BottomMiddle())); };
            //logout button
            logoutButton = AddCustomTitleBarButton();
            logoutButton.Colour = ColorTranslator.FromHtml("#DF3F26");
            logoutButton.Visible = false;
            logoutButton.Click += (b) =>
            {
                if (otexServer.Running && otexServer.ClientCount > 1 &&
                    !Logger.WarningQuestion("You are currently running in server mode. "
                    + "Leaving the session will disconnect the other {0} connected users.\n\nLeave session?", otexServer.ClientCount - 1))
                    return;

                otexClient.Disconnect();
                otexServer.Stop();
            };

            // CREATE OTEX SERVER //////////////////////////////////////////////////////////////////
            otexServer = new Server();
            otexServer.OnThreadException += (s, e) =>
            {
                Logger.W("Server: {0}: {1}", e.InnerException.GetType().FullName, e.InnerException.Message);
            };
            otexServer.OnStarted += (s) =>
            {
                Logger.I("Server: started for {0} on port {1}", s.FilePath.Length > 0 ? s.FilePath : "a temporary document", s.Port);
            };
            otexServer.OnClientConnected += (s, id) =>
            {
                Logger.I("Server: client {0} connected.", id);
            };
            otexServer.OnClientDisconnected += (s, id) =>
            {
                Logger.I("Server: client {0} disconnected.", id);
            };
            otexServer.OnStopped += (s) =>
            {
                Logger.I("Server: stopped.");
            };
            otexServer.OnFileSynchronized += (s) =>
            {
                Logger.I("Server: file synchronized.");
            };

            // CREATE OTEX CLIENT //////////////////////////////////////////////////////////////////
            otexClient = new Client();
            otexClient.OnThreadException += (c, e) =>
            {
                Logger.W("Client: {0}: {1}", e.InnerException.GetType().Name, e.InnerException.Message);
            };
            otexClient.OnConnected += (c) =>
            {
                Logger.I("Client: connected to {0}:{1}.", c.ServerAddress, c.ServerPort);
                this.Execute(() =>
                {
                    tbEditor.DiffGeneration = false;
                    tbEditor.Text = "";
                    tbEditor.DiffGeneration = true;

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

                    logoutButton.Visible = true;
                    paginator.ActivePageKey = "editor";
                    tbEditor.Focus();
                }, false);
            };
            otexClient.OnRemoteOperations += (c,operations) =>
            {
                this.Execute(() =>
                {
                    tbEditor.DiffGeneration = false;

                    foreach (var operation in operations)
                    {
                        if (operation.IsInsertion)
                        {
                            tbEditor.InsertTextAndRestoreSelection(
                                new Range(tbEditor, tbEditor.PositionToPlace(operation.Offset),
                                    tbEditor.PositionToPlace(operation.Offset)),
                                operation.Text, null, false);
                        }
                        else if (operation.IsDeletion)
                        {
                            tbEditor.InsertTextAndRestoreSelection(
                                new Range(tbEditor, tbEditor.PositionToPlace(operation.Offset),
                                    tbEditor.PositionToPlace(operation.Offset + operation.Length)),
                                "", null, false);
                        }
                    }

                    tbEditor.DiffGeneration = true;
                }, false);
            };
            otexClient.OnRemoteMetadata += (c, id, md) =>
            {
                if (md != null)
                {
                    lock (remoteUsers)
                    {
                        User remoteUser = null;
                        if (remoteUsers.TryGetValue(id, out remoteUser))
                            remoteUser.Update(md);
                        else
                        {
                            remoteUsers[id] = remoteUser = new User(id, md);
                            remoteUser.OnColourChanged += (u) =>
                            {
                                this.Execute(() => { tbEditor.Refresh(); });
                            };
                            remoteUser.OnSelectionChanged += (u) =>
                            {
                                this.Execute(() => { tbEditor.Refresh(); });
                            };
                            this.Execute(() => { tbEditor.Refresh(); });
                        }
                    }   
                }
            };
            otexClient.OnDisconnected += (c, serverSide) =>
            {
                Logger.I("Client: disconnected{0}.", serverSide ? " (connection closed by server)" : "");
                lock (remoteUsers)
                {
                    remoteUsers.Clear();
                }
                localUser.SetSelection(0,0);
                if (!closing)
                {
                    this.Execute(() =>
                    {
                        //client mode (in server mode, client always disconnects first)
                        if (serverSide)
                        {
                            if (lastConnectionEndpoint != null)
                            {
                                lblConnectingStatus.Text = string.Format("Connection to {0} was lost.",
                                    lastConnectionEndpoint);
                                btnConnectingBack.Visible = true;
                                btnConnectingReconnect.Visible = true;
                                paginator.ActivePageKey = "connecting";
                            }
                            else
                                paginator.ActivePageKey = "menu";
                        }
                        else
                            paginator.ActivePageKey = "menu";

                        Text = App.Name;
                        logoutButton.Visible = false;
                        tbEditor.Text = "";
                    });
                }
            };

            // CREATE OTEX SERVER LISTENER /////////////////////////////////////////////////////////
            try
            {
                otexServerListener = new ServerListener();
                otexServerListener.OnThreadException += (sl, e) =>
                {
                    Logger.W("ServerListener: {0}: {1}", e.InnerException.GetType().FullName, e.InnerException.Message);
                };
                otexServerListener.OnServerAdded += (sl, s) =>
                {
                    if (s.ID.Equals(otexServer.ID)) //ignore self
                        return;

                    Logger.I("ServerListener: new server {0}: {1}", s.ID, s.EndPoint);
                    this.Execute(() =>
                    {
                        var row = dgvServers.AddRow(s.Name.Length > 0 ? s.Name : "OTEX Server", s.TemporaryDocument ? "Yes" : "", s.EndPoint.Address,
                            s.EndPoint.Port, s.RequiresPassword ? "Yes" : "", string.Format("{0} / {1}",s.ClientCount, s.MaxClients), 0);
                        row.Tag = s;
                        s.Tag = row;
                    });

                    s.OnUpdated += (sd) =>
                    {
                        Logger.I("ServerDescription: {0} updated.", sd.ID);
                        this.Execute(() =>
                        {
                            (s.Tag as DataGridViewRow).Update(s.Name.Length > 0 ? s.Name : "OTEX Server", s.TemporaryDocument ? "Yes" : "", s.EndPoint.Address,
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
                Logger.ErrorMessage("An error occurred while creating the server listener:\n\n{0}"
                    + "\n\nYou can still use OTEX Editor, but the \"Public Documents\" list will be empty.",
                    exc.Message);
            }

            // CREATE TEXT EDITOR //////////////////////////////////////////////////////////////////
            tbEditor = new EditorTextBox();
            tbEditor.DiffsGenerated += (sender, previous, current, diffs) =>
            {
                if (!otexClient.Connected)
                    return;

                //convert changes into operations
                int position = 0;
                foreach (var diff in diffs)
                {
                    //skip unchanged characters
                    position = Math.Min(diff.InsertStart, current.Length);

                    //process a deletion
                    if (diff.DeleteLength > 0)
                        otexClient.Delete((uint)position, (uint)diff.DeleteLength);

                    //process an insertion
                    if (position < (diff.InsertStart + diff.InsertLength))
                        otexClient.Insert((uint)position, current.Substring(position, diff.InsertLength));
                }
            };
            tbEditor.SelectionChanged += (s, e) =>
            {
                if (!otexClient.Connected)
                    return;
                var sel = tbEditor.Selection;
                localUser.SetSelection(tbEditor.PlaceToPosition(sel.Start), tbEditor.PlaceToPosition(sel.End));
            };
            tbEditor.PaintLine += (s, e) =>
            {
                if (!otexClient.Connected || remoteUsers.Count == 0)
                    return;

                Range lineRange = new Range(tbEditor, e.LineIndex);
                lock (remoteUsers)
                {
                    var len = tbEditor.TextLength;
                    foreach (var kvp in remoteUsers)
                    {
                        //check range
                        var selStart = tbEditor.PositionToPlace(kvp.Value.SelectionStart.Clamp(0, len));
                        var selEnd = tbEditor.PositionToPlace(kvp.Value.SelectionEnd.Clamp(0, len));
                        var selRange = new Range(tbEditor, selStart, selEnd);
                        var range = lineRange.GetIntersectionWith(selRange);
                        if (range.Length == 0 && !lineRange.Contains(selStart))
                            continue;

                        var ptStart = tbEditor.PlaceToPoint(range.Start);
                        var ptEnd = tbEditor.PlaceToPoint(range.End);
                        var caret = lineRange.Contains(selStart);
                        var colour = kvp.Value.Colour;

                        //draw "current line" fill
                        if (caret && selRange.Length == 0)
                        {
                            using (SolidBrush b = new SolidBrush(Color.FromArgb(12, colour)))
                                e.Graphics.FillRectangle(b, e.LineRect);
                        }
                        //draw highlight
                        if (range.Length > 0)
                        {
                            using (SolidBrush b = new SolidBrush(Color.FromArgb(32, colour)))
                                e.Graphics.FillRectangle(b, new Rectangle(ptStart.X, e.LineRect.Y,
                                    ptEnd.X - ptStart.X, e.LineRect.Height));
                        }
                        //draw caret
                        if (caret)
                        {
                            ptStart = tbEditor.PlaceToPoint(selStart);
                            using (Pen p = new Pen(Color.FromArgb(190, colour)))
                            {
                                p.Width = 2;
                                e.Graphics.DrawLine(p, ptEnd.X, e.LineRect.Top,
                                    ptEnd.X, e.LineRect.Bottom);
                            }
                        }
                    }
                }
            };
            tbEditor.Paint += (s, e) =>
            {
                if (cbLineLength.Checked)
                {
                    var pt = tbEditor.PlaceToPoint(new Place(((int)nudLineLength.Value).Clamp(60,200), 0));
                    using (Pen p = new Pen(Color.FromArgb(32, localUser.Colour)))
                    {
                        p.Width = 2;
                        p.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                        e.Graphics.DrawLine(p, new Point(pt.X, e.ClipRectangle.Top), new Point(pt.X, e.ClipRectangle.Bottom));
                    }
                }
            };

            // CREATE LOCAL USER ///////////////////////////////////////////////////////////////////
            //create local user (handles settings file)
            localUser = new User(otexClient.ID);
            //generate list of allowed user colours via colour combo box
            cbClientColour.RegenerateItems(
                false, //darks
                true, //mids
                false, //lights
                false, //transparents
                false, //monochromatics
                0.15f, //similarity threshold
                new Color[] { Color.Blue, Color.MediumBlue, Color.Red, Color.Fuchsia, Color.Magenta } //exludes
            );
            var allowedColours = cbClientColour.Items.OfType<Color>().ToList();
            //user colour
            var selectedColourIndex = allowedColours.FindIndex(c => c.ToArgb() == localUser.ColourInteger );
            if (selectedColourIndex < 0)
            {
                cbClientColour.SelectedIndex = App.Random.Next(cbClientColour.Items.Count);
                localUser.Colour = cbClientColour.SelectedColour;
            }
            else
                cbClientColour.SelectedIndex = selectedColourIndex;
            tbEditor.UserColour = localUser.Colour;
            localUser.OnColourChanged += (u) =>
            {
                otexClient.Metadata = u.Serialize();
                this.Execute(() => { tbEditor.UserColour = u.Colour; });
            };
            //selection changed
            localUser.OnSelectionChanged += (u) => { otexClient.Metadata = u.Serialize(); };
            //update interval
            nudClientUpdateInterval.Value = (decimal)(otexClient.UpdateInterval = localUser.UpdateInterval);
            localUser.OnUpdateIntervalChanged += (u) => { otexClient.UpdateInterval = u.UpdateInterval; };
            //line length ruler
            cbLineLength.Checked = localUser.Ruler;
            nudLineLength.Value = localUser.RulerOffset;
            localUser.OnRulerChanged += (u) => { this.Execute(() => { if (tbEditor.Visible) tbEditor.Refresh(); }); };
            //last direct connection address
            tbClientAddress.Text = localUser.LastDirectConnection;
            //theme
            var allowedThemes = App.Themes.Keys.ToList();
            foreach (var theme in allowedThemes)
                cbTheme.Items.Add(theme.Nameify());
            var selectedTheme = localUser.Theme;
            var selectedThemeIndex = allowedThemes.FindIndex(s => s.Equals(selectedTheme));
            if (selectedThemeIndex < 0)
            {
                cbTheme.SelectedIndex = 0;
                localUser.Theme = allowedThemes[0];
            }
            else
                cbTheme.SelectedIndex = selectedThemeIndex;
            localUser.OnThemeChanged += (u) => { this.Execute(() => { App.Theme = App.Themes[u.Theme]; }); };
            //save settings
            settingsLoaded = true;
            App.Config.User.Flush();

            // HANDLE THEMES ///////////////////////////////////////////////////////////////////////
            App.ThemeChanged += (t) =>
            {
                this.Execute(() =>
                {
                    lblManualEntry.Font
                        = lblServerBrowser.Font
                        = t.Font.Large.Regular;

                    lblTitle.Font = t.Font.Huge.Bold;

                    lblAbout.ForeColor
                        = lblVersion.ForeColor
                        = t.Foreground.LowContrast.Colour;

                    lblAbout.Font = t.Font.Underline;

                    settingsButton.Colour = t.Accent(1).Colour;

                    settingsButton.Image = App.Images.Resource("cog" + (t.IsDark ? "" : "_black"), App.Assembly, "OTEX");
                    logoutButton.Image = App.Images.Resource("logout" + (t.IsDark ? "" : "_black"), App.Assembly, "OTEX");
                }, false);
            };
            App.Theme = App.Themes[localUser.Theme];

            // CONFIGURE PAGINATOR /////////////////////////////////////////////////////////////////
            paginator = new Paginator(this);
            paginator.Add("menu", panMenuPage);
            paginator.Add("connecting", panConnectingPage);
            paginator.Add("servers", panServerBrowserPage);
            paginator.Add("editor", tbEditor);
            paginator.PageActivated += (s, k, p) =>
            {
                switch (k)
                {
                    case "menu":
                        panMenu.CenterInParent();
                        break;

                    case "connecting":
                        PositionConnectingPageControls();
                        break;
                }
            };
            //set current page
            paginator.ActivePageKey = "menu";
        }

        /////////////////////////////////////////////////////////////////////
        // SERVER MODE
        /////////////////////////////////////////////////////////////////////

        private void StartServerMode(Server.StartParams startParams)
        {
            //start server
            try
            {
                startParams.ReplaceTabsWithSpaces = 4;
                startParams.Port = Server.DefaultPort + 1;
                startParams.Public = true;
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
                otexClient.Metadata = localUser.Serialize();
                otexClient.Connect(IPAddress.Loopback, startParams.Port, null);
            }
            catch (Exception exc)
            {
                Logger.ErrorMessage("An error occurred while connecting:\n\n{0}",
                    exc.Message);
                return;
            }

            //set last connection data to null
            lastConnectionEndpoint = null;
            lastConnectionPassword = null;
            lastConnectionReturnToServerBrowser = false;

            //started ok, set title text
            Text = string.Format("Hosting {0} - {1}",
                otexClient.ServerFilePath.Length == 0 ? "a temporary document" : Path.GetFileName(otexClient.ServerFilePath),
                App.Name);
        }

        /////////////////////////////////////////////////////////////////////
        // CLIENT MODE
        /////////////////////////////////////////////////////////////////////

        private bool StartClientMode(IPEndPoint endPoint, string passwordString)
        {
            //validate port
            if (endPoint.Port < 1024 || endPoint.Port > 65535 || Server.AnnouncePorts.Contains(endPoint.Port))
            {
                Logger.ErrorMessage("Port must be between 1024-{0} or {1}-65535.",
                                    Server.AnnouncePorts.First - 1, Server.AnnouncePorts.Last + 1);
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

            return StartClientMode(endPoint, password);
        }

        private bool StartClientMode(IPEndPoint endPoint, Password password)
        {
            //show "connecting" page
            lastConnectionEndpoint = endPoint;
            lastConnectionPassword = password;
            lblConnectingStatus.Text = string.Format("Connecting to {0}...", lastConnectionEndpoint);
            btnConnectingBack.Visible = false;
            btnConnectingReconnect.Visible = false;
            paginator.ActivePageKey = "connecting";

            //start connection thread
            clientConnectingThread = new Thread(() =>
            {
                try
                {
                    otexClient.Metadata = localUser.Serialize();
                    otexClient.Connect(lastConnectionEndpoint, lastConnectionPassword);
                }
                catch (Exception exc)
                {
                    this.Execute(() =>
                    {
                        lblConnectingStatus.Text = string.Format("Could not connect to {0}.\r\n\r\n{1}",
                            lastConnectionEndpoint, exc.Message);
                        btnConnectingBack.Visible = true;
                        btnConnectingReconnect.Visible = true;
                    });
                    clientConnectingThread = null;
                    return;
                }

                //started ok
                lastConnectionReturnToServerBrowser = false;
                this.Execute(() =>
                {
                    Text = string.Format("Editing {0} ({1}) - {2}",
                        otexClient.ServerFilePath.Length == 0 ? "a temporary document" : Path.GetFileName(otexClient.ServerFilePath),
                        otexClient.ServerName.Length == 0 ? lastConnectionEndpoint.ToString() : string.Format("{0}, {1}",
                            otexClient.ServerName, lastConnectionEndpoint),
                        App.Name);
                });

                clientConnectingThread = null;
            });
            clientConnectingThread.IsBackground = false;
            clientConnectingThread.Start();
            return true;
        }

        /////////////////////////////////////////////////////////////////////
        // UI EVENTS
        /////////////////////////////////////////////////////////////////////

        protected override void OnFirstShown(EventArgs e)
        {
            base.OnFirstShown(e);
            if (IsDesignMode)
                return;
            paginator.ActivePageKey = "menu";
            Refresh();
        }

        private void btnClient_Click(object sender, EventArgs e)
        {
            paginator.ActivePageKey = "servers";
        }

        private void btnClientCancel_Click(object sender, EventArgs e)
        {
            paginator.ActivePageKey = "menu";
        }

        private void btnServerNew_Click(object sender, EventArgs e)
        {
            if (dlgServerCreateNew.ShowDialog() == DialogResult.OK)
                StartServerMode(new Server.StartParams()
                    { FilePath = dlgServerCreateNew.FileName, EditMode = false });
        }

        private void btnServerExisting_Click(object sender, EventArgs e)
        {
            if (dlgServerOpenExisting.ShowDialog() == DialogResult.OK)
                StartServerMode(new Server.StartParams()
                    { FilePath = dlgServerOpenExisting.FileName, EditMode = true });
        }

        private void btnServerTemporary_Click(object sender, EventArgs e)
        {
            StartServerMode(new Server.StartParams() { FilePath = "" });
        }

        private void btnClientConnect_Click(object sender, EventArgs e)
        {
            IPEndPoint endpoint = null;
            try
            {
                endpoint = tbClientAddress.EndPoint;
                if (endpoint.Port == 0)
                    endpoint.Port = Server.DefaultPort;
            }
            catch (Exception exc)
            {
                Logger.ErrorMessage(exc.Message);
                return;
            }
            lastConnectionReturnToServerBrowser = true;
            if (StartClientMode(endpoint, tbClientPassword.Text.Trim()))
                localUser.LastDirectConnection = tbClientAddress.Text;
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
            if (paginator != null)
            {
                paginator.Dispose();
                paginator = null;
            }
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
            if (settingsForm != null)
            {
                settingsForm.Dispose();
                settingsForm = null;
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
                {
                    lastConnectionReturnToServerBrowser = true;
                    StartClientMode(sd.EndPoint, "");
                }
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
                    lastConnectionReturnToServerBrowser = true;
                    if (StartClientMode((passwordForm.Tag as ServerDescription).EndPoint, pw))
                        this.Activate();
                }
                e.Handled = true;
            }
        }

        private void panConnectingPage_Resize(object sender, EventArgs e)
        {
            if (panConnectingPage.Visible)
                PositionConnectingPageControls();
        }

        private void panMenuPage_Resize(object sender, EventArgs e)
        {
            panMenu.CenterInParent();
        }

        private void btnConnectingBack_Click(object sender, EventArgs e)
        {
            if (lastConnectionReturnToServerBrowser)
                paginator.ActivePageKey = "servers";
            else
                paginator.ActivePageKey = "menu";
        }

        private void btnConnectingReconnect_Click(object sender, EventArgs e)
        {
            StartClientMode(lastConnectionEndpoint, lastConnectionPassword);
        }

        private void cbClientColour_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (settingsLoaded)
                localUser.Colour = cbClientColour.SelectedColour;
        }

        private void cbLineLength_CheckedChanged(object sender, EventArgs e)
        {
            nudLineLength.Enabled = cbLineLength.Checked;
            if (settingsLoaded)
                localUser.Ruler = cbLineLength.Checked;
        }

        private void nudLineLength_ValueChanged(object sender, EventArgs e)
        {
            if (settingsLoaded)
                localUser.RulerOffset = (uint)nudLineLength.Value;
        }

        private void nudClientUpdateInterval_ValueChanged(object sender, EventArgs e)
        {
            if (settingsLoaded)
                localUser.UpdateInterval = (float)nudClientUpdateInterval.Value;
        }

        private void cbTheme_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (settingsLoaded)
                localUser.Theme = cbTheme.Items[cbTheme.SelectedIndex] as string;
        }

        private void PositionConnectingPageControls()
        {
            panConnectingPage.SuspendLayout();
            lblConnectingStatus.Width = lblConnectingStatus.Parent.Width - 10;
            lblConnectingStatus.CenterInParent();
            panConnectingContent.CenterInParent();
            panConnectingContent.Top = lblConnectingStatus.Bottom + 8;
            panConnectingPage.ResumeLayout(true);
        }
    }
}
