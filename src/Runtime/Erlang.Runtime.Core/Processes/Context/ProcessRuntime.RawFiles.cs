namespace Erlang;

public sealed partial class ProcessRuntime
{
    private readonly Dictionary<Pid, List<WeakReference<PrimFileDataResource>>> rawFileOwners = new();

    internal void RegisterRawFile(ProcessContext context, PrimFileDataResource resource)
    {
        bool rejected;
        lock (gate)
        {
            rejected = context.Terminated || !processes.TryGetValue(context.Self, out var current) || !ReferenceEquals(context, current);
            if (!rejected)
            {
                if (!rawFileOwners.TryGetValue(context.Self, out var resources))
                {
                    resources = new();
                    rawFileOwners.Add(context.Self, resources);
                }
                resources.RemoveAll(weak => !weak.TryGetTarget(out var file) || file.IsClosing);
                resources.Add(new(resource));
            }
        }
        if (rejected)
        {
            resource.Dispose();
            throw new OperationCanceledException(context.Cancellation);
        }
    }

    private Task RawFileProcessRetirement(ProcessContext context, Task previous)
    {
        if (!rawFileOwners.Remove(context.Self, out var resources))
        {
            return previous;
        }
        var files = resources.Select(weak => weak.TryGetTarget(out var file) ? file : null).OfType<PrimFileDataResource>().ToArray();
        foreach (var file in files)
        {
            file.RequestClose();
        }
        Task close;

        void Close()
        {
            foreach (var file in files)
            {
                file.Dispose();
            }
        }
        if (ExecutionContext.IsFlowSuppressed())
        {
            close = Task.Run(Close);
        }
        else
        {
            using var flow = ExecutionContext.SuppressFlow();
            close = Task.Run(Close);
        }

        return Task.WhenAll(previous, close);
    }
}
