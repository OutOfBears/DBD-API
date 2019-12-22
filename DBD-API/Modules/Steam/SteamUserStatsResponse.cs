using SteamKit2;
using SteamKit2.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBD_API.Modules.Steam
{
    /// <summary>
    /// This callback is fired when steam retrieves the users stats.
    /// </summary>
    public class SteamUserStatsResponse : CallbackMsg
    {
        public GameID GameId { get; private set; }

        public bool GameIdSpecified { get; private set; }

        public EResult Result { get; private set; }

        public bool ResultSpecified { get; private set; }

        public uint CrcStats { get; private set; }

        public bool CrcStatsSpecified { get; private set; }

        public byte[] Schema { get; private set; }

        public bool SchemaSpecified { get; private set; }

        public List<CMsgClientGetUserStatsResponse.Stats> Stats { get; private set; }

        public List<CMsgClientGetUserStatsResponse.Achievement_Blocks> AchievementBlocks { get; private set; }

        public KeyValue ParsedSchema;

        internal SteamUserStatsResponse(JobID jobId, CMsgClientGetUserStatsResponse body)
        {
            JobID = jobId;
            GameId = body.game_id;
            GameIdSpecified = body.game_idSpecified;
            Result = (EResult)body.eresult;
            ResultSpecified = body.eresultSpecified;
            CrcStats = body.crc_stats;
            CrcStatsSpecified = body.crc_statsSpecified;
            Schema = body.schema;
            SchemaSpecified = body.schemaSpecified;
            Stats = body.stats;
            AchievementBlocks = body.achievement_blocks;

            ParsedSchema = new KeyValue();
            if (body.schemaSpecified && body.schema != null)
            {
                using (MemoryStream ms = new MemoryStream(body.schema))
                using (var br = new BinaryReader(ms))
                {
                    ParsedSchema.TryReadAsBinary(ms);
                }
            }
        }
    }

    public class SteamStatsHandler : ClientMsgHandler
    {
        public override void HandleMsg(IPacketMsg packetMsg)
        {
            if (packetMsg.MsgType != EMsg.ClientGetUserStatsResponse)
                return;

            var statsResp = new ClientMsgProtobuf<CMsgClientGetUserStatsResponse>(packetMsg);
            var acknowledged = new SteamUserStatsResponse(statsResp.TargetJobID, statsResp.Body);
            Client.PostCallback(acknowledged);
        }
    }
}
