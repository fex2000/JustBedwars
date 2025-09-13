
using JustBedwars.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.ComponentModel;

namespace JustBedwars.Models
{
    public class Player : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private string? _username;
        public string? Username
        {
            get => _username;
            set
            {
                if (_username != value)
                {
                    _username = value;
                    OnPropertyChanged(nameof(Username));
                }
            }
        }

        private int _star;
        public int Star
        {
            get => _star;
            set
            {
                if (_star != value)
                {
                    _star = value;
                    OnPropertyChanged(nameof(Star));
                }
            }
        }

        private long _firstlogin;
        public long FirstLogin
        {
            get => _firstlogin;
            set
            {
                if (_firstlogin != value)
                {
                    _firstlogin = value;
                    OnPropertyChanged(nameof(FirstLogin));
                    OnPropertyChanged(nameof(FirstLoginDate));
                }
            }
        }

        public string FirstLoginDate
        {
            get
            {
                if (_firstlogin == 0)
                {
                    return string.Empty;
                }
                return DateTimeOffset.FromUnixTimeMilliseconds(_firstlogin).ToString("dd.MM.yyyy");
            }
        }

        private double _fkdr;
        public double FKDR
        {
            get => _fkdr;
            set
            {
                if (_fkdr != value)
                {
                    _fkdr = value;
                    OnPropertyChanged(nameof(FKDR));
                }
            }
        }

        private double _bblr;
        public double BBLR
        {
            get => _bblr;
            set
            {
                if (_bblr != value)
                {
                    _bblr = value;
                    OnPropertyChanged(nameof(BBLR));
                }
            }
        }

        private double _wlr;
        public double WLR
        {
            get => _wlr;
            set
            {
                if (_wlr != value)
                {
                    _wlr = value;
                    OnPropertyChanged(nameof(WLR));
                }
            }
        }

        private double _kdr;
        public double KDR
        {
            get => _kdr;
            set
            {
                if (_kdr != value)
                {
                    _kdr = value;
                    OnPropertyChanged(nameof(WLR));
                }
            }
        }

        private int _finals;
        public int Finals
        {
            get => _finals;
            set
            {
                if (_finals != value)
                {
                    _finals = value;
                    OnPropertyChanged(nameof(Finals));
                }
            }
        }

        private int _finaldeaths;
        public int FinalDeaths
        {
            get => _finaldeaths;
            set
            {
                if (_finaldeaths != value)
                {
                    _finaldeaths = value;
                    OnPropertyChanged(nameof(FinalDeaths));
                }
            }
        }

        private int _kills;
        public int Kills
        {
            get => _kills;
            set
            {
                if (_kills != value)
                {
                    _kills = value;
                    OnPropertyChanged(nameof(Kills));
                }
            }
        }

        private int _deaths;
        public int Deaths
        {
            get => _deaths;
            set
            {
                if (_deaths != value)
                {
                    _deaths = value;
                    OnPropertyChanged(nameof(Deaths));
                }
            }
        }

        private int _wins;
        public int Wins
        {
            get => _wins;
            set
            {
                if (_wins != value)
                {
                    _wins = value;
                    OnPropertyChanged(nameof(Wins));
                }
            }
        }

        private int _losses;
        public int Losses
        {
            get => _losses;
            set
            {
                if (_losses != value)
                {
                    _losses = value;
                    OnPropertyChanged(nameof(Losses));
                }
            }
        }

        private int _beds;
        public int Beds
        {
            get => _beds;
            set
            {
                if (_beds != value)
                {
                    _beds = value;
                    OnPropertyChanged(nameof(Beds));
                }
            }
        }

