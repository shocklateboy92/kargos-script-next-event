using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Ical.Net.CalendarComponents;
using Calendar = Ical.Net.Calendar;

namespace NextEvent
{
    internal static class Program
    {
        private const string ConfigFileName = "kargos-script-next-event";

        public static async Task Main(string[] args)
        {
            var calendarUrl = File.ReadAllText(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                ConfigFileName));

            using var client = new HttpClient();
            var response = await client.GetAsync(calendarUrl);
            if (!response.IsSuccessStatusCode)
            {
                Console.Error.WriteLine(
                    $"Failed to load calendar. Server returned {response.StatusCode}");
                return;
            }

            var time = args.Length > 0 ? DateTime.Parse(args.First()) : DateTime.Now;
            Logger.Log($"Getting events from {time.ToString(CultureInfo.CurrentCulture)}");

            var cal = Calendar.Load(await response.Content.ReadAsStreamAsync());
            var events = cal.Calendar.GetOccurrences(time, time + TimeSpan.FromDays(1))
                .Select(e => (CalendarEvent) e.Source);

            var orderEvents = events
                .Where(f => f.Start.Value.TimeOfDay > time.TimeOfDay)
                .OrderBy(f => f.Start.Value.TimeOfDay)
                .ToList();
            Logger.Log(string.Join('\n',
                orderEvents.Select(e => $"{e.Start.Value.TimeOfDay}: {e.Summary}")));

            if (orderEvents.Count == 0)
            {
                Logger.Log("No events within 24h");
                return;
            }

            var nextEvent = orderEvents.First();
            Console.WriteLine(
                $"{nextEvent.Start.Value:HH:mm} {nextEvent.Summary} | iconName=view-calendar");
        }
    }
}