using System;
using System.Collections.Generic;

namespace OTEX.Packets
{
    /// <summary>
    /// Interface for packets that contain a set of client metadata.
    /// </summary>
    internal interface IClientMetadata
    {
        Dictionary<Guid, byte[]> Metadata { get; }
    }
}