        private int _bedsLost;
        public int BedsLost
        {
            get => _bedsLost;
            set
            {
                if (_bedsLost != value)
                {
                    _bedsLost = value;
                    OnPropertyChanged(nameof(BedsLost));
                }
            }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    OnPropertyChanged(nameof(IsLoading));
                }
            }
        }

        private string? _playerTag;
        public string? PlayerTag
        {
            get => _playerTag;
            set
            {
                if (_playerTag != value)
                {
                    _playerTag = value;
                    OnPropertyChanged(nameof(PlayerTag));
                }
            }
        }

        private string? _playeruuid;
        public string? PlayerUUID
        {
            get => _playeruuid;
            set
            {
                if (_playeruuid != value)
                {
                    _playeruuid = value;
                    OnPropertyChanged(nameof(PlayerUUID));
                }
            }
        }

        private long _bedwarsExperience;
        public long BedwarsExperience
        {
            get => _bedwarsExperience;
            set
            {
                if (_bedwarsExperience != value)
                {
                    _bedwarsExperience = value;
                    OnPropertyChanged(nameof(BedwarsExperience));
                    OnPropertyChanged(nameof(BedwarsLevelProgress));
                }
            }
        }

        private long _networkExp;
        public long NetworkExp
        {
            get => _networkExp;
            set
            {
                if (_networkExp != value)
                {
                    _networkExp = value;
                    OnPropertyChanged(nameof(NetworkExp));
                    OnPropertyChanged(nameof(HypixelLevelProgress));
                    OnPropertyChanged(nameof(HypixelLevel));
                }
            }
        }

        public double BedwarsLevelProgress
        {
            get
            {
                if (BedwarsExperience == 0) return 0;
                return GetBedWarsLevelPercentage(BedwarsExperience) * 100;
            }
        }

        public double HypixelLevelProgress
        {
            get
            {
                if (NetworkExp == 0) return 0;
                double progress = (Math.Sqrt((2 * NetworkExp) + 30625) / 50) - 2.5;
                return (progress - Math.Truncate(progress)) * 100;
            }
        }

        public int HypixelLevel
        {
            get
            {
                if (NetworkExp == 0) return 0;
                double progress = (Math.Sqrt((2 * NetworkExp) + 30625) / 50) - 2.5;
                return (int)Math.Truncate(progress);
            }
        }

        private static double GetBedWarsLevelPercentage(double exp)
        {
            int level = 100 * (int)(exp / 487000);
            exp = exp % 487000;
            if (exp < 500) return level + exp / 500;
            if (exp < 1500) return level + (exp - 500) / 1000;
            if (exp < 3500) return level + (exp - 1500) / 2000;
            if (exp < 7000) return level + (exp - 3500) / 3500;
            exp -= 7000;
            return ((level + exp / 5000) - Math.Truncate(level + exp / 5000));
        }

        public Visibility IsLoaderVisible
        {
            get
            {
                if (_isLoading == false)
                    return Visibility.Collapsed;
                else
                    return Visibility.Visible;
            }
        }

        public Visibility IsContentVisible
        {
            get
            {
                if (_isLoading == false)
                    return Visibility.Visible;
                else
                    return Visibility.Collapsed;
            }
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private bool _isExpanded;
        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    OnPropertyChanged(nameof(IsExpanded));
                    OnPropertyChanged(nameof(IsCollapsedContentVisible));
                    OnPropertyChanged(nameof(IsExpandedContentVisible));
                }
            }
        }

        public Visibility IsCollapsedContentVisible
        {
            get
            {
                if (_isExpanded == true)
                    return Visibility.Collapsed;
                else
                    return Visibility.Visible;
            }
        }

        public Visibility IsExpandedContentVisible
        {
            get
            {
                if (_isExpanded == true)
                    return Visibility.Visible;
                else
                    return Visibility.Collapsed;
            }
        }

        public BitmapImage PlayerImageFace
        {
            get
            {
                return new BitmapImage(new Uri($"https://starlightskins.lunareclipse.studio/render/default/{_username}/face"));
            }
        }

        public double Score
        {
            get {
                return Star * Math.Pow(FKDR, 2) * Math.Pow(WLR, 1.2) * Math.Pow(BBLR, 1.1) * (1 + Finals / 1000.0 + Kills / 2000.0 + Beds / 500.0 + Wins / 1000.0);
            }
        }
    }
}
