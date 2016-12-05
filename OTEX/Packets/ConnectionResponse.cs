using System;
using System.Collections.Generic;

namespace OTEX.Packets
{
    /// <summary>
    /// Connection response packet sent from an OTEX server to client.
    /// </summary>
    [Serializable]
    internal sealed class ConnectionResponse : IPacketPayload
    {
        /////////////////////////////////////////////////////////////////////
        // PROPERTIES/VARIABLES
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Payload type ID for this class.
        /// </summary>
        public const uint PayloadType = 3;

        /// <summary>
        /// Implements IPacketPayload.PacketPayloadType (returns PayloadType).
        /// </summary>
        public uint PacketPayloadType
        {
            get { return PayloadType; }
        }

        /// <summary>
        /// Possible results of a connection request.
        /// </summary>
        public enum ResponseCode : uint
        {
            /// <summary>
            /// Connection request was approved and the client
            /// is now 'connected' according to the Server.
            /// </summary>
            Approved,

            /// <summary>
            /// The password provided by client was not correct
            /// (also fires when clients provide passwords when none is required)
            /// </summary>
            IncorrectPassword,

            /// <summary>
            /// Server has already reached the maximum number of connected clients
            /// </summary>
            SessionFull,

            /// <summary>
            /// A client with the same GUID is already connected to the server
            /// (shouldn't happen, really; fluke!)
            /// </summary>
            DuplicateGUID,

            /// <summary>
            /// Client fell into an invalid state(sent non-request packet as first packet)
            /// </summary>
            InvalidState,

            /// <summary>
            /// Client has been banned from this server
            /// </summary>
            Banned,

            /// <summary>
            /// Server had a different AppKey
            /// </summary>
            DifferentAppKey,

            /// <summary>
            /// Other (not used by anything currently)
            /// </summary>
            Other
        }

        /// <summary>
        /// What was the result of the request?
        /// </summary>
        public ResponseCode Result { get; private set; }

        /// <summary>
        /// The server's session state.
        /// </summary>
        public Session Session { get; private set; }

        /////////////////////////////////////////////////////////////////////
        // CONSTRUCTORS
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an "approved" connection request response.
        /// </summary>
        /// <param name="session">The server's current session (the one the client has just successfully joined).</param>
        public ConnectionResponse(Session session)
        {
            Session = session ?? throw new ArgumentNullException("session");
            Result = ResponseCode.Approved;
        }

        /// <summary>
        /// Constructs a "rejected" connection request response.
        /// </summary>
        /// <param name="failReason">Reason the connection was rejected.</param>
        /// <exception cref="ArgumentOutOfRangeException" />
        public ConnectionResponse(ResponseCode failReason = ResponseCode.Other)
        {
            if (failReason == ResponseCode.Approved || failReason > ResponseCode.Other)
                throw new ArgumentOutOfRangeException("failReason", "failReason must be one of the negative ResponseCode values.");
            Result = failReason;
            Session = null;
        }

        /////////////////////////////////////////////////////////////////////
        // DESCRIBING ERROR CODES
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Describes a response code in plain-English (for error messages, exceptions, etc.).
        /// </summary>
        /// <param name="code">The code to describe</param>
        /// <returns>A plain-English description of a server's response.</returns>
        public static string Describe(ResponseCode code)
        {
            switch (code)
            {
                case ResponseCode.Approved:
                    return "The connection was approved by the server.";

                case ResponseCode.DuplicateGUID:
                    return "A client with the same ID is already connected to the server.";

                case ResponseCode.IncorrectPassword:
                    return "The password provided by the client was incorrect.";

                case ResponseCode.InvalidState:
                    return "The server detected the client being in an invalid state.";

                case ResponseCode.SessionFull:
                    return "The server's session is full.";

                case ResponseCode.Banned:
                    return "The client has been banned from the server.";

                case ResponseCode.DifferentAppKey:
                    return "The server's application key did not match the client's.";

                default:
                    return "The server rejected the connection.";
            }
        }
    }
}
