using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OTEX
{
    /// <summary>
    /// Nodes of an OTEX framework network.
    /// </summary>
    public interface INode
    {
        /// <summary>
        /// ID for this node.
        /// </summary>
        Guid ID { get; }

        /// <summary>
        /// AppKey for this node. Will only be compatible with other nodes sharing a matching AppKey.
        /// </summary>
        AppKey AppKey { get; }
    }
}
