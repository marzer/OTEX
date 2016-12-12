using Marzersoft;
using Marzersoft.Controls;
using Marzersoft.Forms;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Windows.Forms;

namespace OTEX.Editor
{
    public partial class EditorForm : MainForm
    {
        /////////////////////////////////////////////////////////////////////
        // PROPERTIES/VARIABLES
        /////////////////////////////////////////////////////////////////////

        private readonly Dictionary<Guid, Editor> editors
            = new Dictionary<Guid, Editor>();
        internal Editor activeEditor;
        internal readonly LanguageManager languageManager;
        internal readonly IconManager iconManager;
        private readonly Server otexServer;
        internal readonly Client otexClient;
        private readonly ServerListener otexServerListener;
        private volatile Thread clientConnectingThread = null;
        private volatile bool closing = false;
        private FlyoutForm passwordForm = null, temporaryDocumentForm = null;
        private volatile IPEndPoint lastConnectionEndpoint = null;
        private volatile Password lastConnectionPassword = null;
        private TitleBarButton logoutButton = null, settingsButton = null, usersButton = null;
        private volatile bool settingsLoaded = false;
        private Paginator mainPaginator = null, sidePaginator = null, menuPaginator = null;
        internal readonly Paginator editorPaginator = null;
        internal readonly User localUser;
        private readonly Dictionary<Guid, User> remoteUsers = new Dictionary<Guid, User>();
        internal readonly PluginFactory plugins;
        private readonly Dictionary<Keys, Action<IEditorTextBox>> editorBindings
            = new Dictionary<Keys, Action<IEditorTextBox>>();
        private readonly Dictionary<Keys, Action> globalBindings
            = new Dictionary<Keys, Action>();
        private volatile bool suspendTitleBarButtonEvents = false;
        internal readonly Settings settings;
        private readonly Session hostSession = new Session();

        /// <summary>
        /// List of allowed user colours.
        /// </summary>
        public static IReadOnlyList<Color> AllowedColours
        {
            get { return allowedColors.Value; }
        }
        private static readonly Lazy<IReadOnlyList<Color>> allowedColors
            = new Lazy<IReadOnlyList<Color>>(() =>
            {
                List<Color> colours = new List<Color>();
                colours.Add(ColorTranslator.FromHtml("#999999"));
                colours.Add(ColorTranslator.FromHtml("#ec7063"));
                colours.Add(ColorTranslator.FromHtml("#af7ac5"));
                colours.Add(ColorTranslator.FromHtml("#5dade2"));
                colours.Add(ColorTranslator.FromHtml("#48c9b0"));
                colours.Add(ColorTranslator.FromHtml("#52be80"));
                colours.Add(ColorTranslator.FromHtml("#f4d03f"));
                colours.Add(ColorTranslator.FromHtml("#f5b041"));
                colours.Add(ColorTranslator.FromHtml("#dc7633"));
                return colours.AsReadOnly();
            }, true);

        /////////////////////////////////////////////////////////////////////
        // CONSTRUCTOR
        /////////////////////////////////////////////////////////////////////

