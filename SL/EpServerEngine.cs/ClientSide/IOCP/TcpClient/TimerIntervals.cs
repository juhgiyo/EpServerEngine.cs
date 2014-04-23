using System.Threading;

namespace System.Net.Sockets
{
    internal static class TimerIntervals
    {
        public static TimeSpan Never = TimeSpan.FromMilliseconds(Timeout.Infinite);
    }
}