
using Microsoft.UI.Xaml;
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
    }
}
