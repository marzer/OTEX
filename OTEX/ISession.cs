using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;

namespace OTEX
{
    /// <summary>
    /// Client-side interface to an OTEX session.
    /// </summary>
    public interface ISession
    {
        Guid ID { get; }
        ReadOnlyDictionary<Guid, Document> Documents { get; }
        uint ClientCount { get; }
        uint ClientLimit { get; }
        string Name { get; }
        ushort Port { get; }
        IPAddress Address { get; }
        bool Public { get; }
    }
}