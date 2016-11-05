using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatClassLibrary.Protocols
{
    public static class ProtocolSettings
    {
        /// <summary>
        /// Port that server uses to listen for new client connection.
        /// </summary>
        public static int ChatProtocolPort => 60000;

        /// <summary>
        /// Buffer size used in all socket stream reading operations.
        /// </summary>
        public static int ChatProtocolBufferSize => 8192;

        /// <summary>
        /// Port that server uses to listen for file upload requests.
        /// </summary>
        public static int FileProtocolPort => 60001;

        /// <summary>
        /// Buffer size used in file sending/receiving operations.
        /// </summary>
        public static int FileProtocolBufferSize => 8192;

        /// <summary>
        /// Text encoding for all network operations.
        /// </summary>
        public static Encoding TextEncoding => Encoding.UTF8;

        /// <summary>
        /// Use as SenderID or TargetID when there's no need to be specific.
        /// </summary>
        public static Guid NullId => Guid.Empty;
    }
}
