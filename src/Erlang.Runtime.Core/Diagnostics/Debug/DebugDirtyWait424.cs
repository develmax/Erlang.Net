using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Erlang;

internal static class DebugDirtyWait424
{
    [DllImport("kernel32.dll", EntryPoint = "Sleep", ExactSpelling = true)]
    private static extern void WindowsSleep(uint milliseconds);

    internal static void Wait(long milliseconds, bool busy)
    {
        long now = Stopwatch.GetTimestamp();
        // Keep the source signed-machine-word conversion and deadline wrap,
        // relative to this VM's actual monotonic unit. Extending to an Int128
        // deadline would incorrectly turn wait(INT64_MAX) into an endless wait.
        long duration = unchecked((long)((Int128)milliseconds * Stopwatch.Frequency / 1000));
        long deadline = milliseconds < 0 ? now : unchecked(now + duration);
        while (now < deadline)
        {
            if (busy) Thread.Yield();
            else
            {
                long remaining = unchecked(deadline - now);
                long sleep = unchecked((long)((Int128)remaining * 1000 / Stopwatch.Frequency));
                if (OperatingSystem.IsWindows()) WindowsSleep(unchecked((uint)sleep));
                else Thread.Sleep((int)Math.Clamp(sleep, 0, int.MaxValue));
            }
            now = Stopwatch.GetTimestamp();
        }
    }
}
