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
            var events = cal.Calendar.GetOccurrences(time, time + TimeSpan.FromDays(1));

            var orderEvents = events
                .Where(f => !((CalendarEvent) f.Source).IsAllDay)
                .OrderBy(f => f.Period.StartTime.Value)
                .ToList();
            Logger.Log(string.Join('\n',
                orderEvents.Select(e => $"{e.Period.StartTime.Value}: {((CalendarEvent) e.Source).Summary}")));

            var message = "";
            if (orderEvents.Count > 0)
            {
                // Push out already started events if we have more 
                if (orderEvents.Count > 1 &&
                    orderEvents.First().Period.StartTime.Value < (time - TimeSpan.FromMinutes(10)))
                {
                    orderEvents.RemoveAt(0);
                }

                var nextEvent = (CalendarEvent) orderEvents.First().Source;
                var nextOccurence = orderEvents.First().Period.StartTime;
                message = $"{nextOccurence.Value:HH:mm} {nextEvent.Summary}";
            }
            else
            {
                message = "No events within 24h";
            }

            if (message.Length > 30)
            {
                message = message.Substring(0, 28) + "...";
            }

            Console.WriteLine(
                $"{message} | iconName=view-calendar");
        }
    }
}