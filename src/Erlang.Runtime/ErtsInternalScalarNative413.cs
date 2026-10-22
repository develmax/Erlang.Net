namespace Erlang;

internal static class ErtsInternalScalarNative413
{
    internal static bool TryCapture(
        ProcessRuntime? owner,
        string module,
        string function,
        int arity,
        out ErlangFunction body
    )
    {
        if (owner is not null && module == ErtsInternalScalarNativeNames413.Module
            && ((arity == 2 && function is ErtsInternalScalarNativeNames413.BinaryInteger or ErtsInternalScalarNativeNames413.ListInteger)
                || (arity == 0 && function is ErtsInternalScalarNativeNames413.TimeUnit or ErtsInternalScalarNativeNames413.PerformanceUnit)))
        {
            body = (context, arguments) =>
            {
                if (!ReferenceEquals(context.Runtime, owner) || !ReferenceEquals(context.Runtime.Modules, owner.Modules))
                    throw new InvalidOperationException(ErtsInternalScalarNativeNames413.Foreign);
                owner.ThrowIfHalted();
                context.Cancellation.ThrowIfCancellationRequested();

                return ValueTask.FromResult(function switch
                {
                    ErtsInternalScalarNativeNames413.BinaryInteger => ErtsInternalIntegerFast413.Binary(context, arguments[0], arguments[1]),
                    ErtsInternalScalarNativeNames413.ListInteger => ErtsInternalIntegerFast413.List(context, arguments[0], arguments[1]),
                    ErtsInternalScalarNativeNames413.TimeUnit => Term.I(ErlangClockUnits413.NativeFrequency),
                    _ => new Integer(ErlangClockUnits413.PerformanceFrequency)
                });
            };

            return true;
        }
        body = null!;

        return false;
    }
}
