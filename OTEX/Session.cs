using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Marzersoft;
using System.IO;
using System.Security.AccessControl;
using System.Security;
using System.Net;
using System.Collections.ObjectModel;

namespace OTEX
{
    /// <summary>
    /// An OTEX session. Create these server-side to use with Server's Start() to initialize the session.
    /// Also serialized and sent to new clients as part of the initial ConnectionResponse packet.
    /// </summary>
    [Serializable]
    public sealed class Session : ISession
    {
        /////////////////////////////////////////////////////////////////////
        // PROPERTIES/VARIABLES
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// ID of the session (always matches the server; set internally).
        /// </summary>
        public Guid ID { get; internal set; }

        /// <summary>
        /// Collection of files in the session.
        /// </summary>
        /// <exception cref="InvalidOperationException" />
        public ReadOnlyDictionary<Guid, Document> Documents
        {
            get
            {
                if (documentsReadOnly == null)
                    documentsReadOnly = new ReadOnlyDictionary<Guid, Document>(documents);
                return documentsReadOnly;

            }
        }
        internal Dictionary<Guid, Document> documents = null;
        [NonSerialized]
        private ReadOnlyDictionary<Guid, Document> documentsReadOnly = null;

        /// <summary>
        /// Listening for new client connections will bind to this port (supports IPv4 and IPv6).
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException" />
        /// <exception cref="InvalidOperationException" />
        public ushort Port
        {
            get { return port; }
            set
            {
                if (ReadOnly)
                    throw new InvalidOperationException("Setting Port is only valid server-side");
                if (value < 1024 || Server.AnnouncePorts.Contains(value))
                    throw new ArgumentOutOfRangeException("Port",
                        string.Format("Port must be between 1024-{0} or {1}-65535.",
                            Server.AnnouncePorts.First - 1, Server.AnnouncePorts.Last + 1));
                port = value;
            }
        }
        private ushort port = Server.DefaultPort;

        /// <summary>
        /// A password required for clients to connect to this session (null == no password required).
        /// </summary>
        /// <exception cref="InvalidOperationException" />
        public Password Password
        {
            get { return password; }
            set
            {
                if (ReadOnly)
                    throw new InvalidOperationException("Setting Password is only valid server-side");
                password = value;
            }
        }
        [NonSerialized]
        private Password password = null;

        /// <summary>
        /// Advertise the presence of this session so it shows up in local server browsers.
        /// </summary>
        /// <exception cref="InvalidOperationException" />
        public bool Public
        {
            get { return isPublic; }
            set
            {
                if (ReadOnly)
                    throw new InvalidOperationException("Setting Public is only valid server-side");
                isPublic = value;
            }
        }
        private bool isPublic = false;

        /// <summary>
        /// The friendly name of the session.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException" />
        /// <exception cref="InvalidOperationException" />
        public string Name
        {
            get { return name; }
            set
            {
                if (ReadOnly)
                    throw new InvalidOperationException("Setting Name is only valid server-side");
                if ((value = (value ?? "").Trim()).Length > 32)
                    throw new ArgumentOutOfRangeException("Name", "Name may not be longer than 32 characters.");
                name = value;
            }
        }
        private string name = "";

        /// <summary>
        /// How many clients are allowed to be connected at once?
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException" />
        /// <exception cref="InvalidOperationException" />
        public uint ClientLimit
        {
            get { return maxClients; }
            set
            {
                if (ReadOnly)
                    throw new InvalidOperationException("Setting ClientLimit is only valid server-side");
                if (value == 0 || value > 100)
                    throw new ArgumentOutOfRangeException("ClientLimit", "ClientLimit must be between 1-100.");
                maxClients = value;
            }
        }
        private uint maxClients = 10;

        /// <summary>
        /// How many clients are currently connected?
        /// </summary>
        public uint ClientCount
        {
            get { return Clients == null ? RemoteClientCount : (uint)Clients.Count; }
        }
        [NonSerialized]
        internal uint RemoteClientCount = 0;

        /// <summary>
        /// Path to plain text file containing list of clients who have been banned from the server.
        /// If no path is provided, bans will not be read from or written to disk.
        /// </summary>
        /// <exception cref="InvalidOperationException" />
        public string BanListPath
        {
            get { return banListPath; }
            set
            {
                if (ReadOnly)
                    throw new InvalidOperationException("Setting BanListPath is only valid server-side");
                banListPath =  (value ?? "").Trim();
            }
        }
        [NonSerialized]
        private string banListPath = "";

