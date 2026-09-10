using System;
using System.ComponentModel;
#if WINDOWS
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;
#endif

namespace JustBedwars.Models;

public class Player : INotifyPropertyChanged
{
    private double _bblr;

    private int _beds;

    private int _bedsLost;

    private long _bedwarsExperience;

    private int _deaths;

    private int _finaldeaths;

    private int _finals;

    private long _firstlogin;

    private double _fkdr;

    private bool _isExpanded;

    private bool _isLoading;

    private double _kdr;

    private int _kills;

    private int _losses;

    private long _networkExp;

    private string? _playerTag;

    private string? _playeruuid;

    private int _star;

    private string? _username;

    private int _wins;

    private double _wlr;

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
            if (_firstlogin == 0) return string.Empty;
            return DateTimeOffset.FromUnixTimeMilliseconds(_firstlogin).ToString("dd.MM.yyyy");
        }
    }

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

    public double KDR
    {
        get => _kdr;
        set
        {
            if (_kdr != value)
            {
                _kdr = value;
                OnPropertyChanged(nameof(KDR));
            }
        }
    }

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

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            if (_isLoading != value)
            {
                _isLoading = value;
                OnPropertyChanged(nameof(IsLoading));
#if WINDOWS
                OnPropertyChanged(nameof(IsLoaderVisible));
                OnPropertyChanged(nameof(IsContentVisible));
                OnPropertyChanged(nameof(TagCardVisibility));
#endif
            }
        }
    }

    public string? PlayerTag
    {
        get => _playerTag;
        set
        {
            if (_playerTag != value)
            {
                _playerTag = value;
                OnPropertyChanged(nameof(PlayerTag));
#if WINDOWS
                OnPropertyChanged(nameof(TagCardVisibility));
                OnPropertyChanged(nameof(UnknownStatsVisibility));
#endif
            }
        }
    }

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
            var progress = Math.Sqrt(2 * NetworkExp + 30625) / 50 - 2.5;
            return (progress - Math.Truncate(progress)) * 100;
        }
    }

    public int HypixelLevel
    {
        get
        {
            if (NetworkExp == 0) return 0;
            var progress = Math.Sqrt(2 * NetworkExp + 30625) / 50 - 2.5;
            return (int)Math.Truncate(progress);
        }
    }

#if WINDOWS
    public Visibility IsLoaderVisible
    {
        get
        {
            if (!_isLoading)
                return Visibility.Collapsed;
            return Visibility.Visible;
        }
    }

    public Visibility IsContentVisible
    {
        get
        {
            if (!_isLoading)
                return Visibility.Visible;
            return Visibility.Collapsed;
        }
    }
#endif

    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (_isExpanded != value)
            {
                _isExpanded = value;
                OnPropertyChanged(nameof(IsExpanded));
#if WINDOWS
                OnPropertyChanged(nameof(IsCollapsedContentVisible));
                OnPropertyChanged(nameof(IsExpandedContentVisible));
#endif
            }
        }
    }

    public double StatOpacity
    {
        get
        {
            if (PlayerTag == "NICK" || PlayerTag == "ERROR")
                return 0.2;
            return 1;
        }
    }

#if WINDOWS
    public Visibility TagCardVisibility
    {
        get
        {
            if (IsContentVisible != Visibility.Visible || PlayerTag == "" || PlayerTag == "-")
                return Visibility.Collapsed;
            return Visibility.Visible;
        }
    }

    public Visibility UnknownStatsVisibility
    {
        get
        {
            if (PlayerTag == "NICK" || PlayerTag == "ERROR")
                return Visibility.Visible;
            return Visibility.Collapsed;
        }
    }

    public Visibility IsCollapsedContentVisible
    {
        get
        {
            if (_isExpanded)
                return Visibility.Collapsed;
            return Visibility.Visible;
        }
    }

    public Visibility IsExpandedContentVisible
    {
        get
        {
            if (_isExpanded)
                return Visibility.Visible;
            return Visibility.Collapsed;
        }
    }

    public BitmapImage PlayerImageFace => new(new Uri($"https://skins.jbw.fexei.at/bust/{_username}"));

    public BitmapImage PlayerImageIcon => new(new Uri($"https://skins.jbw.fexei.at/face/{_username}"));
#endif

    public double Score => Star * Math.Pow(FKDR, 2) * Math.Pow(WLR, 1.2) * Math.Pow(BBLR, 1.1) *
                           (1 + Finals / 1000.0 + Kills / 2000.0 + Beds / 500.0 + Wins / 1000.0);

    public event PropertyChangedEventHandler? PropertyChanged;

    private static double GetBedWarsLevelPercentage(double exp)
    {
        var level = 100 * (int)(exp / 487000);
        exp = exp % 487000;
        if (exp < 500) return level + exp / 500;
        if (exp < 1500) return level + (exp - 500) / 1000;
        if (exp < 3500) return level + (exp - 1500) / 2000;
        if (exp < 7000) return level + (exp - 3500) / 3500;
        exp -= 7000;
        return level + exp / 5000 - Math.Truncate(level + exp / 5000);
    }

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}