using System.Reflection;

namespace Erlang.Tests;

internal static class NifPhysicalCustody521Checks
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    private const string ScopeName = "Erlang.NifImagePhysicalScope521";
    private const string PrepareName = "Prepare521";
    private const string RegisterName = "Register521";
    private const string ExitName = "BodyExited521";
    private const string FailureName = "FinalReleaseFailure521";
    private static Type ScopeType => typeof(ProcessRuntime).Assembly.GetType(ScopeName, true)!;

    internal static async Task DeferredPhysicalRelease()
    {
        var physical = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var released = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var custody = new NifPhysicalCustody521TestLease(false);
        int callbacks = 0;
        using IDisposable scope = Scope(
            failure =>
        {
            Check(failure is null && custody.Disposals == 1);
            Interlocked.Increment(ref callbacks);
            released.TrySetResult();
        },
            _ => throw new InvalidOperationException()
        );
        object pin = Prepare(custody);
        pin.GetType().GetMethod(RegisterName, Members)!.Invoke(pin, new object[] { physical.Task });
        Exit(scope);
        Check(callbacks == 0 && custody.Disposals == 0);
        physical.TrySetResult();
        await released.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Check(callbacks == 1);
    }

    internal static Task CleanupFailureStillReleasesImage()
    {
        var first = new NifPhysicalCustody521TestLease(true);
        var last = new NifPhysicalCustody521TestLease(false);
        Exception? recorded = null;
        bool released = false;
        using IDisposable scope = Scope(failure =>
 {
     recorded = failure;
     released = true;
 }, _ => throw new InvalidOperationException());
        // These pins never reached Enqueue. That path must dispose argv roots
        // and release the image after the failed scheduling body exits.
        Prepare(first);
        Prepare(last);
        Exit(scope);
        Check(released && first.Disposals == 1 && last.Disposals == 1 && recorded is IOException);

        return Task.CompletedTask;
    }

    internal static Task FinalBookkeepingFailureRetainsImage()
    {
        var fatal = new OutOfMemoryException(nameof(FinalBookkeepingFailureRetainsImage));
        Exception? retained = null;
        int releaseAttempts = 0;
        using IDisposable scope = Scope(_ =>
 {
     releaseAttempts++;
     throw fatal;
 }, error => retained = error);
        Exit(scope);
        Exit(scope);
        Check(releaseAttempts == 1 && ReferenceEquals(retained, fatal));
        Check(ReferenceEquals(ScopeType.GetProperty(FailureName, Members)!.GetValue(scope), fatal));

        return Task.CompletedTask;
    }

    private static IDisposable Scope(Action<Exception?> release, Action<Exception> retain)
        => (IDisposable)Activator.CreateInstance(
            ScopeType,
            Members,
            null,
            new object[] { release, retain },
            null
        )!;

    private static object Prepare(IDisposable custody)
        => ScopeType.GetMethod(PrepareName, Members)!.Invoke(null, new object[] { custody })!;

    private static void Exit(IDisposable scope) => ScopeType.GetMethod(ExitName, Members)!.Invoke(scope, null);

    private static void Check(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(nameof(NifPhysicalCustody521Checks));
    }
}
