using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
