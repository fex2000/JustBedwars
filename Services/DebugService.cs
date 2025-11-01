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
        private readonly object _logHistoryLock = new object();
        private readonly object _fileLock = new object();
        private bool _isSavingToFile = false;
        private string _logFilePath;

        public event Action<string> LogAdded = delegate { };
        public event Action<string> EmulatePlayerJoined = delegate { };
        public event Action<string> EmulatePlayerLeft = delegate { };

        public void Log(string message)
        {
            string logEntry = $"[{DateTime.Now:HH:mm:ss}] {message}";

            lock (_logHistoryLock)
            {
                _logHistory.Add(logEntry);
            }

            LogAdded?.Invoke(logEntry);

            if (_isSavingToFile && !string.IsNullOrEmpty(_logFilePath))
            {
                try
                {
                    lock (_fileLock)
                    {
                        using (StreamWriter writer = new StreamWriter(_logFilePath, true))
                        {
                            writer.WriteLine(logEntry);
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Swallow exception to prevent task from crashing
                    Console.WriteLine($"[DebugService] Failed to write to log file: {ex.Message}");
                }
            }
        }

        public List<string> GetLogHistory()
        {
            lock (_logHistoryLock)
            {
                return new List<string>(_logHistory);
            }
        }

        public void SetFileLogging(bool enable, string filePath, bool enableLogHistory)
        {
            _isSavingToFile = enable;
            if (enable && !string.IsNullOrEmpty(filePath))
            {
                _logFilePath = filePath;
                try
                {
                    var logDirectory = Path.GetDirectoryName(filePath);
                    Directory.CreateDirectory(logDirectory);

                    if (enableLogHistory && File.Exists(filePath))
                    {
                        var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                        var historyFilePath = Path.Combine(logDirectory, $"{timestamp}.log.tz");
                        File.Move(filePath, historyFilePath);
                    }

                    // Delete old log file
                    var oldLogFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "JustBedwars", "debug.log");
                    if(File.Exists(oldLogFile))
                        File.Delete(oldLogFile);

                    lock (_fileLock)
                    {
                        lock (_logHistoryLock)
                        {
                            File.WriteAllLines(_logFilePath, _logHistory);
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Swallow exception
                    Console.WriteLine($"[DebugService] Failed to initialize log file: {ex.Message}");
                }
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
