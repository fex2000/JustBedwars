using System;
using Microsoft.UI.Xaml.Media.Imaging;

namespace JustBedwars.Models
{
    public class LeaderboardEntry
    {
        public int Rank { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }
        public string Uuid { get; set; }

        public Uri PlayerImageIconUri
        {
            get
            {
                return new Uri($"https://skins.jbw.fexei.at/face/{Name}");
            }
        }
    }
}