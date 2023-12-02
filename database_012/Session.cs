using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TorneioBot.database_012
{
    public class Session
    {
        public int MatchId { get; set; }
        public DateTime Date { get; set; }
        public ulong DiscordId { get; set; }
        public bool Joined { get; set; }
    }

}