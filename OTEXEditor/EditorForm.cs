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
        private readonly Server otexServer;
        internal readonly Client otexClient;
        private readonly ServerListener otexServerListener;
        private volatile Thread clientConnectingThread = null;
        private volatile bool closing = false;
        private FlyoutForm passwordForm = null;
        private volatile IPEndPoint lastConnectionEndpoint = null;
        private volatile Password lastConnectionPassword = null;
        private volatile bool lastConnectionReturnToServerBrowser = false;
        private TitleBarButton logoutButton = null, settingsButton = null, usersButton = null;
        private volatile bool settingsLoaded = false;
        private Paginator mainPaginator = null, sidePaginator = null;
        internal readonly Paginator editorPaginator = null;
        internal readonly User localUser;
        private readonly Dictionary<Guid, User> remoteUsers = new Dictionary<Guid, User>();
        internal readonly PluginFactory plugins;
        private readonly Dictionary<Keys, Action<IEditorTextBox>> singleEditorBindings
            = new Dictionary<Keys, Action<IEditorTextBox>>(),
            globalEditorBindings = new Dictionary<Keys, Action<IEditorTextBox>>();
        private volatile bool suspendTitleBarButtonEvents = false;
        internal readonly Settings settings;

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
            //button text alignment
            btnClient.TextAlign = btnServerNew.TextAlign = btnServerExisting.TextAlign
                = btnServerTemporary.TextAlign = ContentAlignment.MiddleCenter;
            //about label
            lblAbout.MouseEnter += (s, e) => { lblAbout.ForeColor = App.Theme.Foreground.Colour; };
            lblAbout.MouseLeave += (s, e) => { lblAbout.ForeColor = App.Theme.Foreground.LowContrast.Colour; };
            lblAbout.Click += (s, e) => { App.Website.LaunchWebsite(); };
            //version label
            lblVersion.Text = "v" + RegularExpressions.VersionTrailingZeroes.Replace(App.AssemblyVersion.ToString(),"");
            if (lblVersion.Text.IndexOf('.') == -1)
                lblVersion.Text += ".0";
            //debug label
#if DEBUG
            lblDebug.Text = instanceID.ToString();
#else
            lblDebug.Visible = false;
