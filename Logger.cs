using System;

namespace NextEvent
{
    internal static class Logger
    {
        public static void Log<T>(T message) => Console.Error.WriteLine(message);
    }
}