        /// <summary>
        /// Initial collection of bans. Can be used in tandem with BanListPath; if a file exists, the contents
        /// will be merged into this set.
        /// </summary>
        [NonSerialized]
        internal HashSet<Guid> BanList = null;

        /// <summary>
        /// All currently connected clients.
        /// </summary>
        internal Dictionary<Guid, RemoteClient> Clients = null;

        /// <summary>
        /// Internal "have I been stored for server use?" flag
        /// </summary>
        [NonSerialized]
        internal volatile bool ReadOnly = false;

        /// <summary>
        /// Address of the server (set client-side; ignore server-side).
        /// </summary>
        public IPAddress Address
        {
            get { return address; }
        }
        [NonSerialized]
        internal IPAddress address = null;

        /////////////////////////////////////////////////////////////////////
        // CONSTRUCTORS
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Default constructor (server-side).
        /// </summary>
        public Session()
        {
            documents = new Dictionary<Guid, Document>();
            BanList = new HashSet<Guid>();
        }

        /// <summary>
        /// Internal copy constructor (server-side).
        /// </summary>
        internal Session(Session s)
        {
            documents = new Dictionary<Guid, Document>();
            lock (s.documents)
            {
                foreach (var kvp in s.documents)
                    documents[kvp.Key] = new Document(kvp.Value);
            }
            Clients = new Dictionary<Guid, RemoteClient>();
            Port = s.Port;
            Password = s.Password;
            Public = s.Public;
            Name = s.Name;
            ClientLimit = s.ClientLimit;
            BanListPath = s.BanListPath;
            lock (s.BanList)
                BanList = new HashSet<Guid>(s.BanList);
            ReadOnly = true;
        }

        /////////////////////////////////////////////////////////////////////
        // SERVER-SIDE SESSION CONFIGURATION
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Adds a temporary document to the session.
        /// </summary>
        /// <param name="name">A descriptive "name" for the temporary document. Even though it will not
        /// be backed by a file, using a file extension can still be useful for the purposes of default
        /// syntax highlighting engines (e.g. a temporary file meant as a C# "scratchpad").</param>
        /// <exception cref="ArgumentOutOfRangeException" />
        /// <exception cref="InvalidOperationException" />
        public Document AddDocument(string name)
        {
            if (ReadOnly)
                throw new InvalidOperationException("AddDocument is only valid server-side");
            var doc = new Document(name);
            lock (documents)
                documents[doc.ID] = doc;
            return doc;
        }

        /// <summary>
        /// Constructor for a file-backed document (server-side).
        /// </summary>
        /// <param name="path">Path to the local file.</param>
        /// <param name="conflictStrategy">What to do if the file exists.</param>
        /// <param name="tabWidth">When loading files, how many spaces should tab characters be replaced with?</param>
        /// <exception cref="ArgumentOutOfRangeException" />
        /// <exception cref="InvalidOperationException" />
        public Document AddDocument(string path, Document.ConflictResolutionStrategy conflictStrategy, uint tabWidth = 4)
        {
            if (ReadOnly)
                throw new InvalidOperationException("AddDocument is only valid server-side");
            var doc = new Document(path, conflictStrategy, tabWidth);
            lock (documents)
                documents[doc.ID] = doc;
            return doc;
        }

        /// <summary>
        /// Removes a document from a session configuration (server-side).
        /// </summary>
        /// <param name="doc">Document object to remove.</param>
        /// <exception cref="InvalidOperationException" />
        /// <exception cref="ArgumentNullException" />
        public bool RemoveDocument(Document doc)
        {
            if (ReadOnly)
                throw new InvalidOperationException("RemoveDocument is only valid server-side");
            if (doc == null)
                throw new ArgumentNullException("doc");
            return documents.Remove(doc.ID);
        }

        /// <summary>
        /// Removes a document from a session configuration (server-side).
        /// </summary>
        /// <param name="documentID">ID of document object to remove.</param>
        /// <exception cref="InvalidOperationException" />
        /// <exception cref="ArgumentOutOfRangeException" />
        public bool RemoveDocument(Guid documentID)
        {
            if (ReadOnly)
                throw new InvalidOperationException("RemoveDocument is only valid server-side");
            if (documentID == Guid.Empty)
                throw new ArgumentOutOfRangeException("documentID", "documentID cannot be Guid.Empty");
            return documents.Remove(documentID);
        }

