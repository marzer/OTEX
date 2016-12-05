using System;
using System.Collections.Generic;
using PATH = System.IO.Path;
using System.IO;
using Marzersoft;
using System.Security.AccessControl;
using System.Security;

namespace OTEX
{
    /// <summary>
    /// A document in an OTEX session. Create these server-side and add them to your server's Session for Start().
    /// Also serialized and sent to new clients as part of the initial ConnectionResponse packet.
    /// </summary>
    [Serializable]
    public sealed class Document
    {
        /////////////////////////////////////////////////////////////////////
        // PROPERTIES/VARIABLES
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Unique identifier for this file.
        /// </summary>
        public Guid ID { get; private set; }

        /// <summary>
        /// Is this a temporary document (i.e. will it be backed by a file on disk, or entirely in memory and lost
        /// on shutdown)?.
        /// </summary>
        public bool Temporary { get; private set; }

        /// <summary>
        /// Path to the text file you'd like clients to edit/create, or if Temporary == true, a descriptive "name" for the document
        /// (e.g. "Random notes.txt", "Scratchpad", etc.).
        /// </summary>
        public string Path { get; private set; }

        /// <summary>
        /// How to handle a file name conflict
        /// </summary>
        public enum ConflictResolutionStrategy : uint
        {
            /// <summary>
            /// Load the file for editing. This is default.
            /// </summary>
            Edit,

            /// <summary>
            /// Overwrites the file on disk with the contents of a new session.
            /// </summary>
            Replace,

            /// <summary>
            /// Skip the path entirely.
            /// </summary>
            Skip
        }

        /// <summary>
        /// What to do if the file exists already (assuming Temporary == false)?
        /// </summary>
        public ConflictResolutionStrategy ConflictResolution { get; private set; }

        /// <summary>
        /// When loading files, how many spaces should tab characters be replaced with?
        /// </summary>
        public uint TabWidth { get; private set; }

        /// <summary>
        /// Contents of the file at last sync.
        /// </summary>
        [NonSerialized]
        internal string Contents = "";

        /// <summary>
        /// Starting operation index for next sync.
        /// </summary>
        [NonSerialized]
        internal int SyncIndex = 0;

        /// <summary>
        /// Line ending scheme used by loaded file (defaults to CRLF on windows).
        /// </summary>
        [NonSerialized]
        internal string LineEndings = Environment.NewLine;

        /// <summary>
        /// Master list of operations, used for synchronizing newly-connected clients.
        /// </summary>
        internal List<Operation> MasterOperations = null;

        /////////////////////////////////////////////////////////////////////
        // CONSTRUCTORS
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructor for a temporary document (server-side).
        /// </summary>
        /// <param name="name">A descriptive "name" for the temporary document. Even though it will not
        /// be backed by a file, using a file extension can still be useful for the purposes of default
        /// syntax highlighting engines (e.g. a temporary file meant as a C# "scratchpad").</param>
        /// <exception cref="ArgumentOutOfRangeException" />
        internal Document(string name)
        {
            ID = Guid.NewGuid();
            if ((name = (name ?? "").Trim()).Length > 32)
                throw new ArgumentOutOfRangeException("name", "Temporary document names may not be longer than 32 characters.");
            if (name.Length == 0)
                throw new ArgumentOutOfRangeException("name", "Temporary document names cannot be blank.");
            Path = name;
            Temporary = true;
        }

        /// <summary>
        /// Constructor for a file-backed document (server-side).
        /// </summary>
        /// <param name="path">Path to the local file.</param>
        /// <param name="conflictStrategy">What to do if the file exists.</param>
        /// <param name="tabWidth">When loading files, how many spaces should tab characters be replaced with?</param>
        /// <exception cref="ArgumentOutOfRangeException" />
        internal Document(string path, ConflictResolutionStrategy conflictStrategy, uint tabWidth = 4)
        {
            ID = Guid.NewGuid();
            if ((path = (path ?? "").Trim()).Length > 260)
                throw new ArgumentOutOfRangeException("path", "Document paths may not be longer than 260 characters.");
            if (path.Length == 0)
                throw new ArgumentOutOfRangeException("path", "Document paths cannot be blank.");
            if (conflictStrategy > ConflictResolutionStrategy.Skip)
                throw new ArgumentOutOfRangeException("conflictStrategy", "What?? Why??");
            if (tabWidth > 16)
                throw new ArgumentOutOfRangeException("tabWidth", "tabWidth must be between 0-16.");
            Path = path;
            Temporary = false;
            ConflictResolution = conflictStrategy;
            TabWidth = tabWidth;
        }