        public EditorForm(params object[] plf)
        {
            InitializeComponent();
            if (IsDesignMode)
                return;

            // USER ID /////////////////////////////////////////////////////////////////////////////
            var instanceID = App.UserID;
#if DEBUG
            string argID = null;
            if (App.Arguments.Value("id", ref argID))
            {
                if (argID.Trim().ToLower().Equals("random"))
                    instanceID = Guid.NewGuid();
                else
                    argID.TryParse(out instanceID);
            }
#endif

            // PLUGINS /////////////////////////////////////////////////////////////////////////////
            plugins = plf[0] as PluginFactory;

            // SPLIT PANEL /////////////////////////////////////////////////////////////////////////
            splitter.Dock = DockStyle.Fill;
            splitter.HidePanel(2);

            // CONFIGURE MAIN MENU /////////////////////////////////////////////////////////////////
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
            filterFactory.Apply(dlgHostNew);
            filterFactory.Apply(dlgHostExisting);

            // CONFIGURE JOIN PAGE /////////////////////////////////////////////////////////////////
            dgvServers.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dgvServers.Columns[1].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            btnHostDelete.Visible = false;

            // CONFIGURE "CONNECTING" PAGE /////////////////////////////////////////////////////////
            lblConnectingStatus.Width = panConnectingPage.ClientSize.Width - 10;            

            // CONFIGURE FORM TITLEBAR /////////////////////////////////////////////////////////////
            //settings button
            settingsButton = new TitleBarButton(this);
            settingsButton.Image.Add(App.Images.Resource("settings", App.Assembly, "OTEX.Editor"));
            settingsButton.ToggleMode = true;
            settingsButton.Tag = panSettings;
            panSettings.Tag = settingsButton;
            settingsButton.CheckedChanged += TitleBarButton_CheckedChanged;
            //users button
            usersButton = new TitleBarButton(this);
            usersButton.Image.Add(App.Images.Resource("users", App.Assembly, "OTEX.Editor"));
            usersButton.ToggleMode = true;
            usersButton.Tag = flpUsers;
            usersButton.Offset += settingsButton.Width;
            flpUsers.Tag = usersButton;
            usersButton.CheckedChanged += TitleBarButton_CheckedChanged;
            usersButton.Visible = false;
            //logout button
            logoutButton = new TitleBarButton(this);
            logoutButton.Image.Add(App.Images.Resource("logout", App.Assembly, "OTEX.Editor"));
            logoutButton.RightAligned = false;
            logoutButton.Colour = ColorTranslator.FromHtml("#DF3F26");
            logoutButton.Visible = false;
            logoutButton.Click += (b) =>
            {
                if (otexServer.Running && otexServer.Session.ClientCount > 1 &&
                    !Logger.WarningQuestion(this,"You are currently running in server mode. "
                    + "Leaving the session will disconnect the other {0} connected users.\n\nLeave session?", otexServer.Session.ClientCount - 1))
                    return;

                otexClient.Disconnect();
                otexServer.Stop();
            };

            // CREATE OTEX SERVER //////////////////////////////////////////////////////////////////
            otexServer = new Server(Shared.AppKey, instanceID);
            otexServer.OnThreadException += (s, e) =>
            {
                Logger.W("Server: {0}: {1}", e.InnerException.GetType().FullName, e.InnerException.Message);
#if DEBUG
                if (Debugger.IsAttached && !closing)
                    throw new Exception("otexServer.OnThreadException", e);
#endif
            };
            otexServer.OnStarted += (s) =>
            {
                Logger.I("Server: started on port {0}", s.Session.Port);
            };
            otexServer.OnClientConnected += (s, rc) =>
            {
                Logger.I("Server: client {0} connected.", rc.ID);
            };
            otexServer.OnClientDisconnected += (s, rc) =>
            {
                Logger.I("Server: client {0} disconnected.", rc.ID);
            };
            otexServer.OnStopped += (s) =>
            {
                Logger.I("Server: stopped.");
            };

            // CREATE OTEX CLIENT //////////////////////////////////////////////////////////////////
            otexClient = new Client(Shared.AppKey, instanceID);
            otexClient.OnThreadException += (c, e) =>
            {
                Logger.W("Client: {0}: {1}", e.InnerException.GetType().Name, e.InnerException.Message);
#if DEBUG
                if (Debugger.IsAttached && !closing)
                    throw new Exception("otexClient.OnThreadException", e);
#endif
            };
            otexClient.OnConnected += (c) =>
            {
                Logger.I("Client: connected to {0}:{1}.", c.Session.Address, c.Session.Port);
                this.Execute(() =>
                {
                    mainPaginator.ActivePageKey = "editors";
                    usersButton.Visible = logoutButton.Visible = true;
                    flpUsers.Users.Clear().Add(localUser);

                    foreach (var doc in c.Session.Documents)
                        editors[doc.Key] = new Editor(this, doc.Value);
                }, false);
            };
            otexClient.OnRemoteConnection += (c, rc) =>
            {
                lock (remoteUsers)
                {
                    var remoteUser = remoteUsers[rc.ID] =  new User(rc.ID, rc.Metadata);
                    remoteUser.OnColourChanged += (u) =>
                    {
                        this.Execute(() =>
                        {
                            foreach (var editor in editors)
                                editor.Value.UpdateRemoteUser(u);
                        });
                    };
                    remoteUser.OnSelectionChanged += (u) =>
                    {
                        this.Execute(() =>
                        {
                            foreach (var editor in editors)
                                editor.Value.UpdateRemoteUser(u);
                        });
                    };
                    this.Execute(() =>
                    {
                        foreach (var editor in editors)
                            editor.Value.UpdateRemoteUser(remoteUser);
                        flpUsers.Users.Add(remoteUser);
                    }, false);
                }
            };
            otexClient.OnRemoteOperations += (c, id, ops) =>
            {
                this.Execute(() =>
                {
                    if (editors.TryGetValue(id, out var editor))
                        editor.RemoteOperations(ops);
                });
            };
            otexClient.OnRemoteMetadata += (c, rc) =>
            {
                lock (remoteUsers)
                {
                    if (remoteUsers.TryGetValue(rc.ID, out var remoteUser))
                    {
                        remoteUser.Update(rc.Metadata);
                        this.Execute(() =>
                        {
                            foreach (var editor in editors)
                                editor.Value.UpdateRemoteUser(remoteUser);
                        });
                    }
                }
            };
            otexClient.OnRemoteDisconnection += (c, rc) =>
            {
                Logger.I("Client: remote client {0} disconnected.", rc.ID);

                lock (remoteUsers)
                {
                    if (remoteUsers.TryGetValue(rc.ID, out var remoteUser))
                    {
                        remoteUsers.Remove(rc.ID);
                        this.Execute(() => { flpUsers.Users.Remove(remoteUser); });
                        remoteUser.Dispose();
                    }
                }
            };
            otexClient.OnDisconnected += (c, serverSide) =>
            {
                Logger.I("Client: disconnected{0}.", serverSide ? " (connection closed by server)" : "");
                lock (remoteUsers)
                {
                    foreach (var ru in remoteUsers)
                        ru.Value.Dispose();
                    remoteUsers.Clear();
                }
                if (!closing)
                {
                    localUser.SetSelection(Guid.Empty, 0, 0);
                    this.Execute(() =>
                    {
                        flpUsers.Users.Clear();
                        foreach (var editor in editors)
                            editor.Value.Dispose();
                        editors.Clear();

                        //client mode (in server mode, client always disconnects first)
                        if (serverSide)
                        {
                            if (lastConnectionEndpoint != null)
                            {
                                lblConnectingStatus.Text = string.Format("Connection to {0} was lost.",
                                    lastConnectionEndpoint);
                                btnConnectingBack.Visible = true;
                                btnConnectingReconnect.Visible = true;
                                mainPaginator.ActivePageKey = "connecting";
                            }
                            else
                                mainPaginator.ActivePageKey = "menu";
                        }
                        else
                            mainPaginator.ActivePageKey = "menu";


                        usersButton.Checked = false;
                        usersButton.Visible = logoutButton.Visible = false;
                    });
                }
            };

            // CREATE OTEX SERVER LISTENER /////////////////////////////////////////////////////////
            try
            {
                otexServerListener = new ServerListener(Shared.AppKey, otexServer.ID);
                otexServerListener.OnThreadException += (sl, e) =>
                {
                    Logger.W("ServerListener: {0}: {1}", e.InnerException.GetType().FullName, e.InnerException.Message);
#if DEBUG
                    if (Debugger.IsAttached && !closing)
                        throw new Exception("otexServerListener.OnThreadException", e);
#endif
                };
                otexServerListener.OnServerAdded += (sl, s) =>
                {
                    Logger.I("ServerListener: new server {0}: {1}", s.ID, s.EndPoint);
                    this.Execute(() =>
                    {
                        var row = dgvServers.AddRow(s.Name.Length > 0 ? s.Name : "OTEX Server", s.EndPoint.Address,
                            s.EndPoint.Port, s.RequiresPassword ? "Yes" : "", string.Format("{0} / {1}",s.ClientCount, s.ClientLimit), 0);
                        row.Tag = s;
                        s.Tag = row;
                    });

                    s.OnUpdated += (sd) =>
                    {
                        Logger.I("ServerDescription: {0} updated.", sd.ID);
                        this.Execute(() =>
                        {
                            (s.Tag as DataGridViewRow).Update(s.Name.Length > 0 ? s.Name : "OTEX Server", s.EndPoint.Address,
                                s.EndPoint.Port, s.RequiresPassword ? "Yes" : "", string.Format("{0} / {1}", s.ClientCount, s.ClientLimit), 0);
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
                Logger.ErrorMessage(this, "An error occurred while creating the server listener:\n\n{0}"
                    + "\n\nYou can still use OTEX Editor, but the \"Public sessions\" list will be empty.",
                    exc.Message);
            }

            // KEY BINDINGS ////////////////////////////////////////////////////////////////////////
            globalBindings[Keys.Control | Keys.W] = () => { cbLineEndings.Checked = !cbLineEndings.Checked; };
            editorBindings[Keys.Control | Keys.Q] = (tb) => { tb.ToggleCommentSelection(); };
            editorBindings[Keys.Control | Keys.B] = (tb) => { tb.ToggleBookmark(); };
            editorBindings[Keys.F2] = (tb) => { tb.NextBookmark(); };
            editorBindings[Keys.Shift | Keys.F2] = (tb) => { tb.PreviousBookmark(); };
            editorBindings[Keys.Control | Keys.Subtract] = (tb) => { tb.DecreaseZoom(); };
            editorBindings[Keys.Control | Keys.Add] = (tb) => { tb.IncreaseZoom(); };
            editorBindings[Keys.Control | Keys.NumPad0] = (tb) => { tb.ResetZoom(); };
            editorBindings[Keys.Control | Keys.U] = (tb) => { tb.UppercaseSelection(); };
            editorBindings[Keys.Control | Keys.Shift | Keys.U] = (tb) => { tb.LowercaseSelection(); };

            // CREATE LANGUAGE MANAGER /////////////////////////////////////////////////////////////
            languageManager = new LanguageManager();
            languageManager.OnThreadException += (lm, e) =>
            {
                Logger.E("LanguageManager: {0}: {1}", e.InnerException.GetType().FullName, e.InnerException.Message);
#if DEBUG
                if (Debugger.IsAttached && !closing)
                    throw new Exception("languageManager.OnThreadException", e);
#endif
            };
            languageManager.OnLoaded += (lm, c) =>
            {
                Logger.I("LanguageManager: loaded {0} languages.", c);
            };

            // CREATE ICON MANAGER /////////////////////////////////////////////////////////////////
            iconManager = new IconManager();
            iconManager.OnThreadException += (lm, e) =>
            {
                Logger.E("IconManager: {0}: {1}", e.InnerException.GetType().FullName, e.InnerException.Message);
#if DEBUG
                if (Debugger.IsAttached && !closing)
                    throw new Exception("iconManager.OnThreadException", e);
#endif
            };
            iconManager.OnLoaded += (lm, c, ex) =>
            {
                Logger.I("IconManager: loaded {0} icons for {1} file extensions.", c, ex);
            };

            // CREATE LOCAL USER ///////////////////////////////////////////////////////////////////
            localUser = new User(otexClient.ID);
            cbClientColour.SetItems(AllowedColours, false);
            var selectedColourIndex = Array.FindIndex(AllowedColours.ToArray(), c => c.ToArgb() == localUser.ColourInteger );
            if (selectedColourIndex < 0)
            {
                cbClientColour.SelectedIndex = App.Random.Next(cbClientColour.Items.Count);
                localUser.Colour = cbClientColour.SelectedColour;
            }
            else
                cbClientColour.SelectedIndex = selectedColourIndex;
            localUser.OnColourChanged += (u) =>
            {
                otexClient.Metadata = u.Serialize();
            };
            //selection changed
            localUser.OnSelectionChanged += (u) => { otexClient.Metadata = u.Serialize(); };

            // CONFIGURE SETTINGS //////////////////////////////////////////////////////////////////
            //about label
            lblAbout.MouseEnter += (s, e) => { lblAbout.ForeColor = App.Theme.Foreground.Colour; };
            lblAbout.MouseLeave += (s, e) => { lblAbout.ForeColor = App.Theme.Foreground.LowContrast.Colour; };
            lblAbout.Click += (s, e) => { App.Website.LaunchWebsite(); };
            //debug label
#if DEBUG
            lblDebug.Text = instanceID.ToString();
#else
            lblDebug.Visible = false;
#endif
            //settings manager
            settings = new Settings();
            //update interval
            nudClientUpdateInterval.Value = (decimal)(otexClient.UpdateInterval = settings.UpdateInterval);
            settings.OnUpdateIntervalChanged += (u) =>
            {
                if (!otexClient.Connected || otexClient.ID != otexServer.ID)
                    otexClient.UpdateInterval = u.UpdateInterval;
            };
            //line length ruler
            cbLineLength.Checked = settings.RulerVisible;
            nudLineLength.Value = settings.RulerOffset;
            //last direct connection address
            tbClientAddress.Text = settings.LastDirectConnection;
            //theme
            var allowedThemes = App.Themes.Keys.ToList();
            foreach (var theme in allowedThemes)
                cbTheme.Items.Add(theme.Nameify());
            var selectedTheme = settings.Theme;
            var selectedThemeIndex = allowedThemes.FindIndex(s => s.Equals(selectedTheme));
            if (selectedThemeIndex < 0)
            {
                cbTheme.SelectedIndex = 0;
                settings.Theme = allowedThemes[0];
            }
            else
                cbTheme.SelectedIndex = selectedThemeIndex;
            settings.OnThemeChanged += (u) => { this.Execute(() => { App.Theme = App.Themes[u.Theme]; }); };
            //line endings
            cbLineEndings.Checked = settings.LineEndings;
            //save settings
            settingsLoaded = true;
            App.Config.Flush();

            // HANDLE THEMES ///////////////////////////////////////////////////////////////////////
            App.ThemeChanged += (t) =>
            {
                this.Execute(() =>
                {
                    lblAbout.ForeColor = t.Foreground.LowContrast.Colour;
                    lblAbout.Font = t.Font.Underline;

                    settingsButton.Colour = t.Accent(0).Colour;
                    usersButton.Colour = t.Accent(1).Colour;
                    settingsButton.InvertImage = settingsButton.InvertImageWhenChecked = !t.IsDark;
                    usersButton.InvertImage = usersButton.InvertImageWhenChecked = !t.IsDark;
                    logoutButton.InvertImage = logoutButton.InvertImageWhenChecked = !t.IsDark;
                    sideSplitter.Panel1.Refresh();

                    var mutator = t.IsDark ? "" : "invert";
                    btnHostNew.Image = App.Images.Resource("doc_new", mutator, App.Assembly, "OTEX.Editor");
                    btnHostExisting.Image = App.Images.Resource("open", mutator, App.Assembly, "OTEX.Editor");
                    btnHostTemporary.Image = App.Images.Resource("doc_temp", mutator, App.Assembly, "OTEX.Editor");
                    btnHostDelete.Image = App.Images.Resource("doc_delete", mutator, App.Assembly, "OTEX.Editor");
                    btnClientConnect.Image = btnHostStart.Image = App.Images.Resource("play", mutator, App.Assembly, "OTEX.Editor");
                    btnConnectingReconnect.Image = App.Images.Resource("refresh", mutator, App.Assembly, "OTEX.Editor");
                    btnConnectingBack.Image = App.Images.Resource("previous", mutator, App.Assembly, "OTEX.Editor");
                }, false);
            };
            App.Theme = App.Themes[settings.Theme];

            // CONFIGURE MAIN CONTENT PAGINATOR ////////////////////////////////////////////////////
            mainPaginator = new Paginator(splitter.Panel1);
            mainPaginator.Add("menu", menuSplitter);
            mainPaginator.Add("connecting", panConnectingPage);
            mainPaginator.Add("editors", panEditors);
            mainPaginator.PageActivated += (s, k, p) =>
            {
                if (p == null)
                    return;
                switch (k)
                {
                    case "connecting":
                        PositionConnectingPageControls();
                        break;
                }
            };
            mainPaginator.ActivePageKey = "menu";

            // CONFIGURE SIDEBAR PAGINATOR /////////////////////////////////////////////////////////
            sidePaginator = new Paginator(sideSplitter.Panel2);
            sidePaginator.Add("settings", panSettings);
            sidePaginator.Add("users", flpUsers);
            sidePaginator.ActivePage = null;
            sidePaginator.PageActivated += (s, k, p) =>
            {
                if (p == null)
                    return;
                lblSideBar.Text = k.Nameify();
            };

            // CONFIGURE EDITOR PAGINATOR //////////////////////////////////////////////////////////
            editorPaginator = new Paginator(panEditors);

            // CONFIGURE MAIN MENU PAGINATOR ///////////////////////////////////////////////////////
            menuPaginator = new Paginator(menuSplitter.Panel2);
            menuPaginator.Add("host", panJoin);
            menuPaginator.Add("join", panHost);
            menuPaginator.ActivePage = panHost;
            btnHost.Tag = panHost;
            btnJoin.Tag = panJoin;
            btnHost.CheckedChanged += BtnHost_CheckedChanged;
            btnJoin.CheckedChanged += BtnHost_CheckedChanged;
        }

        private void BtnHost_CheckForeColour(object sender, EventArgs e)
        {
            var rb = sender as RadioButton;
            rb.ForeColor = rb.BackColor.Furthest(rb.ForeColor, rb.ForeColor.Invert());
        }

        private void BtnHost_CheckedChanged(object sender, EventArgs e)
        {
            var rb = sender as RadioButton;
            if (rb.Checked)
                menuPaginator.ActivePage = (rb.Tag as Control);
        }

        /////////////////////////////////////////////////////////////////////
        // SERVER MODE
        /////////////////////////////////////////////////////////////////////

        private void StartServerMode(Session session, string password = "")
        {
            //start server
            try
            {
                if (password.Length > 0)
                    session.Password = new Password(password);
                session.Port = Server.DefaultPort + 1;
#if DEBUG
                session.Public = true;
#endif
                otexServer.Start(session);
            }
            catch (Exception exc)
            {
                Logger.ErrorMessage(this, "An error occurred while starting the server:\n\n{0}",
                    exc.Message);
#if DEBUG
                if (Debugger.IsAttached && !closing)
                    throw new Exception("otexServer.Start", exc);
#endif
                return;
            }

            //start client
            try
            {
                otexClient.Metadata = localUser.Serialize();
                otexClient.UpdateInterval = 0.5f; //server mode can have more frequent updates
                otexClient.Connect(IPAddress.Loopback, session.Port, null);
            }
            catch (Exception exc)
            {
                Logger.ErrorMessage(this, "An error occurred while connecting:\n\n{0}",
                    exc.Message);
                otexServer.Stop();

#if DEBUG
                if (Debugger.IsAttached && !closing)
                    throw new Exception("otexClient.Connect", exc);
#endif
                return;
            }

            //set last connection data to null
            lastConnectionEndpoint = null;
            lastConnectionPassword = null;

            //change "news" to "edits" in session so we can restart session without
            //overwriting
            if (session == hostSession)
            {
                List<Document> docs = new List<Document>();
                foreach (var doc in hostSession.Documents)
                    if (!doc.Value.Temporary && doc.Value.ConflictResolution == Document.ConflictResolutionStrategy.Replace)
                        docs.Add(doc.Value);
                foreach (var doc in docs)
                {
                    hostSession.RemoveDocument(doc);
                    lbDocuments.Items.Remove(doc);
                    lbDocuments.Items.Add(hostSession.AddDocument(
                        doc.Path, Document.ConflictResolutionStrategy.Edit, 4));
                }
            }
        }

        /////////////////////////////////////////////////////////////////////
        // CLIENT MODE
        /////////////////////////////////////////////////////////////////////

        private bool StartClientMode(IPEndPoint endPoint, string passwordString)
        {
            //validate port
            if (endPoint.Port < 1024 || endPoint.Port > 65535 || Server.AnnouncePorts.Contains(endPoint.Port))
            {
                Logger.ErrorMessage(this, "Port must be between 1024-{0} or {1}-65535.",
                                    Server.AnnouncePorts.First - 1, Server.AnnouncePorts.Last + 1);
                return false;
            }

            //validate password
            var originalPasswordLength = passwordString.Length;
            passwordString = (passwordString ?? "").Trim();
            if (originalPasswordLength > 0 && passwordString.Length == 0)
            {
                Logger.ErrorMessage(this, "Passwords cannot be entirely whitespace.");
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
                    Logger.ErrorMessage(this, "An error occurred while parsing password:\n\n{0}", exc.Message);
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
            mainPaginator.ActivePageKey = "connecting";

            //start connection thread
            clientConnectingThread = new Thread(() =>
            {
                try
                {
                    otexClient.Metadata = localUser.Serialize();
                    otexClient.UpdateInterval = settings.UpdateInterval;
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
                clientConnectingThread = null;
            });
            clientConnectingThread.IsBackground = false;
            clientConnectingThread.Start();
            return true;
        }

        /////////////////////////////////////////////////////////////////////
        // UI EVENTS
        /////////////////////////////////////////////////////////////////////

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            //check per-editor bindings
            if (otexClient.Connected && activeEditor != null)
            {
                if (editorBindings.TryGetValue(keyData, out var editorBinding))
                {
                    editorBinding(activeEditor.TextBox);
                    return true;
                }
            }

            //check global bindings
            if (globalBindings.TryGetValue(keyData, out var globalBinding))
            {
                globalBinding();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override void OnFirstShown(EventArgs e)
        {
            base.OnFirstShown(e);
            if (IsDesignMode)
                return;
            btnHostDelete.Visible = false;
            languageManager.Load(Path.Combine(Shared.BasePath, "languages.xml"));
            iconManager.Load(Path.Combine(Shared.BasePath, "icons\\mapping.less"));

            string[] edits = null;
            App.Arguments.Values("edit", ref edits);
            string[] news = null;
            App.Arguments.Values("new", ref news);
            string[] temps = null;
            App.Arguments.Values("temp", ref temps);
            if (edits != null || temps != null || news != null)
            {
                Session session = new Session();
                if (edits != null)
                {
                    foreach (var f in edits)
                        session.AddDocument(f, Document.ConflictResolutionStrategy.Edit, 4);
                }
                if (news != null)
                {
                    foreach (var f in news)
                        session.AddDocument(f, Document.ConflictResolutionStrategy.Replace, 4);
                }
                if (temps != null)
                {
                    foreach (var f in temps)
                        session.AddDocument(f);
                }
                ushort port = 0;
                if (App.Arguments.Value("port", ref port))
                    session.Port = port;
                string str = "";
                if (App.Arguments.Value("name", ref str))
                    session.Name = str;
                uint lim = 0;
                if (App.Arguments.Value("maxclients", ref lim))
                    session.ClientLimit = lim;
                string pass = "";
                App.Arguments.Value("password", ref pass);
                bool pub = true;
                App.Arguments.Boolean("public", "private", ref pub);
                session.Public = pub;
                StartServerMode(session, pass);
            }
            else
                mainPaginator.ActivePageKey = "menu";
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
                Logger.ErrorMessage(this, exc.Message);
                return;
            }
            if (StartClientMode(endpoint, tbClientPassword.Text.Trim()))
                settings.LastDirectConnection = tbClientAddress.Text;
        }

        private void EditorForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (otexServer.Running && otexServer.Session.ClientCount > 1 &&
                !Logger.WarningQuestion(this, "You are currently running in server mode. "
                + "Closing the application will disconnect the other {0} connected users.\n\nClose OTEX Editor?", otexServer.Session.ClientCount - 1))
                e.Cancel = true;
        }

        protected override void OnClosed(EventArgs e)
        {
            closing = true;

            //clear key bindings
            editorBindings.Clear();
            globalBindings.Clear();

            //dispose network objects
            if (otexServerListener != null)
                otexServerListener.Dispose();
            if (otexClient != null)
                otexClient.Dispose();
            if (otexServer != null)
                otexServer.Dispose();

            //dispose ui
            if (mainPaginator != null)
            {
                mainPaginator.Dispose();
                mainPaginator = null;
            }
            if (sidePaginator != null)
            {
                sidePaginator.Dispose();
                sidePaginator = null;
            }
            if (editorPaginator != null)
                editorPaginator.Dispose();
            if (clientConnectingThread != null)
            {
                clientConnectingThread.Join();
                clientConnectingThread = null;
            }
            if (passwordForm != null)
            {
                passwordForm.Dispose();
                passwordForm = null;
            }
            if (temporaryDocumentForm != null)
            {
                temporaryDocumentForm.Dispose();
                temporaryDocumentForm = null;
            }
            if (languageManager != null)
                languageManager.Dispose();
            if (iconManager != null)
                iconManager.Dispose();

            //flush config
            App.Config.Flush();

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
                e.Handled = true;
                btnClientConnect_Click(this, null);
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
                        passwordForm = new FlyoutForm(panServerPassword, null, tbClientPassword);
                    passwordForm.Tag = sd;
                    tbServerPassword.Text = "";
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
                e.Handled = true;
                if (tbServerPassword.Text.Length > 0)
                {
                    var pw = tbServerPassword.Text;
                    tbServerPassword.Text = "";
                    if (StartClientMode((passwordForm.Tag as ServerDescription).EndPoint, pw))
                        this.Activate();
                }
            }
        }

        private void panConnectingPage_Resize(object sender, EventArgs e)
        {
            if (panConnectingPage.Visible)
                PositionConnectingPageControls();
        }

        private void btnConnectingBack_Click(object sender, EventArgs e)
        {
            mainPaginator.ActivePageKey = "menu";
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
                settings.RulerVisible = cbLineLength.Checked;
        }

        private void themedSplitContainer1_Panel1_Paint(object sender, PaintEventArgs e)
        {
            if (sidePaginator.ActivePage == null)
                return;
            var button = sidePaginator.ActivePage.Tag as TitleBarButton;
            if (button == null)
                return;

            using (var brush = new SolidBrush(button.Colour.Value))
                e.Graphics.FillRectangle(brush, 0, 0, (sender as Control).Width, 4);
        }

        private void nudLineLength_ValueChanged(object sender, EventArgs e)
        {
            if (settingsLoaded)
                settings.RulerOffset = (uint)nudLineLength.Value;
        }

        private void cmUsers_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!otexClient.Connected)
            {
                e.Cancel = true;
                return;
            }

            var selectedUsers = flpUsers.Users.SelectedItems;
            if (selectedUsers.Length == 0)
            {
                e.Cancel = true;
                return;
            }

            adminSeparator.Visible = adminToolStripMenuItem.Visible = (otexClient.Session.ID == otexServer.ID);
        }

        private void btnHostNew_Click(object sender, EventArgs e)
        {
            if (dlgHostNew.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    var path = Path.GetFullPath(dlgHostNew.FileName.Trim());
                    var key = path.ToLower();
                    foreach (var kvp in hostSession.Documents)
                    {
                        if (!kvp.Value.Temporary && Path.GetFullPath(kvp.Value.Path.Trim().ToLower()).Equals(key))
                        {
                            Logger.ErrorMessage(this, "{0} has already been added to the session.", path);
                            return;
                        }
                    }
                    lbDocuments.Items.Add(
                        hostSession.AddDocument(path, Document.ConflictResolutionStrategy.Replace, 4));
                    ValidateHostForm();
                }
                catch (Exception exc)
                {
                    Logger.ErrorMessage(this, exc.Message);
                }
            }
        }

        private void btnHostExisting_Click(object sender, EventArgs e)
        {
            if (dlgHostExisting.ShowDialog() == DialogResult.OK)
            {
                var paths = dlgHostExisting.FileNames;
                foreach (var p in paths)
                {
                    try
                    {
                        var path = Path.GetFullPath(p.Trim());
                        var key = path.ToLower();
                        bool skip = false;
                        foreach (var kvp in hostSession.Documents)
                        {
                            if (!kvp.Value.Temporary && Path.GetFullPath(kvp.Value.Path.Trim().ToLower()).Equals(key))
                            {
                                Logger.ErrorMessage(this, "{0} has already been added to the session.", path);
                                skip = true;
                                break;
                            }
                        }
                        if (skip)
                            continue;
                        lbDocuments.Items.Add(
                            hostSession.AddDocument(path, Document.ConflictResolutionStrategy.Edit, 4));
                    }
                    catch (Exception exc)
                    {
                        Logger.ErrorMessage(this, exc.Message);
                    }
                }
                ValidateHostForm();
            }
        }

        private void tbHostTempName_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\n' || e.KeyChar == '\r')
            {
                e.Handled = true;
                var desc = tbHostTempName.Text.Trim();
                var key = desc.ToLower();
                if (desc.Length > 0)
                {
                    foreach (var kvp in hostSession.Documents)
                    {
                        if (kvp.Value.Temporary && kvp.Value.Path.Trim().ToLower().Equals(desc))
                        {
                            Logger.ErrorMessage(this, "A temporary document with description \"{0}\" has already been added to the session.", desc);
                            this.Activate();
                            return;
                        }
                    }
                    lbDocuments.Items.Add(hostSession.AddDocument(desc));
                    ValidateHostForm();
                    this.Activate();
                }
            }
        }