#endif
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
            settingsButton = new TitleBarButton(this);
            settingsButton.ToggleMode = true;
            settingsButton.Tag = panSettings;
            panSettings.Tag = settingsButton;
            settingsButton.CheckedChanged += TitleBarButton_CheckedChanged;
            //users menu
            usersButton = new TitleBarButton(this);
            usersButton.ToggleMode = true;
            usersButton.Tag = flpUsers;
            usersButton.Offset += settingsButton.Width;
            flpUsers.Tag = usersButton;
            usersButton.CheckedChanged += TitleBarButton_CheckedChanged;
            usersButton.Visible = false;
            //logout button
            logoutButton = new TitleBarButton(this);
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
                    + "\n\nYou can still use OTEX Editor, but the \"Public Documents\" list will be empty.",
                    exc.Message);
            }

            // KEY BINDINGS ////////////////////////////////////////////////////////////////////////
            globalEditorBindings[Keys.Control | Keys.W] = (tb) => { tb.LineEndingsVisible = !tb.LineEndingsVisible; };
            singleEditorBindings[Keys.Control | Keys.Q] = (tb) => { tb.ToggleCommentSelection(); };
            singleEditorBindings[Keys.Control | Keys.B] = (tb) => { tb.ToggleBookmark(); };
            singleEditorBindings[Keys.F2] = (tb) => { tb.NextBookmark(); };
            singleEditorBindings[Keys.Shift | Keys.F2] = (tb) => { tb.PreviousBookmark(); };
            singleEditorBindings[Keys.Control | Keys.Subtract] = (tb) => { tb.DecreaseZoom(); };
            singleEditorBindings[Keys.Control | Keys.Add] = (tb) => { tb.IncreaseZoom(); };
            singleEditorBindings[Keys.Control | Keys.NumPad0] = (tb) => { tb.ResetZoom(); };
            singleEditorBindings[Keys.Control | Keys.U] = (tb) => { tb.UppercaseSelection(); };
            singleEditorBindings[Keys.Control | Keys.Shift | Keys.U] = (tb) => { tb.LowercaseSelection(); };

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

            // CREATE SETTINGS MANAGER /////////////////////////////////////////////////////////////
            settings = new Settings();
            //update interval
            nudClientUpdateInterval.Value = (decimal)(otexClient.UpdateInterval = settings.UpdateInterval);
            settings.OnUpdateIntervalChanged += (u) => { otexClient.UpdateInterval = u.UpdateInterval; };
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
            //save settings
            settingsLoaded = true;
            App.Config.Flush();

            // HANDLE THEMES ///////////////////////////////////////////////////////////////////////
            App.ThemeChanged += (t) =>
            {
                this.Execute(() =>
                {
                    lblManualEntry.Font
                        = lblServerBrowser.Font
                        = lblSideBar.Font
                        = t.Font.Large.Regular;

                    lblTitle.Font = t.Font.Huge.Bold;

                    lblAbout.ForeColor
                        = lblVersion.ForeColor
                        = t.Foreground.LowContrast.Colour;

                    lblAbout.Font = t.Font.Underline;

                    settingsButton.Colour = t.Accent(0).Colour;
                    settingsButton.Image = App.Images.Resource("cog", App.Assembly, "OTEX.Editor");
                    settingsButton.InvertImage = settingsButton.InvertImageWhenChecked = !t.IsDark;

                    usersButton.Colour = t.Accent(1).Colour;
                    
                    logoutButton.Image = App.Images.Resource("logout", App.Assembly, "OTEX.Editor");
                    logoutButton.InvertImage = logoutButton.InvertImageWhenChecked = !t.IsDark;

                    sideSplitter.Panel1.Refresh();

                }, false);
            };
            App.Theme = App.Themes[settings.Theme];

            // CONFIGURE MAIN CONTENT PAGINATOR ////////////////////////////////////////////////////
            mainPaginator = new Paginator(splitter.Panel1);
            mainPaginator.Add("menu", panMenuPage);
            mainPaginator.Add("connecting", panConnectingPage);
            mainPaginator.Add("servers", panServerBrowserPage);
            mainPaginator.Add("editors", panEditors);
            mainPaginator.PageActivated += (s, k, p) =>
            {
                if (p == null)
                    return;
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
            lastConnectionReturnToServerBrowser = false;
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
            Action<IEditorTextBox> binding = null;
            if (singleEditorBindings.TryGetValue(keyData, out binding))
            {
                binding(activeEditor.TextBox);
                return true;
            }
            if (globalEditorBindings.TryGetValue(keyData, out binding))
            {
                foreach (var editor in editors)
                    binding(editor.Value.TextBox);
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override void OnFirstShown(EventArgs e)
        {
            base.OnFirstShown(e);
            if (IsDesignMode)
                return;
            languageManager.Load(Path.Combine(App.ExecutableDirectory, "..\\languages.xml"));

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

        private void btnClient_Click(object sender, EventArgs e)
        {
            mainPaginator.ActivePageKey = "servers";
        }

        private void btnClientCancel_Click(object sender, EventArgs e)
        {
            mainPaginator.ActivePageKey = "menu";
        }

        private void btnServerNew_Click(object sender, EventArgs e)
        {
            if (dlgServerCreateNew.ShowDialog() == DialogResult.OK)
            {
                Session session = new Session();
                session.AddDocument(dlgServerCreateNew.FileName, Document.ConflictResolutionStrategy.Replace, 4);
                session.Public = true;
                StartServerMode(session);
            }
        }

        private void btnServerExisting_Click(object sender, EventArgs e)
        {
            if (dlgServerOpenExisting.ShowDialog() == DialogResult.OK)
            {
                Session session = new Session();
                foreach (var file in dlgServerOpenExisting.FileNames)
                    session.AddDocument(file, Document.ConflictResolutionStrategy.Edit, 4);
                session.Public = true;
                StartServerMode(session);
            }
        }

        private void btnServerTemporary_Click(object sender, EventArgs e)
        {
            Session session = new Session();
            session.AddDocument("Temporary Document");
            session.Public = true;
            StartServerMode(session);
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
            lastConnectionReturnToServerBrowser = true;
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
            singleEditorBindings.Clear();
            globalEditorBindings.Clear();

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
            if (languageManager != null)
                languageManager.Dispose();

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
                mainPaginator.ActivePageKey = "servers";
            else
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
