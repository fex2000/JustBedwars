using System.Collections.Generic;

namespace JustBedwars.Models;

public class Guild
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<GuildMember> Members { get; set; } = new();
    public List<GuildRank> Ranks { get; set; } = new();
    public string Tag { get; set; } = string.Empty;
    public long Exp { get; set; }
    public long Created { get; set; }
    public string Description { get; set; } = string.Empty;
    public List<string> PreferredGames { get; set; } = new();
    public double Level { get; set; }
    public int OnlinePlayers { get; set; }
    public Dictionary<string, long> ExpByGameType { get; set; } = new();
}

public class GuildMember
{
    public string Uuid { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Rank { get; set; } = string.Empty;
    public long Joined { get; set; }
}

public class GuildRank
{
    public string Name { get; set; } = string.Empty;
    public int Priority { get; set; }
    public string Tag { get; set; } = string.Empty;
}