        private void ValidateHostForm()
        {
            btnHostStart.Enabled = lbDocuments.Items.Count > 0;
            btnHostDelete.Visible = lbDocuments.Items.Count > 0 && lbDocuments.SelectedIndex > -1;
        }

        private void btnHostStart_Click(object sender, EventArgs e)
        {
            hostSession.Public = true;
            StartServerMode(hostSession, "");
        }

        private void lbDocuments_SelectedIndexChanged(object sender, EventArgs e)
        {
            ValidateHostForm();
        }

        private void btnHostDelete_Click(object sender, EventArgs e)
        {
            if (lbDocuments.Items.Count == 0 || lbDocuments.SelectedItems == null
                || lbDocuments.SelectedItems.Count == 0)
                return;
            var docs = lbDocuments.SelectedItems.Cast<Document>().ToList();
            if (docs.Count == 0)
                return;
            foreach (var doc in docs)
            {
                lbDocuments.Items.Remove(doc);
                hostSession.RemoveDocument(doc);
            }
            ValidateHostForm();
        }

        private void btnHostTemporary_Click(object sender, EventArgs e)
        {
            if (temporaryDocumentForm == null)
                temporaryDocumentForm = new FlyoutForm(panHostTempName, null, tbHostTempName);
            tbHostTempName.Text = "";
            temporaryDocumentForm.Flyout();

        }

