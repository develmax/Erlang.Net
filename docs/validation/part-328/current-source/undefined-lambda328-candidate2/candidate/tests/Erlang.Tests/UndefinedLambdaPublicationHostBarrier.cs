using System.Reflection;
using System.Runtime.ExceptionServices;
using Erlang;

namespace Erlang.Tests;

internal sealed class UndefinedLambdaPublicationHostBarrier(Delegate genuineFactory, bool block, Action? constructed = null) : IDisposable
{
    private readonly ManualResetEventSlim release = new(!block);
    private readonly TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int constructions;

    internal Task Entered => entered.Task;
    internal int Constructions => Volatile.Read(ref constructions);

    public FunctionTerm Construct(IReadOnlyList<Term> environment, object? invocation)
    {
        FunctionTerm result;
        try
        {
            result = (FunctionTerm)genuineFactory.DynamicInvoke(environment, invocation)!;
        }
        catch (TargetInvocationException failure) when (failure.InnerException is { } inner)
        {
            entered.TrySetException(inner);
            ExceptionDispatchInfo.Capture(inner).Throw();
            throw;
        }
        Interlocked.Increment(ref constructions);
        constructed?.Invoke();
        entered.TrySetResult();
        if (!release.Wait(UndefinedLambdaPublicationHostNames.Deadline))
            throw new TimeoutException(UndefinedLambdaPublicationHostNames.Broken);

        return result;
    }

    internal void Release() => release.Set();

    public void Dispose() => release.Dispose();
}
