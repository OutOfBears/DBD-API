using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SteamKit2;
using SteamKit2.Internal;

namespace DBD_API.Modules.Steam
{
    /// <summary>
    /// This callback is fired when Steam accepts our auth ticket.
    /// </summary>
    public class SteamTicketAccepted : CallbackMsg
    {
        /// <summary>
        /// <see cref="List{T}"/> of AppIDs of the games that have generated tickets.
        /// </summary>
        public List<uint> AppIDs { get; private set; }
        /// <summary>
        /// <see cref="List{T}"/> of CRC32 hashes of activated tickets.
        /// </summary>
        public List<uint> ActiveTicketsCRC { get; private set; }
        /// <summary>
        /// Number of message in sequence.
        /// </summary>
        public uint MessageSequence { get; private set; }

        internal SteamTicketAccepted(JobID jobId, CMsgClientAuthListAck body)
        {
            JobID = jobId;
            AppIDs = body.app_ids;
            ActiveTicketsCRC = body.ticket_crc;
            MessageSequence = body.message_sequence;
        }
    }

    public class SteamTicketAuth : ClientMsgHandler
    {
        public override void HandleMsg(IPacketMsg packetMsg)
        {
            if (packetMsg.MsgType != EMsg.ClientAuthListAck)
                return;
            
            var authAck = new ClientMsgProtobuf<CMsgClientAuthListAck>(packetMsg);
            var acknowledged = new SteamTicketAccepted(authAck.TargetJobID, authAck.Body);
            Client.PostCallback(acknowledged);
        }
    }
}
