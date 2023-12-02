using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TorneioBot
{
    public class MatchOptions
    {
        public string Type {get; set;}
        public int Round {get; set;}
        
        public MatchOptions(string type, int round)
        {
            Type = type;
            Round = round;
        }
        
    }
}