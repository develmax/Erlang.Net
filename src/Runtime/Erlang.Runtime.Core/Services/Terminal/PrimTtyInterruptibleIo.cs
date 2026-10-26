using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Erlang;

internal static class PrimTtyInterruptibleIo
{
    internal static async Task<T> Run<T>(Func<T> body, CancellationToken cancellation)
    {
        cancellation.ThrowIfCancellationRequested();
        var opened = new TaskCompletionSource<nint>(TaskCreationOptions.RunContinuationsAsynchronously);
        var settled = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        var worker = new Thread(() =>
        {
            nint handle = PrimTtyWindowsNative.OpenThread(PrimTtyNames.ThreadTerminate, false,
                PrimTtyWindowsNative.GetCurrentThreadId());
            int openError = handle == 0 ? Marshal.GetLastPInvokeError() : 0;
            opened.TrySetResult(handle);
            try
            {
                if (handle == 0)
                    throw new Win32Exception(openError);
                cancellation.ThrowIfCancellationRequested();
                settled.TrySetResult(body());
            }
            catch (Exception error)
            {
                settled.TrySetException(error);
            }
        })
        { IsBackground = false };
        worker.Start();
        nint thread = await opened.Task.ConfigureAwait(false);
        var errors = new List<Exception>();
        var cancelCodes = new HashSet<int>();
        T? result = default;
        bool hasResult = false;
        try
        {
            // Retry cancellation to close the race before a blocking syscall enters.
            // This thread handle belongs only to this invocation, never the next job.
            while (!settled.Task.IsCompleted)
            {
                if (cancellation.IsCancellationRequested && thread != 0
                    && !PrimTtyWindowsNative.CancelSynchronousIo(thread))
                {
                    int code = Marshal.GetLastPInvokeError();
                    if (code != PrimTtyNames.IoNotFound && cancelCodes.Add(code))
                        errors.Add(new Win32Exception(code));
                }
                await Task.WhenAny(settled.Task, Task.Delay(PrimTtyNames.RetryMilliseconds)).ConfigureAwait(false);
            }
            result = await settled.Task.ConfigureAwait(false);
            hasResult = true;
            cancellation.ThrowIfCancellationRequested();
        }
        catch (Exception error)
        {
            errors.Add(error);
        }
        finally
        {
            // Never return or close the cancellation handle while its actual
            // thread/syscall is live, even if the OS rejected cancellation.
            worker.Join();
            if (thread != 0 && !PrimTtyWindowsNative.CloseHandle(thread))
                errors.Add(new Win32Exception(Marshal.GetLastPInvokeError()));
        }
        if (errors.Count == 1)
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(errors[0]).Throw();
        if (errors.Count > 1)
            throw new AggregateException(errors);
        if (!hasResult)
            throw new InvalidOperationException();

        return result!;
    }
}
