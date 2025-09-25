
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Windows.UI;

namespace JustBedwars.Helpers
{
    public static class ColorHelper
    {
        // Minecraft Colors
        public static readonly Color AQUA = Color.FromArgb(255, 85, 255, 255);
        public static readonly Color BLACK = Color.FromArgb(255, 0, 0, 0);
        public static readonly Color BLUE = Color.FromArgb(255, 85, 85, 255);
        public static readonly Color DARK_AQUA = Color.FromArgb(255, 0, 170, 170);
        public static readonly Color DARK_BLUE = Color.FromArgb(255, 0, 0, 170);
        public static readonly Color DARK_GRAY = Color.FromArgb(255, 85, 85, 85);
        public static readonly Color DARK_GREEN = Color.FromArgb(255, 0, 170, 0);
        public static readonly Color DARK_PURPLE = Color.FromArgb(255, 170, 0, 170);
        public static readonly Color DARK_RED = Color.FromArgb(255, 170, 0, 0);
        public static readonly Color GOLD = Color.FromArgb(255, 255, 170, 0);
        public static readonly Color GRAY = Color.FromArgb(255, 170, 170, 170);
        public static readonly Color GREEN = Color.FromArgb(255, 85, 255, 85);
        public static readonly Color LIGHT_PURPLE = Color.FromArgb(255, 255, 85, 255);
        public static readonly Color RED = Color.FromArgb(255, 255, 85, 85);
        public static readonly Color WHITE = Color.FromArgb(255, 255, 255, 255);
        public static readonly Color YELLOW = Color.FromArgb(255, 255, 255, 85);

        public static Color GetStatColor(double value, StatType type)
        {
            var theme = Application.Current.RequestedTheme;
            var level = GetStatLevel(value, type);

            return level switch // Dark/Light mode switches
            {
                StatLevel.Bad => theme == ApplicationTheme.Dark ? GRAY : DARK_GRAY,
                StatLevel.OK => theme == ApplicationTheme.Dark ? WHITE : BLACK,
                StatLevel.Decent => theme == ApplicationTheme.Dark ? YELLOW : GOLD,
                StatLevel.Good => theme == ApplicationTheme.Dark ? AQUA : DARK_AQUA,
                StatLevel.Legend => theme == ApplicationTheme.Dark ? RED : DARK_RED,
                StatLevel.God => DARK_PURPLE,
                _ => theme == ApplicationTheme.Dark ? WHITE : BLACK,
            };
        }

        public static Color GetUsernameColor(int score)
        {
            if (score < 20) return GetStatColor(0, StatType.FKDR);
            if (score < 250) return GetStatColor(3, StatType.FKDR);
            if (score < 10000) return GetStatColor(5, StatType.FKDR);
            if (score < 100000) return GetStatColor(10, StatType.FKDR);
            if (score < 2500000) return GetStatColor(25, StatType.FKDR);
            return GetStatColor(30.1, StatType.FKDR);
        }

        private static StatLevel GetStatLevel(double value, StatType type)
        {
            return type switch
            {
                StatType.FKDR => value switch
                {
                    < 1 => StatLevel.Bad,
                    < 3 => StatLevel.OK,
                    < 5 => StatLevel.Decent,
                    < 10 => StatLevel.Good,
                    < 25 => StatLevel.Legend,
                    _ => StatLevel.God,
                },
                StatType.WLR => value switch
                {
                    < 1 => StatLevel.Bad,
                    < 2 => StatLevel.OK,
                    < 5 => StatLevel.Decent,
                    < 7 => StatLevel.Good,
                    < 10 => StatLevel.Legend,
                    _ => StatLevel.God,
                },
                StatType.Finals => value switch
                {
                    < 1000 => StatLevel.Bad,
                    < 5000 => StatLevel.OK,
                    < 15000 => StatLevel.Decent,
                    < 30000 => StatLevel.Good,
                    < 75000 => StatLevel.Legend,
                    _ => StatLevel.God,
                },
                StatType.Wins => value switch
                {
                    < 500 => StatLevel.Bad,
                    < 1000 => StatLevel.OK,
                    < 3000 => StatLevel.Decent,
                    < 5000 => StatLevel.Good,
                    < 10000 => StatLevel.Legend,
                    _ => StatLevel.God,
                },
                StatType.BBLR => value switch
                {
                    < 1 => StatLevel.Bad,
                    < 2 => StatLevel.OK,
                    < 3 => StatLevel.Decent,
                    < 5 => StatLevel.Good,
                    < 7.5 => StatLevel.Legend,
                    _ => StatLevel.God,
                },
                StatType.KDR => value switch
                {
                    < 1 => StatLevel.Bad,
                    < 3 => StatLevel.OK,
                    < 5 => StatLevel.Decent,
                    < 10 => StatLevel.Good,
                    < 25 => StatLevel.Legend,
                    _ => StatLevel.God,
                },
                StatType.Stars => value switch
                {
                    < 100 => StatLevel.Bad,
                    < 200 => StatLevel.OK,
                    < 300 => StatLevel.Decent,
                    < 475 => StatLevel.Good,
                    < 700 => StatLevel.Legend,
                    _ => StatLevel.God,
                },
                _ => StatLevel.OK,
            };
        }
    }

    public enum StatType
    {
        FKDR,
        WLR,
        Finals,
        Wins,
        BBLR,
        KDR,
        Stars
    }

    public enum StatLevel
    {
        Bad,
        OK,
        Decent,
        Good,
        Legend,
        God
    }
}
