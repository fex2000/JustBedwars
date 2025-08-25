using System;

namespace JustBedwars.Services
{
    public class DebugService
    {
        private static readonly DebugService _instance = new DebugService();
        public static DebugService Instance => _instance;

        public event Action<string> LogAdded = delegate { };
        public event Action<string> EmulatePlayerJoined = delegate { };
        public event Action<string> EmulatePlayerLeft = delegate { };

        public void Log(string message)
        {
            LogAdded?.Invoke(message);
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
