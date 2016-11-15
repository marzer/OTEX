using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OTEX.Packets
{
    /// <summary>
    /// Interface for all packet types representing a general client state update from the server.
    /// </summary>
    internal interface IClientUpdate
    {
        Dictionary<Guid, byte[]> Metadata { get; }

        List<Operation> Operations { get; }
    }
}
