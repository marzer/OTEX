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
        private volatile bool disableOperationGeneration = false;
        private FlyoutForm passwordForm = null, settingsForm = null;
        private volatile IPEndPoint lastConnectionEndpoint = null;
        private volatile Password lastConnectionPassword = null;
        private volatile string lastConnectionFailedReason = null;
        private volatile bool lastConnectionReturnToServerBrowser = false;
        private TitleBarButton logoutButton = null, settingsButton;
        private volatile bool settingsLoaded = false;

        private bool MainMenuPage
        {
            set
            {
                panMenuPage.Visible = value;
                panServerBrowserPage.Visible = !value;
                panConnectingPage.Visible = !value;
                tbEditor.Visible = !value;
                if (value)
                    panMenu.CenterInParent();
            }
        }

        private bool ServerBrowserPage
        {
            set
            {
                panMenuPage.Visible = !value;
                panServerBrowserPage.Visible = value;
                panConnectingPage.Visible = !value;
                tbEditor.Visible = !value;
            }
        }

        private bool ConnectingPageShared
        {
            set
            {
                panMenuPage.Visible = !value;
                panServerBrowserPage.Visible = !value;
                panConnectingPage.Visible = value;
                tbEditor.Visible = !value;
                if (value)
                {
                    lblConnectingStatus.CenterInParent();
                    panConnectingContent.CenterInParent();
                    panConnectingContent.Top = lblConnectingStatus.Bottom + 8;
                }
            }
        }

        private bool ConnectingPage
        {
            set
            {
                ConnectingPageShared = value;
                if (value)
                {
                    lblConnectingStatus.Text = string.Format("Connecting to {0}...", lastConnectionEndpoint);
                    btnConnectingBack.Visible = false;
                    btnConnectingReconnect.Visible = false;
                }
            }
        }

        private bool ConnectingFailedPage
        {
            set
            {
                ConnectingPageShared = value;
                if (value)
                {
                    lblConnectingStatus.Text = string.Format("Could not connect to {0}.\r\n\r\n{1}",
                        lastConnectionEndpoint, lastConnectionFailedReason);
                    btnConnectingBack.Visible = true;
                    btnConnectingReconnect.Visible = true;
                }
            }
        }

        private bool ConnectionLostPage
        {
            set
            {
                ConnectingPageShared = value;
                if (value)
                {
                    lblConnectingStatus.Text = string.Format("Connection to {0} was lost.",
                        lastConnectionEndpoint);
                    btnConnectingBack.Visible = true;
                    btnConnectingReconnect.Visible = true;
                }
            }
        }

        private bool EditorPage
        {
            get { return tbEditor.Visible; }
            set
            {
                panMenuPage.Visible = !value;
                panServerBrowserPage.Visible = !value;
                panConnectingPage.Visible = !value;
                tbEditor.Visible = value;
            }
        }

        [Serializable]
        private class EditorClient
        {
            public int SelectionStart = 0;
            public int SelectionEnd = 0;
            public int Colour = unchecked((int)0xFFFFFFFF);
        }
        private readonly Dictionary<Guid, EditorClient> remoteClients
            = new Dictionary<Guid, EditorClient>();
        private readonly EditorClient localClient = new EditorClient();

        private Color ClientColour
        {
            get { return localClient.Colour.ToColour(); }
            set
            {
                //check value
                var newVal = value;
                if (newVal.A != 255)
                    newVal = Color.FromArgb(255, newVal);
                var intVal = newVal.ToArgb();
                if (localClient.Colour == intVal)
                    return;
                localClient.Colour = intVal;

                //push to server
                otexClient.Metadata = localClient.Serialize();

                //update locally
                this.Execute(() =>
                {
                    tbEditor.SelectionColor
                        = tbEditor.CurrentLineColor
                        = tbEditor.LineNumberColor
                        = localClient.Colour.ToColour();
                    tbEditor.CaretColor = localClient.Colour.ToColour().Brighten(0.3f);
                    if (tbEditor.Visible)
                        tbEditor.Refresh();
                });

                //update in settings file
                if (settingsLoaded)
                {
                    App.Config.User.Set("user.colour", newVal);
                    App.Config.User.Flush();
                }
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

            
            //splash panel
            panMenuPage.Dock = DockStyle.Fill;

            //colours
            btnServerNew.Accent = btnServerExisting.Accent = 2;
            btnServerTemporary.Accent = 1;

            //about link
            lblAbout.Cursor = Cursors.Hand;
            lblAbout.MouseEnter += (s, e) => { lblAbout.ForeColor = App.Theme.Foreground.Colour; };
            lblAbout.MouseLeave += (s, e) => { lblAbout.ForeColor = App.Theme.Foreground.LowContrast.Colour; };
            lblAbout.Click += (s, e) => { App.Website.LaunchWebsite(); };

            //version label
            lblVersion.Text = "v" + Marzersoft.Text.REGEX_VERSION_REPEATING_ZEROES.Replace(App.AssemblyVersion.ToString(),"");
            if (lblVersion.Text.IndexOf('.') == -1)
                lblVersion.Text += ".0";

            //client connection/server browser panel
            btnClient.TextAlign = btnServerNew.TextAlign = btnServerExisting.TextAlign
                = btnServerTemporary.TextAlign = ContentAlignment.MiddleCenter;
            btnClientConnect.Image = App.Images.Resource("next");
            btnClientCancel.Image = App.Images.Resource("previous");
            btnClientConnect.ImageAlign = btnClientCancel.ImageAlign = ContentAlignment.MiddleCenter;

            panServerBrowserPage.Dock = DockStyle.Fill;
            dgvServers.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dgvServers.Columns[1].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dgvServers.ShowCellToolTips = true;

            //'connecting' page
            panConnectingPage.Dock = DockStyle.Fill;
            lblConnectingStatus.Width = panConnectingPage.ClientSize.Width - 10;
            btnConnectingReconnect.Image = App.Images.Resource("refresh");
            btnConnectingBack.Image = App.Images.Resource("previous");

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
            TextFlourishes = false;
            Text = App.Name;

            //settings menu
            settingsForm = new FlyoutForm(panSettings);
            settingsForm.Accent = 1;
            settingsButton = AddCustomTitleBarButton();
            settingsButton.Click += (b) => { settingsForm.Flyout(PointToScreen(b.Bounds.BottomMiddle())); };
            cbClientColour.ShowNames = false;

            //logout button
            logoutButton = AddCustomTitleBarButton();
            logoutButton.Colour = Color.Red;
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
                Logger.W("Client: {0}: {1}", e.InnerException.GetType().Name, e.InnerException.Message);
            };
            otexClient.OnConnected += (c) =>
            {
                Logger.I("Client: connected to {0}:{1}.", c.ServerAddress, c.ServerPort);
                this.Execute(() =>
                {
                    disableOperationGeneration = true;
                    tbEditor.Text = "";
                    disableOperationGeneration = false;

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
                    EditorPage = true;
                    tbEditor.Focus();
                }, false);
            };
            otexClient.OnRemoteOperations += (c,operations) =>
            {
                this.Execute(() =>
                {
                    disableOperationGeneration = true;
                    
                    foreach (var operation in operations)
                    {
                        if (operation.IsInsertion)
                        {
                            InsertTextAndRestoreSelection(
                                new Range(tbEditor, tbEditor.PositionToPlace(operation.Offset),
                                    tbEditor.PositionToPlace(operation.Offset)),
                                operation.Text, null);
                        }
                        else if (operation.IsDeletion)
                        {
                            InsertTextAndRestoreSelection(
                                new Range(tbEditor, tbEditor.PositionToPlace(operation.Offset),
                                    tbEditor.PositionToPlace(operation.Offset + operation.Length)),
                                "", null);
                        }
                    }

                    disableOperationGeneration = false;
                }, false);
            };
            otexClient.OnRemoteMetadata += (c, id, md) =>
            {
                lock (remoteClients)
                {
                    if (md != null)
                        remoteClients[id] = md.Deserialize<EditorClient>();
                }
                this.Execute(() => { tbEditor.Refresh(); });
            };
            otexClient.OnDisconnected += (c, serverSide) =>
            {
                Logger.I("Client: disconnected{0}.", serverSide ? " (connection closed by server)" : "");
                lock (remoteClients)
                {
                    remoteClients.Clear();
                }
                localClient.SelectionStart = 0;
                localClient.SelectionEnd = 0;
                if (!closing)
                {
                    this.Execute(() =>
                    {
                        //non-host
                        if (serverSide)
                        {
                            if (lastConnectionEndpoint != null)
                                ConnectionLostPage = true;
                            else
                                MainMenuPage = true;
                        }
                        else
                            MainMenuPage = true;

                        Text = App.Name;
                        logoutButton.Visible = false;
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

            // CREATE TEXT EDITOR ////////////////////////////////////////////////
            tbEditor = new FastColoredTextBox();
            tbEditor.Parent = this;
            tbEditor.Dock = DockStyle.Fill;
            tbEditor.ReservedCountOfLineNumberChars = 4;
            tbEditor.WordWrap = true;
            tbEditor.WordWrapAutoIndent = true;
            tbEditor.WordWrapMode = WordWrapMode.WordWrapControlWidth;
            tbEditor.TabLength = 4;
            tbEditor.LineInterval = 2;
            tbEditor.HotkeysMapping.Remove(Keys.Control | Keys.H); //remove default "replace" (CTRL + H, wtf?)
            tbEditor.HotkeysMapping[Keys.Control | Keys.R] = FCTBAction.ReplaceDialog; // CTRL + R for replace
            tbEditor.HotkeysMapping[Keys.Control | Keys.Y] = FCTBAction.Undo; // CTRL + Y for undo
            tbEditor.TextChanging += (sender, args) =>
            {
                if (disableOperationGeneration || !otexClient.Connected)
                    return;

                //cache previous version of the text
                previousText = tbEditor.Text;
            };
            tbEditor.TextChanged += (sender, args) =>
            {
                if (disableOperationGeneration || !otexClient.Connected)
                    return;

                //do diff on two versions of text
                var currentText = tbEditor.Text;
                var diffs = Diff.Calculate(previousText.ToCharArray(), currentText.ToCharArray());

                //convert changes into operations
                int position = 0;
                foreach (var diff in diffs)
                {
                    //skip unchanged characters
                    position = Math.Min(diff.InsertStart, currentText.Length);

                    //process a deletion
                    if (diff.DeleteLength > 0)
                        otexClient.Delete((uint)position, (uint)diff.DeleteLength);

                    //process an insertion
                    if (position < (diff.InsertStart + diff.InsertLength))
                        otexClient.Insert((uint)position, currentText.Substring(position, diff.InsertLength));
                }
            };
            tbEditor.SelectionChanged += (s, e) =>
            {
                if (!otexClient.Connected)
                    return;
                var sel = tbEditor.Selection;
                localClient.SelectionStart = tbEditor.PlaceToPosition(sel.Start);
                localClient.SelectionEnd = tbEditor.PlaceToPosition(sel.End);
                otexClient.Metadata = localClient.Serialize();
            };
            tbEditor.PaintLine += (s, e) =>
            {
                if (!otexClient.Connected || remoteClients.Count == 0)
                    return;

                Range lineRange = new Range(tbEditor, e.LineIndex);
                lock (remoteClients)
                {
                    var len = tbEditor.TextLength;
                    foreach (var kvp in remoteClients)
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
                        int colour = kvp.Value.Colour & 0x00FFFFFF;

                        //draw "current line" fill
                        if (caret && selRange.Length == 0)
                        {
                            using (SolidBrush b = new SolidBrush((colour | 0x09000000).ToColour()))
                                e.Graphics.FillRectangle(b, e.LineRect);
                        }
                        //draw highlight
                        if (range.Length > 0)
                        {
                            using (SolidBrush b = new SolidBrush((colour | 0x20000000).ToColour()))
                                e.Graphics.FillRectangle(b, new Rectangle(ptStart.X, e.LineRect.Y,
                                    ptEnd.X - ptStart.X, e.LineRect.Height));
                        }
                        //draw caret
                        if (caret)
                        {
                            ptStart = tbEditor.PlaceToPoint(selStart);
                            using (Pen p = new Pen((colour | 0xBB000000).ToColour()))
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
                    using (Pen p = new Pen(((localClient.Colour & 0x00FFFFFF) | 0x30000000).ToColour()))
                    {
                        p.Width = 2;
                        p.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                        e.Graphics.DrawLine(p, new Point(pt.X, e.ClipRectangle.Top), new Point(pt.X, e.ClipRectangle.Bottom));
                    }
                }
            };

            // CLIENT SETTINGS ///////////////////////////////////////////////////
            cbClientColour.RegenerateItems(
                false, //darks
                true, //mids
                false, //lights
                false, //transparents
                false, //monochromatics
                0.15f, //similarity threshold
                new Color[] { Color.Blue, Color.MediumBlue, Color.Red, Color.Fuchsia, Color.Magenta } //exlude (colours that contrast poorly with the app theme)
            );
            //read user colour
            {
                var colours = cbClientColour.Items.OfType<Color>().ToList();
                Color colour = App.Config.User.Get("user.colour", Color.Transparent);
                if (colour == Color.Transparent)
                    colour = otexClient.ID.ToColour(colours.ToArray());
                if (colour.A != 255)
                    colour = Color.FromArgb(255, colour);
                var colourIndex = colours.FindIndex((c) => { return c.ToArgb() == colour.ToArgb(); });
                if (colourIndex >= 0)
                    cbClientColour.SelectedIndex = colourIndex;
                ClientColour = colour;
                App.Config.User.Set("user.colour", colour);
            }
            //read update interval
            otexClient.UpdateInterval = App.Config.User.Get("client.interval", 1.0f);
            nudClientUpdateInterval.Value = (decimal)otexClient.UpdateInterval;
            App.Config.User.Set("client.interval", otexClient.UpdateInterval);
            //read line length ruler
            cbLineLength.Checked = App.Config.User.Get("user.ruler", true);
            App.Config.User.Set("user.ruler", cbLineLength.Checked);
            nudLineLength.Value = App.Config.User.Get("user.ruler_width", 100);
            App.Config.User.Set("user.ruler_width", ((int)nudLineLength.Value).Clamp(60, 200));
            //read last direct connection address
            tbClientAddress.Text = App.Config.User.Get("user.last_direct_connection", "").Trim();
            if (tbClientAddress.Text.Length == 0)
                tbClientAddress.Text = "127.0.0.1";
            //read theme
            cbTheme.Items.Add("Dark");
            cbTheme.Items.Add("Light");
            App.Config.User.Default("user.theme", 0, "dark");
            for (int i = 0; i < cbTheme.Items.Count; ++i)
            {
                if ((cbTheme.Items[i] as string).Trim().ToLower().Equals(App.Config.User.Get("user.theme", "dark").Trim().ToLower()))
                {
                    cbTheme.SelectedIndex = i;
                    break;
                }
            }
            //save settings
            settingsLoaded = true;
            App.Config.User.Flush();

            // HANDLE THEMES /////////////////////////////////////////////////////
            App.ThemeChanged += (t) =>
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

                tbEditor.BackBrush = t.Workspace.Brush;
                tbEditor.IndentBackColor = t.Workspace.Colour;
                tbEditor.ServiceLinesColor = t.Workspace.HighContrast.Colour;
                tbEditor.Font = t.Monospaced.Regular;

                settingsButton.Image = App.Images.Resource("cog" + (t.IsDark ? "" : "_black"), App.Assembly, "OTEX");
                logoutButton.Image = App.Images.Resource("logout" + (t.IsDark ? "" : "_black"), App.Assembly, "OTEX");
            };
            App.Theme = App.Themes[App.Config.User.Get("user.theme", "dark")];

            cbTheme.SelectedIndexChanged += (s, e) =>
            {
                App.Config.User.Set("user.theme", cbTheme.Items[cbTheme.SelectedIndex] as string);
                App.Config.User.Flush();
                App.Theme = App.Themes[App.Config.User.Get("user.theme", "dark")];
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
                otexClient.Metadata = localClient.Serialize();
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
            lastConnectionFailedReason = null;
            lastConnectionReturnToServerBrowser = false;

            //started ok, set title text
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
            long port = Server.DefaultPort;
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
            if (port < 1024 || port > 65535 || Server.AnnouncePorts.Contains(port))
            {
                Logger.ErrorMessage("Port must be between 1024-{0} or {1}-65535.",
                                    Server.AnnouncePorts.First - 1, Server.AnnouncePorts.Last + 1);
                return false;
            }

            return StartClientMode(new IPEndPoint(address, (int)port), passwordString);
        }

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
            lastConnectionFailedReason = null;
            ConnectingPage = true;

            //start connection thread
            clientConnectingThread = new Thread(() =>
            {
                try
                {
                    otexClient.Metadata = localClient.Serialize();
                    otexClient.Connect(lastConnectionEndpoint, lastConnectionPassword);
                }
                catch (Exception exc)
                {
                    this.Execute(() =>
                    {
                        lastConnectionFailedReason = exc.Message;
                        ConnectingFailedPage = true;
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
            MainMenuPage = true;
            Refresh();
        }

        private void btnClient_Click(object sender, EventArgs e)
        {
            ServerBrowserPage = true;
        }

        private void btnClientCancel_Click(object sender, EventArgs e)
        {
            MainMenuPage = true;
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
            lastConnectionReturnToServerBrowser = true;
            if (StartClientMode(tbClientAddress.Text.Trim(), tbClientPassword.Text.Trim()))
                App.Config.User.Set("user.last_direct_connection", tbClientAddress.Text.Trim());
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
            lblConnectingStatus.CenterInParent();
            panConnectingContent.CenterInParent();
            panConnectingContent.Top = lblConnectingStatus.Bottom + 8;
        }

        private void panMenuPage_Resize(object sender, EventArgs e)
        {
            panMenu.CenterInParent();
        }

        private void btnConnectingBack_Click(object sender, EventArgs e)
        {
            if (lastConnectionReturnToServerBrowser)
                ServerBrowserPage = true;
            else
                MainMenuPage = true;
        }

        private void btnConnectingReconnect_Click(object sender, EventArgs e)
        {
            StartClientMode(lastConnectionEndpoint, lastConnectionPassword);
        }

        private void cbClientColour_SelectedIndexChanged(object sender, EventArgs e)
        {
            ClientColour = (Color)cbClientColour.Items[cbClientColour.SelectedIndex];
        }

        private void cbLineLength_CheckedChanged(object sender, EventArgs e)
        {
            nudLineLength.Enabled = cbLineLength.Checked;
            if (tbEditor.Visible)
                tbEditor.Refresh();
            if (settingsLoaded)
            {
                App.Config.User.Set("user.ruler", cbLineLength.Checked);
                App.Config.User.Flush();
            }
        }

        private void nudLineLength_ValueChanged(object sender, EventArgs e)
        {
            if (tbEditor.Visible)
                tbEditor.Refresh();
            if (settingsLoaded)
            {
                App.Config.User.Set("user.ruler_width", ((int)nudLineLength.Value).Clamp(60, 200));
                App.Config.User.Flush();
            }
        }

        private void nudClientUpdateInterval_ValueChanged(object sender, EventArgs e)
        {
            otexClient.UpdateInterval = (float)nudClientUpdateInterval.Value;
            if (settingsLoaded)
            {
                App.Config.User.Set("client.interval", otexClient.UpdateInterval);
                App.Config.User.Flush();
            }
        }

        //this is a version of FCTB's InsertTextAndRestoreSelection that does not move the bloody scroll window
        private Range InsertTextAndRestoreSelection(Range replaceRange, string text, Style style)
        {
            if (text == null)
                return null;

            var oldStart = tbEditor.PlaceToPosition(tbEditor.Selection.Start);
            var oldEnd = tbEditor.PlaceToPosition(tbEditor.Selection.End);
            var count = replaceRange.Text.Length;
            var pos = tbEditor.PlaceToPosition(replaceRange.Start);
            //
            tbEditor.Selection.BeginUpdate();
            tbEditor.Selection = replaceRange;
            var range = tbEditor.InsertText(text, style, false);
            //
            count = range.Text.Length - count;
            tbEditor.Selection.Start = tbEditor.PositionToPlace(oldStart + (oldStart >= pos ? count : 0));
            tbEditor.Selection.End = tbEditor.PositionToPlace(oldEnd + (oldEnd >= pos ? count : 0));
            tbEditor.Selection.EndUpdate();
            return range;
        }
    }
}
