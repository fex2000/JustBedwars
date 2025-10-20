using System;
using System.Collections.Generic;
using System.IO;

namespace JustBedwars.Services
{
    public class DebugService
    {
        private static readonly DebugService _instance = new DebugService();
        public static DebugService Instance => _instance;

        private readonly List<string> _logHistory = new List<string>();
        private bool _isSavingToFile = false;
        private string _logFilePath;

        public event Action<string> LogAdded = delegate { };
        public event Action<string> EmulatePlayerJoined = delegate { };
        public event Action<string> EmulatePlayerLeft = delegate { };

        public void Log(string message)
        {
            string logEntry = $"[{DateTime.Now:HH:mm:ss}] {message}";
            _logHistory.Add(logEntry);
            LogAdded?.Invoke(logEntry);

            if (_isSavingToFile)
            {
                File.AppendAllText(_logFilePath, logEntry + Environment.NewLine);
            }
        }

        public List<string> GetLogHistory()
        {
            return new List<string>(_logHistory);
        }

        public void SetFileLogging(bool enable, string filePath = null)
        {
            _isSavingToFile = enable;
            if (enable && !string.IsNullOrEmpty(filePath))
            {
                _logFilePath = filePath;
                File.WriteAllLines(_logFilePath, _logHistory);
            }
        }

        public void OnEmulatePlayerJoined(string username)
        {
            EmulatePlayerJoined?.Invoke(username);
        }

        public void OnEmulatePlayerLeft(string username)
        {
            EmulatePlayerLeft?.Invoke(username);
        }
    }
}
