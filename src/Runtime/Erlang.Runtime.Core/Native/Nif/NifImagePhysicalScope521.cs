namespace Erlang;

// All pin/continuation allocation precedes native queue publication.
internal sealed class NifImagePhysicalScope521 : IDisposable
{
    private static readonly AsyncLocal<NifImagePhysicalScope521?> Current = new();
    private readonly NifImagePhysicalScope521? previous;
    private readonly object gate = new();
    private readonly List<NifImagePhysicalPin521> pins = new();
    private readonly Action<Exception?> release;
    private readonly Action<Exception> retainOnFailure;
    private bool bodyExited;
    private bool finishing;
    internal Exception? FinalReleaseFailure521 { get; private set; }

    internal NifImagePhysicalScope521(Action<Exception?> release, Action<Exception> retainOnFailure)
    {
        this.release = release;
        this.retainOnFailure = retainOnFailure;
        previous = Current.Value;
        Current.Value = this;
    }

    internal static NifImagePhysicalPin521 Prepare521(IDisposable? custody)
    {
        NifImagePhysicalScope521 scope = Current.Value
            ?? throw new InvalidOperationException(NifNativeScheduling521Names.MissingImageScope);
        var pin = new NifImagePhysicalPin521(scope, custody);
        lock (scope.gate)
        {
            if (scope.bodyExited)
                throw new InvalidOperationException(NifNativeScheduling521Names.MissingImageScope);
            scope.pins.Add(pin);
        }

        return pin;
    }

    internal void BodyExited521()
    {
        lock (gate)
            bodyExited = true;
        TryFinish521();
    }

    internal void TryFinish521()
    {
        lock (gate)
        {
            if (!bodyExited || finishing)
                return;
            foreach (NifImagePhysicalPin521 pin in pins)
                if (!pin.IsPhysicallySettled521)
                    return;
            finishing = true;
        }
        try
        {
            Exception? failure = null;
            foreach (NifImagePhysicalPin521 pin in pins)
            {
                try
                {
                    pin.ReleaseCustody521();
                }
                catch (Exception error)
                {
                    failure = failure is null ? error : new AggregateException(failure, error);
                }
            }
            release(failure);
            pins.Clear();
        }
        catch (Exception fatal)
        {
            // Fail closed: no fallback ReleaseCall. The active image lease stays
            // admitted if aggregation or final retirement bookkeeping fails.
            FinalReleaseFailure521 = fatal;
            retainOnFailure(fatal);
        }
    }

    public void Dispose() => Current.Value = previous;
}
