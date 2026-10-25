namespace Erlang;

public sealed partial class ProcessRuntime
{
    internal bool HasWindowsDdllError416 => OperatingSystem.IsWindows() && Environment.Is64BitProcess;

    internal void RequireDdllErrorActor416(ProcessContext context)
    {
        lock (gate)
        {
            if (!ReferenceEquals(context.Runtime, this))
                throw new ArgumentException(DdllErrorNames416.Foreign, nameof(context));
            RequireEtsFoundationActor(context);
            context.Cancellation.ThrowIfCancellationRequested();
            if (!HasWindowsDdllError416) throw new PlatformNotSupportedException(DdllErrorNames416.Profile);
        }
    }
}