        private void cbLineEndings_CheckedChanged(object sender, EventArgs e)
        {
            if (settingsLoaded)
                settings.LineEndings = cbLineEndings.Checked;
        }

        private void kickToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!otexServer.Running)
                return;

            var selectedUsers = flpUsers.Users.SelectedItems;
            if (selectedUsers.Length > 0)
            {
                foreach (var user in selectedUsers)
                    otexServer.Kick(user.ID, sender == banToolStripMenuItem);
            }
        }

        private void nudClientUpdateInterval_ValueChanged(object sender, EventArgs e)
        {
            if (settingsLoaded)
                settings.UpdateInterval = (float)nudClientUpdateInterval.Value;
        }

        private void cbTheme_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (settingsLoaded)
                settings.Theme = cbTheme.Items[cbTheme.SelectedIndex] as string;
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

        private void TitleBarButton_CheckedChanged(TitleBarButton b)
        {
            if (suspendTitleBarButtonEvents)
                return;
            sideSplitter.SuspendRepaints();
            var buttonPage = b.Tag as Control;
            var currentPage = sidePaginator.ActivePage;
            if (b.Checked && currentPage != buttonPage)
            {
                suspendTitleBarButtonEvents = true;
                if (currentPage != null)
                    (currentPage.Tag as TitleBarButton).Checked = false;
                sidePaginator.ActivePage = buttonPage;
                splitter.ShowPanel(2);
                suspendTitleBarButtonEvents = false;
            }
            else if (!b.Checked && currentPage == buttonPage)
            {
                sidePaginator.ActivePage = null;
                splitter.HidePanel(2);
            }
            sideSplitter.ResumeRepaints();
        }
    }
}