        /// <summary>
        /// Adds a client to the banlist for the session.
        /// </summary>
        /// <param name="id">The id of the client to ban.</param>
        /// <exception cref="ArgumentOutOfRangeException" />
        /// <exception cref="InvalidOperationException" />
        public Session AddBan(Guid clientID)
        {
            if (ReadOnly)
                throw new InvalidOperationException("AddBan is only valid server-side");
            if (clientID == Guid.Empty)
                throw new ArgumentOutOfRangeException("clientID", "clientID cannot be Guid.Empty");
            lock (BanList)
                BanList.Add(clientID);
            return this;
        }

        /////////////////////////////////////////////////////////////////////
        // SERVER-SIDE INITIALIZATION
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Initialize the session, performing any final validation and loading documents. Throws on failure.
        /// </summary>
        internal void Initialize()
        {
            //load documents
            if (documents.Count == 0)
                throw new ArgumentOutOfRangeException("Documents", "session must contain at least one document");
            var docs = new List<Document>();
            var paths = new HashSet<string>();
            try
            {
                foreach (var kvp in documents)
                {
                    if (!kvp.Value.Initialize())
                        continue;
                    docs.Add(kvp.Value);
                    if (!kvp.Value.Temporary)
                    {
                        var p = Path.GetFullPath(kvp.Value.Path).ToLower();
                        if (paths.Contains(p))
                            throw new ArgumentOutOfRangeException("Documents", string.Format("duplicate file: {0}", kvp.Value.Path));
                        paths.Add(p);
                    }
                }
                if (documents.Count == 0)
                    throw new ArgumentOutOfRangeException("Documents", "session must contain at least one document (all were skipped)");
                documents.Clear();
                foreach (var doc in docs)
                    documents[doc.ID] = doc;

                //load ban list
                if (BanListPath.Length > 0)
                {
                    BanListPath = Path.GetFullPath(BanListPath);
                    if (Directory.Exists(BanListPath))
                        throw new FileNotFoundException("BanListPath was a directory", BanListPath);
                    var dir = Path.GetDirectoryName(BanListPath);
                    if (!Directory.Exists(dir))
                        throw new DirectoryNotFoundException("BanListPath's directory did not exist");
                    if (!dir.HasPermissions(FileSystemRights.CreateFiles | FileSystemRights.Write))
                        throw new SecurityException("User does not have sufficient file system rights.");

                    if (File.Exists(BanListPath))
                    {
                        var initialCount = BanList.Count;
                        var lines = File.ReadAllLines(BanListPath, BanListPath.DetectEncoding());
                        for (int i = 0; i < lines.Length; ++i)
                        {
                            if ((lines[i] = lines[i].Trim()).Length == 0)
                                continue;
                            Guid guid;
                            if (lines[i].TryParse(out guid))
                                BanList.Add(guid);
                        }

                        if (initialCount > 0)
                            FlushBanList();
                    }
                }
            }
            catch (Exception)
            {
                CloseDocuments();
                throw;
            }
        }

        /////////////////////////////////////////////////////////////////////
        // SERVER-SIDE FILE FLUSHING
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Writes the list of bans to disk.
        /// </summary>
        internal void FlushBanList()
        {
            if (BanListPath.Length == 0)
                return;

            StringBuilder sb = new StringBuilder();
            foreach (var ban in BanList)
                sb.AppendLine(ban.ToString());
            File.WriteAllText(BanListPath, sb.ToString());
        }

        /// <summary>
        /// Applies the output of all new operations to the internal file contents strings, then
        /// writes the new contents to disk.
        /// </summary>
        internal void FlushDocuments()
        {
            foreach (var kvp in documents)
                kvp.Value.FlushDocument();
        }

        /////////////////////////////////////////////////////////////////////
        // SERVER-SIDE FILE CLOSING
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Closes exclusive locks on document files.
        /// </summary>
        internal void CloseDocuments()
        {
            foreach (var kvp in documents)
                kvp.Value.CloseDocument();
        }
    }
}
