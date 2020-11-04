using System;
using System.Diagnostics;

namespace RatesCollector
{
    using _logger = Console;
    sealed class Logger
    {
        public static void Log(string message)
        {
            DateTime dt = DateTime.UtcNow;
            _logger.WriteLine($"{dt.Year}-{dt.Month.ToString().PadLeft(2, '0')}-{dt.Day.ToString().PadLeft(2, '0')} {dt.Hour.ToString().PadLeft(2, '0')}:{dt.Minute.ToString().PadLeft(2, '0')}:{dt.Second.ToString().PadLeft(2, '0')}.{dt.Millisecond.ToString().PadLeft(3, '0')} {message}");
        }
    }
}
