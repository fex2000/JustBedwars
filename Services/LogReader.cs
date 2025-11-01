using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using JustBedwars.Services;

namespace JustBedwars.Services
{
    public class LogReader
    {
        public event Action<string> PlayerJoined = delegate { };
        public event Action<string> PlayerLeft = delegate { };
        public event Action<List<string>> WhoResult = delegate { };
        public event Action ClearList = delegate { };

        private readonly string _logFilePath;
        private long _lastPosition;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly SettingsService _settingsService;
        private bool _enableLogging;
        private const string EnableLogReaderLoggingSettingName = "EnableLogReaderLogging";

        public LogReader(string? logFilePath = null)
        {
            _logFilePath = logFilePath ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraft", "logs", "latest.log");
            if (File.Exists(_logFilePath))
            {
                _lastPosition = new FileInfo(_logFilePath).Length;
            }
            else
            {
                _lastPosition = 0;
            }
            _cancellationTokenSource = new CancellationTokenSource();
            _settingsService = new SettingsService();
            LoadLoggingSetting();
            _settingsService.SettingChanged += OnSettingChanged;
        }

        private void OnSettingChanged(object sender, string key)
        {
            if (key == EnableLogReaderLoggingSettingName)
            {
                LoadLoggingSetting();
            }
        }

        private void LoadLoggingSetting()
        {
            _enableLogging = _settingsService.GetValue(EnableLogReaderLoggingSettingName) as bool? ?? true;
        }

        public void Start()
        {
            Task.Run(() => WatchLogFile(_cancellationTokenSource.Token));
        }

        public void Stop()
        {
            _cancellationTokenSource.Cancel();
            _settingsService.SettingChanged -= OnSettingChanged;
        }

        private async Task WatchLogFile(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (new FileInfo(_logFilePath).Length < _lastPosition)
                    {
                        _lastPosition = 0;
                    }

                    using (var fs = new FileStream(_logFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        fs.Seek(_lastPosition, SeekOrigin.Begin);
                        using (var sr = new StreamReader(fs))
                        {
                            string? line;
                                while ((line = await sr.ReadLineAsync()) != null)
                                {
                                    ParseLine(line);
                                }
                                _lastPosition = fs.Position;
                        }
                    }
                }
                catch (Exception ex)
                {
                    DebugService.Instance.Log($"[LogReader] Error: {ex}");
                }
                await Task.Delay(1000, cancellationToken);
            }
        }

        private void ParseLine(string line)
        {
            if (_enableLogging)
            {
                DebugService.Instance.Log($"[LogReader] Parsing line: {line}");
            }
            // /who command
            if (line.Contains("ONLINE:"))
            {
                var cleanedLine = Regex.Replace(line, @"^.*\[CHAT\]\s*", "");
                var players = cleanedLine.Replace("ONLINE: ", "").Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries)
                                  .Select(p => Regex.Replace(p.Trim(), @"\s*\(\d+\)$", "")).ToList();
                if (players.Any())
                {
                    WhoResult?.Invoke(players);
                    return;
                }
            }

            // Player leave in Round.
            var match = Regex.Match(line, @"\[.*\] \[Client thread/INFO\]: \[CHAT\] (\w+) disconnected\.");
            if (match.Success)
            {
                PlayerLeft?.Invoke(match.Groups[1].Value);
                return;
            }

            // Player rejoin in Round.
            match = Regex.Match(line, @"\[.*\] \[Client thread/INFO\]: \[CHAT\] (\w+) reconnected\.");
            if (match.Success)
            {
                PlayerJoined?.Invoke(match.Groups[1].Value);
                return;
            }

            // Sent message
            match = Regex.Match(line, @"\[.*\] \[Client thread/INFO\]: \[CHAT\] .*\uFFFD.\[.*\]\s(\w+)\uFFFD.:\s.*|\[.*\] \[Client thread/INFO\]: \[CHAT\].* \uFFFD.(\w+)\uFFFD.:\s");
            if (match.Success)
            {
                if (match.Groups[1].Success)
                    PlayerJoined?.Invoke(match.Groups[1].Value);
                if (match.Groups[2].Success)
                    PlayerJoined?.Invoke(match.Groups[2].Value);
                return;
            }

            // Final kill
            match = Regex.Match(line, @"\[.*\] \[Client thread/INFO\]: \[CHAT\]\s(\w+).*\.\sFINAL KILL!");
            if (match.Success)
            {
                PlayerLeft?.Invoke(match.Groups[1].Value);
                return;
            }

            // Server Switch
            match = Regex.Match(line, @"\[.*\] \[Client thread/INFO\]: \[CHAT\] Sending you to mini.*!");
            if (match.Success)
            {
                ClearList?.Invoke();
                return;
            }
        }
    }
}