using System.Runtime.CompilerServices;

namespace Erlang;

internal static class UnixOsNative
{
    private static readonly ConditionalWeakTable<ProcessRuntime, UnixOsEnvironment> Environments = new();

    internal static bool TryCapture(
        ProcessRuntime? owner,
        string module,
        string function,
        int arity,
        out ErlangFunction body
    )
    {
        body = null!;
        if (owner is null || !(OperatingSystem.IsLinux() || OperatingSystem.IsMacOS()) || module != OsNativeNames.Module)
            return false;
        if ((function, arity) is not ((OsNativeNames.Env, 0) or (OsNativeNames.GetEnv, 1) or (OsNativeNames.PutEnv, 2)
            or (OsNativeNames.UnsetEnv, 1) or (OsNativeNames.GetPid, 0) or (OsNativeNames.PerfCounter, 0)
            or (OsNativeNames.SystemTime, 0) or (OsNativeNames.SystemTime, 1) or (OsNativeNames.Timestamp, 0)
            or (OsNativeNames.SetSignal, 2))) return false;
        body = (context, arguments) =>
        {
            if (!ReferenceEquals(context.Runtime, owner)) throw new InvalidOperationException(OsNativeNames.ForeignRuntime);
            context.Cancellation.ThrowIfCancellationRequested();

            return function switch
            {
                OsNativeNames.GetPid => ValueTask.FromResult(OsClockNativeValues.GetPid()),
                OsNativeNames.PerfCounter => ValueTask.FromResult(OsClockNativeValues.PerformanceCounter()),
                OsNativeNames.SystemTime => ValueTask.FromResult(arity == 0 ? OsClockNativeValues.SystemTime() : OsClockNativeValues.SystemTime(arguments[0])),
                OsNativeNames.Timestamp => ValueTask.FromResult(OsClockNativeValues.Timestamp()),
                OsNativeNames.SetSignal => throw new PlatformNotSupportedException(UnixOsNativeNames.SignalUnavailable),
                _ => Environments.GetValue(owner, _ => new UnixOsEnvironment()).Invoke(context, function, arguments)
            };
        };

        return true;
    }
}
