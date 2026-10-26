namespace Erlang;

public sealed partial class ProcessRuntime
{
    private readonly RuntimeDebuggerMode413 debuggerMode413 = RuntimeDebuggerMode413.Disabled;

    internal bool HasDisabledDebugger413 => debuggerMode413 == RuntimeDebuggerMode413.Disabled;

    internal void RequireDisabledDebuggerActor413(ProcessContext context)
    {
        lock (gate)
        {
            if (!ReferenceEquals(context.Runtime, this))
                throw new ArgumentException(DebuggerDisabledNames413.Foreign, nameof(context));
            RequireEtsFoundationActor(context);
            context.Cancellation.ThrowIfCancellationRequested();
            if (!HasDisabledDebugger413)
                throw new InvalidOperationException(DebuggerDisabledNames413.WrongProfile);
        }
    }
}
