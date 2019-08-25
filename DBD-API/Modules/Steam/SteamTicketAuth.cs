using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SteamKit2;
using SteamKit2.Internal;

namespace DBD_API.Modules.Steam
{
    public class SteamTicketAuth : ClientMsgHandler
    {
        public override void HandleMsg(IPacketMsg packetMsg)
        {
            if (packetMsg.MsgType != EMsg.ClientAuthListAck) return;

            var authAck = new ClientMsgProtobuf<CMsgClientAuthListAck>(packetMsg);
            var acknowledged = new SteamTicketAccepted(authAck.TargetJobID, authAck.Body);
            Client.PostCallback(acknowledged);
        }
    }
}
