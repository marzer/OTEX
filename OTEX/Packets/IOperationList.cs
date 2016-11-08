using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OTEX.Packets
{
    /// <summary>
    /// Interface for packets that contain a list of operations.
    /// </summary>
    internal interface IOperationList
    {
        List<Operation> Operations { get; }
    }
}
