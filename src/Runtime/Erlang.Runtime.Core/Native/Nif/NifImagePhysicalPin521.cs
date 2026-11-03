namespace Erlang;

internal sealed class NifImagePhysicalPin521(NifImagePhysicalScope521 scope, IDisposable? custody)
{
    private Task? settled;
    private IDisposable? roots = custody;

    // Null means the scheduler never reached queue admission. Once attached,
    // only the REAL invocation's Settled task permits physical release.
    internal bool IsPhysicallySettled521 => Volatile.Read(ref settled)?.IsCompleted ?? true;

    internal void Register521(Task actualSettled)
    {
        if (Interlocked.CompareExchange(ref settled, actualSettled, null) is not null)
            throw new InvalidOperationException(NifNativeScheduling521Names.MissingImageScope);
        // Called under the dirty queue gate BEFORE queue publication. If any
        // continuation allocation fails, Enqueue fails and never exposes work.
        if (ExecutionContext.IsFlowSuppressed())
            RegisterContinuation521(actualSettled);
        else
        {
            using var flow = ExecutionContext.SuppressFlow();
            RegisterContinuation521(actualSettled);
        }
    }

    private void RegisterContinuation521(Task actualSettled)
    {
        _ = actualSettled.ContinueWith(
            static (_, state) => ((NifImagePhysicalPin521)state!).Settled521(),
            this,
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default
        );
    }

    private void Settled521() => scope.TryFinish521();

    internal void ReleaseCustody521() => Interlocked.Exchange(ref roots, null)?.Dispose();
}
