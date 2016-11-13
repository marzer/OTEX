using System.Collections.Generic;

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
