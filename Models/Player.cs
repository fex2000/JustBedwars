
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

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
