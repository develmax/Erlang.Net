using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using Erlang;

namespace Erlang;

internal sealed unsafe class NifNativeCallEnvironment : IDisposable
{
    private static readonly ConcurrentDictionary<nint, NifNativeCallEnvironment> Environments = new();
    private static long nextTerm;
    private readonly Dictionary<nuint, Term> terms = new();
    private nint pointer;
    private nuint pendingReasonHandle;

    internal ProcessContext Caller
    {
        get;
    }
    internal ModuleCodeImage Image
    {
        get;
    }
    internal nint PrivateData
    {
        get;
    }
    internal NifNativeFailureState Failure { get; }
    private readonly int threadId = Environment.CurrentManagedThreadId;
    internal nint Pointer => pointer != 0 ? pointer : throw new ObjectDisposedException(nameof(NifNativeCallEnvironment));
    internal static int ActiveCount => Environments.Count;

    internal NifNativeCallEnvironment(
        ProcessContext caller,
        ModuleCodeImage image,
        nint privateData,
        NifNativeFailureState failure
    )
    {
        Failure = failure;
        Caller = caller;
        Image = image;
        PrivateData = privateData;
        pointer = (nint)NativeMemory.Alloc(NifNativeAbi.EnvironmentStorageBytes);
        if (pointer == 0)
            throw new OutOfMemoryException();
        if (!Environments.TryAdd(pointer, this))
        {
            NativeMemory.Free((void*)pointer);
            pointer = 0;
            throw new InvalidOperationException(NifNativeDiagnostics.DuplicateEnvironment);
        }
    }

    internal static NifNativeCallEnvironment Resolve(nint pointer)
    {
        if (!Environments.TryGetValue(pointer, out var environment) ||
            environment.threadId != Environment.CurrentManagedThreadId)
            throw new InvalidOperationException(NifNativeDiagnostics.ExpiredEnvironment);

        return environment;
    }

    internal nuint Add(Term term)
    {
        _ = Pointer;
        long handle = Interlocked.Increment(ref nextTerm);
        if (handle <= 0)
            throw new InvalidOperationException(NifNativeDiagnostics.TermHandleExhausted);
        terms.Add((nuint)handle, term);

        return (nuint)handle;
    }

    internal Term Get(nuint handle)
        => terms.TryGetValue(handle, out var term)
            ? term
            : throw new InvalidOperationException(NifNativeDiagnostics.ForeignTermHandle);

    internal void Raise(nuint reason)
    {
        Failure.Raise(Get(reason));
        pendingReasonHandle = reason;
    }

    internal nuint PendingReasonHandle
    {
        get
        {
            if (Failure.GuestPendingReason is not { } reason)
                return 0;
            if (pendingReasonHandle == 0)
                pendingReasonHandle = Add(reason);

            return pendingReasonHandle;
        }
    }

    public void Dispose()
    {
        nint previous = Interlocked.Exchange(ref pointer, 0);
        if (previous == 0)
            return;
        Environments.TryRemove(previous, out _);
        terms.Clear();
        NativeMemory.Free((void*)previous);
    }
}
