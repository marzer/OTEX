using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OTEX.Packets
{
    /// <summary>
    /// Interface to help with the serialization of objects into node packet sequences.
    /// </summary>
    internal interface IPacketPayload
    {
        /// <summary>
        /// The unique PacketType for this object type.
        /// </summary>
        uint PacketPayloadType { get; }
    }
}
