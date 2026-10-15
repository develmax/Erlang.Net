namespace NifPrototype;

internal sealed class NifCallbackScope : IDisposable
{
    [ThreadStatic]
    private static NifNativeAllocationOwner? current;
    [ThreadStatic]
    private static Exception? failure;
    private readonly NifNativeAllocationOwner? previous;
    private readonly Exception? previousFailure;

    internal static NifNativeAllocationOwner Current => current ?? throw new InvalidOperationException("NIF native threads are outside this fixture ABI subset.");

    internal NifCallbackScope(NifNativeAllocationOwner owner)
    {
        previous = current;
        previousFailure = failure;
        current = owner;
        failure = null;
    }

    internal static void Fail(Exception exception) => failure ??= exception;

    internal static void ThrowPending()
    {
        if (failure is not null)
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
    }

    public void Dispose()
    {
        current = previous;
        failure = previousFailure;
    }
}
