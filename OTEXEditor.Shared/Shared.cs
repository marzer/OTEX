using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OTEX.Editor
{
    /// <summary>
    /// Static constants for OTEX Editor applications.
    /// </summary>
    public static class Shared
    {
        /// <summary>
        /// App Key for OTEX Editor apps.
        /// </summary>
        public static readonly AppKey AppKey
            = new AppKey(Guid.Parse("B98107CD-D469-4517-B60C-6AB853033C48"), 0);
    }
}
