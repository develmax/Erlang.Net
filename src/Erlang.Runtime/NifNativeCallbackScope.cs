namespace Erlang;

internal sealed class NifNativeCallbackScope : IDisposable
{
    [ThreadStatic]
    private static NifNativeCallbackScope? current;
    [ThreadStatic]
    private static Exception? unsupportedThreadFailure;
    private readonly NifNativeCallbackScope? previous;

    internal NifNativeAllocationOwner Allocations
    {
        get;
    }
    internal NifNativeFailureState Failure
    {
        get;
    }
    internal bool ReallocationEnabled
    {
        get;
    }
    internal NifNativeCallEnvironment? InvocationEnvironment { get; private set; }

    internal NifNativeEnvironmentBinding BindEnvironment(NifNativeCallEnvironment environment)
    {
        _ = NifNativeCallEnvironment.Resolve(environment.Pointer);
        if (!ReferenceEquals(environment.Failure, Failure))
            throw new InvalidOperationException(NifNativeInvocationNames.ForeignFailureState);
        var binding = new NifNativeEnvironmentBinding(this, InvocationEnvironment);
        InvocationEnvironment = environment;

        return binding;
    }

    internal void RestoreEnvironment(NifNativeCallEnvironment? environment)
        => InvocationEnvironment = environment;

    internal static NifNativeCallbackScope Current
        => current ?? throw new InvalidOperationException(NifNativeDiagnostics.NativeThreadUnsupported);

    internal NifNativeCallbackScope(NifNativeAllocationOwner allocations, NifNativeFailureState failure) : this(allocations, failure, false)
    {
    }

    internal NifNativeCallbackScope(NifNativeAllocationOwner allocations, NifNativeFailureState failure, bool reallocationEnabled)
    {
        ReallocationEnabled = reallocationEnabled;
        previous = current;
        Allocations = allocations;
        Failure = failure;
        current = this;
    }

    internal static void Fail(Exception exception)
    {
        if (current is { } scope)
            scope.Failure.FailHost(exception);
        else
            unsupportedThreadFailure ??= exception;
    }

    public void Dispose()
    {
        InvocationEnvironment = null;
        current = previous;
    }
}
