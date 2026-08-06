using System;

namespace JustBedwars.Models
{
    public class LeaderboardEntry
    {
        public int Rank { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Uuid { get; set; } = string.Empty;

        public Uri PlayerImageIconUri
        {
            get
            {
                return new Uri($"https://skins.jbw.fexei.at/face/{Uuid}");
            }
        }
    }
}