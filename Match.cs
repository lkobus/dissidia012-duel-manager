using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TorneioBot
{
    public class Match
    {
        public ulong Player1DiscordId {get; set;}
        public ulong Player2DiscordId {get; set;}
        public string Player1Pick {get; set;}
        public string Player2Pick {get; set;}
        public string Player1Result {get; set;}
        public string Player2Result {get; set;}
        public string Stage { get; set; }        
        public string BannedStage {get; set;}
        public string PickStage {get ; set;}
        public string Player1Action {get; set;}        
        public string Player2Action {get; set;}
    }
}