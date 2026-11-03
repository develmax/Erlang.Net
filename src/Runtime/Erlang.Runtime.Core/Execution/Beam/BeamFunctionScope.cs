namespace Erlang;

internal sealed class BeamFunctionScope : IDisposable
{
    private readonly ProcessContext context;
    private readonly IDisposable function;
    private IDisposable? location;
    private IDisposable? continuation;
    private Term? currentLocation;
    private readonly BeamOwnedRoots507? roots507;
    private BeamSourceLocationCache154? locationCache154;
    private ProcessTermFrame506? locationRoots146;
    private Dictionary<Term, int>? locationSlots146;

    internal BeamFunctionScope(ProcessContext context, BeamLinkedFunction target, IReadOnlyList<Term> arguments)
    {
        this.context = context;
        Arguments = Array.AsReadOnly(arguments.ToArray());
        if (context.OwnedExecutionEnabled683)
            roots507 = new(context, 2);
        try
        {
            if (roots507 is not null)
            {
                locationCache154 = context.AcquireBeamSourceLocations154();
                locationRoots146 = locationCache154.Frame154;
                locationSlots146 = locationCache154.Slots154;
            }
            roots507?.Prepare(0, BeamRegisterSlot.FromTerm(Term.Tuple(arguments.ToArray())), false);
            roots507?.Commit(0);
            function = context.EnterBeamFunction(target.Module, target.Name, target.Arity);
        }
        catch (Exception original)
        {
            Exception? locationFailure = TryDispose146(locationCache154);
            Exception? argumentFailure = TryDispose146(roots507);
            if (locationFailure is not null || argumentFailure is not null)
                throw AggregateFailures146(
                    original,
                    locationFailure,
                    argumentFailure,
                    null,
                    null,
                    null
                );
            throw;
        }
    }

    internal IReadOnlyList<Term> Arguments { get; }

    internal void SetContinuation(BeamLinkedCode code, int instructionIndex)
    {
        ClearContinuation();
        continuation = context.EnterBeamContinuation(code.ContinuationIdentities.GetOrAdd(instructionIndex, _ => new object()));
    }

    internal void ClearContinuation()
    {
        continuation?.Dispose();
        continuation = null;
    }

    internal void SetLocation(Term value)
    {
        if (ReferenceEquals(currentLocation, value))
            return;
        location?.Dispose();
        location = null;
        currentLocation = null;
        if (roots507 is null)
        {
            location = context.EnterSourceLocation(value);
            currentLocation = value;

            return;
        }
        // Every shared entry has a real registered frame root, owned until the
        // final active function scope releases this exact context cache.
        BeamSourceLocationCache154 cache = locationCache154!;
        if (!ReferenceEquals(locationRoots146, cache.Frame154) || !ReferenceEquals(locationSlots146, cache.Slots154))
            throw new InvalidOperationException();
        Term owned = cache.Get154(value);
        location = context.EnterSourceLocation(owned);
        currentLocation = value;
    }

    public void Dispose()
    {
        IDisposable? previousContinuation = continuation;
        continuation = null;
        Exception? continuationFailure = TryDispose146(previousContinuation);
        IDisposable? previousLocation = location;
        location = null;
        Exception? locationFailure = TryDispose146(previousLocation);
        Exception? functionFailure = TryDispose146(function);
        BeamSourceLocationCache154? previousCache = locationCache154;
        locationCache154 = null;
        locationRoots146 = null;
        locationSlots146 = null;
        currentLocation = null;
        Exception? frameFailure = TryDispose146(previousCache);
        Exception? argumentFailure = TryDispose146(roots507);
        if (continuationFailure is not null || locationFailure is not null || functionFailure is not null
            || frameFailure is not null || argumentFailure is not null)
            throw AggregateFailures146(
                null,
                continuationFailure,
                locationFailure,
                functionFailure,
                frameFailure,
                argumentFailure
            );
    }

    // Static helpers allocate no closure/container before every cleanup attempt.
    private static Exception? TryDispose146(IDisposable? value)
    {
        try
        {
            value?.Dispose();

            return null;
        }
        catch (Exception failure)
        {
            return failure;
        }
    }

    // Only called AFTER all cleanup attempts; allocation failure cannot skip one.
    private static AggregateException AggregateFailures146(
        Exception? original,
        Exception? first,
        Exception? second,
        Exception? third,
        Exception? fourth,
        Exception? fifth
    )
    {
        var failures = new List<Exception>();
        if (original is not null) failures.Add(original);
        if (first is not null) failures.Add(first);
        if (second is not null) failures.Add(second);
        if (third is not null) failures.Add(third);
        if (fourth is not null) failures.Add(fourth);
        if (fifth is not null) failures.Add(fifth);

        return new AggregateException(failures);
    }
}
