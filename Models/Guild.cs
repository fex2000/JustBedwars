
using System;
using System.Collections.Generic;

namespace JustBedwars.Models
{
    public class Guild
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public List<GuildMember> Members { get; set; }
        public List<GuildRank> Ranks { get; set; }
        public string Tag { get; set; }
        public long Exp { get; set; }
        public long Created { get; set; }
        public string Description { get; set; }
        public List<string> PreferredGames { get; set; }
        public double Level { get; set; }
        public int OnlinePlayers { get; set; }
        public Dictionary<string, long> ExpByGameType { get; set; }
    }

    public class GuildMember
    {
        public string Uuid { get; set; }
        public string Name { get; set; }
        public string Rank { get; set; }
        public long Joined { get; set; }
    }

    public class GuildRank
    {
        public string Name { get; set; }
        public int Priority { get; set; }
        public string Tag { get; set; }
    }
}
