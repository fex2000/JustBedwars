using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Threading.Tasks;

namespace JustBedwars.Services
{
    public class AptabaseClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _sessionId;

        public AptabaseClient(string hostUrl, string appKey)
        {
            _sessionId = Guid.NewGuid().ToString().ToLowerInvariant();

            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri(hostUrl.TrimEnd('/') + "/api/v0/events");

            _httpClient.DefaultRequestHeaders.Add("App-Key", appKey);
        }

        public async Task TrackEvent(string eventName, object props = null)
        {
            DebugService.Instance.Log($"[Tracking] Sent event {eventName} to {_httpClient.BaseAddress}");
            var eventData = new
            {
                timestamp = DateTime.UtcNow.ToString("O"),
                sessionId = _sessionId,
                eventName = eventName,
                systemProps = new
                {
                    osName = "Windows",
                    osVersion = Environment.OSVersion.Version.ToString(),
                    locale = System.Globalization.CultureInfo.CurrentCulture.Name,
                    appVersion = Assembly.GetExecutingAssembly().GetName().Version!.ToString(),
                    sdkVersion = "justbedwars-tracker-1.0",
#if DEBUG
                    isDebug = true,
#else
                    isDebug = false,
#endif
                },
                props = props ?? new {}
            };

            var payload = new[] { eventData };

            try
            {
                var result = await _httpClient.PostAsJsonAsync("", payload);
                DebugService.Instance.Log($"[Tracking] Got API Response {result}");
            }
            catch
            {
                // 
            }
        }
    }
}