        /// <summary>
        /// Internal copy constructor.
        /// </summary>
        internal Document(Document d)
        {
            ID = d.ID;
            Path = d.Path;
            Temporary = d.Temporary;
            ConflictResolution = d.ConflictResolution;
        }

        /////////////////////////////////////////////////////////////////////
        // SERVER-SIDE INITIALIZATION
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Initialize the document.
        /// </summary>
        /// <returns>True if the document loaded successfully according to the current conflict resolution strategy,
        /// False otherwise (false does not indicate an error state; these are indicated by exceptions).</returns>
        /// <exception cref="NotSupportedException" />
        /// <exception cref="PathTooLongException" />
        /// <exception cref="FileNotFoundException" />
        /// <exception cref="DirectoryNotFoundException" />
        /// <exception cref="SecurityException" />
        internal bool Initialize()
        {
            if (Temporary)
            {
                MasterOperations = new List<Operation>();
                return true;
            }
            var path = PATH.GetFullPath(Path);
            if (Directory.Exists(path))
                throw new FileNotFoundException("Path was a directory", Path);
            var dir = PATH.GetDirectoryName(path);
            if (!Directory.Exists(dir))
                throw new DirectoryNotFoundException("File's directory did not exist");
            //if (!dir.HasPermissions(FileSystemRights.WriteData))
              //  throw new SecurityException("User does not have sufficient file system rights.");
            if (!File.Exists(path))
            {
                MasterOperations = new List<Operation>();
                return true;
            }
            if (ConflictResolution == ConflictResolutionStrategy.Skip)
                return false;

            MasterOperations = new List<Operation>();
            if (ConflictResolution == ConflictResolutionStrategy.Edit)
            {
                //read file
                Contents = File.ReadAllText(path, path.DetectEncoding());

                //replace tabs with spaces
                if (TabWidth > 0)
                    Contents = Contents.Replace("\t", new string(' ', (int)TabWidth));

                //detect line ending type
                int crlfCount = RegularExpressions.CrLf.Split(Contents).Length;
                string fileContentsNoCRLF = RegularExpressions.CrLf.Replace(Contents, "");
                int crCount = RegularExpressions.Cr.Split(fileContentsNoCRLF).Length;
                int lfCount = RegularExpressions.CrLf.Split(fileContentsNoCRLF).Length;
                if (crlfCount > crCount && crlfCount > lfCount)
                    LineEndings = "\r\n";
                else if (crCount > crlfCount && crCount > lfCount)
                    LineEndings = "\r";
                else if (lfCount > crlfCount && lfCount > crCount)
                    LineEndings = "\n";
                else //??
                    LineEndings = Environment.NewLine;
                fileContentsNoCRLF = null;

                //normalize line endings
                Contents = Contents.NormalizeLineEndings();

                //add initial operation
                MasterOperations.Add(new Operation(Guid.Empty, 0, Contents));
            }
            return true;
        }

        /////////////////////////////////////////////////////////////////////
        // SERVER-SIDE FILE FLUSHING
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Applies the output of all new operations to the internal file contents string, then
        /// writes the new contents to disk.
        /// </summary>
        internal void FlushDocument()
        {
            if (Temporary)
                return;

            //flush pending operations to the file contents
            while (SyncIndex < MasterOperations.Count)
            {
                if (!MasterOperations[SyncIndex].IsNoop)
                    Contents = MasterOperations[SyncIndex].Execute(Contents);
                ++SyncIndex;
            }

            //check line ending normalization
            var fileOutput = Contents;
            if (!LineEndings.Equals(Environment.NewLine))
                fileOutput = Contents.Replace(Environment.NewLine, LineEndings);

            //write contents to disk
            File.WriteAllText(Path, fileOutput);
        }
    }
}
