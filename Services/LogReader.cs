
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace JustBedwars.Services
{
    public class LogReader
    {
        public event Action<string> PlayerJoined = delegate { };
        public event Action<string> PlayerLeft = delegate { };
        public event Action<List<string>> WhoResult = delegate { };

        private readonly string _logFilePath;
        private long _lastPosition;
        private readonly CancellationTokenSource _cancellationTokenSource;

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
        }

        public void Start()
        {
            Task.Run(() => WatchLogFile(_cancellationTokenSource.Token));
        }

        public void Stop()
        {
            _cancellationTokenSource.Cancel();
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
                                    DebugService.Instance.Log($"[LogReader] Read line: {line}");
                                    ParseLine(line);
                                }
                                _lastPosition = fs.Position;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
                await Task.Delay(1000, cancellationToken);
            }
        }

        private void ParseLine(string line)
        {
            // /who command
            if (line.Contains("ONLINE:"))
            {
                var cleanedLine = Regex.Replace(line, @"^.*\[CHAT\]\s*", "");
                var players = cleanedLine.Replace("ONLINE: ", "").Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries)
                                  .Select(p => p.Trim()).ToList();
                if (players.Any())
                {
                    WhoResult?.Invoke(players);
                    return;
                }
            }

            // Player leave in Lobby.
            var match = Regex.Match(line, @"(\w+) has quit!");
            if (match.Success)
            {
                PlayerLeft?.Invoke(match.Groups[1].Value);
                return;
            }

            // Player leave in Round.
            match = Regex.Match(line, @"(\w+) disconnected");
            if (match.Success)
            {
                PlayerLeft?.Invoke(match.Groups[1].Value);
                return;
            }

            // Player rejoin in Round.
            match = Regex.Match(line, @"(\w+) reconnected");
            if (match.Success)
            {
                PlayerJoined?.Invoke(match.Groups[1].Value);
                return;
            }

            // Sent message while waiting
            match = Regex.Match(line, @"\[CHAT\] .* (\w+)\uFFFD.:\s");
            if (match.Success)
            {
                PlayerJoined?.Invoke(match.Groups[1].Value);
                return;
            }

            // Final kill
            match = Regex.Match(line, @"\[CHAT\]\s(\w+)\s.*\s(\w+)\.\sFINAL KILL!");
            if (match.Success)
            {
                PlayerLeft?.Invoke(match.Groups[1].Value);
                return;
            }
        }
    }
}
