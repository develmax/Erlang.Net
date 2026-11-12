using System.Diagnostics;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Erlang;

// Contains no ProcessRuntime, ProcessContext or VM accounting reference.
internal sealed class RuntimeThreadUserCpuReader
{
    private readonly ProcessThread? thread;
    private readonly SafeFileHandle? handle;

    private RuntimeThreadUserCpuReader(ProcessThread? thread, SafeFileHandle? handle, int osThreadId)
    {
        this.thread = thread;
        this.handle = handle;
        OsThreadId = osThreadId;
    }

    internal int OsThreadId { get; }

    internal long ReadTicks()
    {
        if (handle is null)
            return thread!.UserProcessorTime.Ticks;
        if (!RuntimeThreadUserCpuNative.GetThreadTimes(
            handle,
            out _,
            out _,
            out _,
            out long user
        ))
            throw new Win32Exception(Marshal.GetLastPInvokeError());

        return user;
    }

    internal static RuntimeThreadUserCpuReader OpenCurrent(int osThreadId)
    {
        if (OperatingSystem.IsWindows())
        {
            SafeFileHandle handle = RuntimeThreadUserCpuNative.OpenThread(
                RuntimeCpuTimeNames.QueryLimitedThreadInformation,
                false,
                checked((uint)osThreadId)
            );
            if (handle.IsInvalid)
            {
                int error = Marshal.GetLastPInvokeError();
                handle.Dispose();
                throw new Win32Exception(error);
            }
            var reader = new RuntimeThreadUserCpuReader(null, handle, osThreadId);
            try
            {
                _ = reader.ReadTicks();

                return reader;
            }
            catch
            {
                reader.Dispose();
                throw;
            }
        }
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

            return new(selected, null, osThreadId);
        }
        catch
        {
            selected?.Dispose();
            throw;
        }
    }

    internal void Dispose()
    {
        thread?.Dispose();
        handle?.Dispose();
    }
}
