using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TorneioBot
{
    public class Config
    {
        public string Language { get; set; }
        public ulong GuildId {get; set; }
        public ulong ClientId {get; set;}
        public string Token {get; set; }
        public string OverlayFullPath {get; set; }
        public ulong Player1RoleId { get; set; }
        public ulong Player2RoleId { get; set; }
        public ulong Player1ChannelId { get; set; }
        public ulong Player2ChannelId { get; set; }        
    }
}