using System;

namespace JustBedwars.Services
{
    public class DebugService
    {
        private static readonly DebugService _instance = new DebugService();
        public static DebugService Instance => _instance;

        public event Action<string> LogAdded = delegate { };

        public void Log(string message)
        {
            LogAdded?.Invoke(message);
        }
    }
}
