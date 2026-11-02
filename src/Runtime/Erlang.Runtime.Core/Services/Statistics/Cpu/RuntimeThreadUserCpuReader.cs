using System.Diagnostics;

namespace Erlang;

// Contains no ProcessRuntime, ProcessContext or VM accounting reference.
internal sealed class RuntimeThreadUserCpuReader(ProcessThread thread, int osThreadId)
{
    internal int OsThreadId { get; } = osThreadId;

    internal long ReadTicks() => thread.UserProcessorTime.Ticks;

    internal static RuntimeThreadUserCpuReader OpenCurrent(int osThreadId)
    {
        using var process = Process.GetCurrentProcess();
        ProcessThread? selected = null;
        try
        {
            foreach (ProcessThread candidate in process.Threads)
            {
                if (candidate.Id == osThreadId)
                    selected = candidate;
                else
                    candidate.Dispose();
            }
            if (selected is null)
                throw new InvalidOperationException(RuntimeCpuTimeNames.ThreadMissing);
            _ = selected.UserProcessorTime;

            return new(selected, osThreadId);
        }
        catch
        {
            selected?.Dispose();
            throw;
        }
    }

    internal void Dispose() => thread.Dispose();
}
