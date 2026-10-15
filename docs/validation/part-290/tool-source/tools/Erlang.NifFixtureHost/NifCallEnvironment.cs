using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using Erlang;

namespace Erlang.NifFixtureHost;

internal sealed unsafe class NifCallEnvironment : IDisposable
{
    private static readonly ConcurrentDictionary<nint, NifCallEnvironment> Environments = new();
    private static long nextTerm;
    private readonly Dictionary<nuint, Term> terms = new();
    private nint pointer;

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
    internal Exception? PendingException
    {
        get; private set;
    }
    internal nint Pointer => pointer != 0 ? pointer : throw new ObjectDisposedException(nameof(NifCallEnvironment));
    internal static int ActiveCount => Environments.Count;

    internal NifCallEnvironment(
        ProcessContext caller,
        ModuleCodeImage image,
        nint privateData
    )
    {
        Caller = caller;
        Image = image;
        PrivateData = privateData;
        pointer = (nint)NativeMemory.Alloc(NifAbiLayout.EnvironmentStorageBytes);
        if (pointer == 0)
            throw new OutOfMemoryException();
        if (!Environments.TryAdd(pointer, this))
        {
            NativeMemory.Free((void*)pointer);
            pointer = 0;
            throw new InvalidOperationException(NifHostDiagnostics.DuplicateEnvironment);
        }
    }

    internal static NifCallEnvironment Resolve(nint pointer)
        => Environments.TryGetValue(pointer, out var environment)
            ? environment
            : throw new InvalidOperationException(NifHostDiagnostics.ExpiredEnvironment);

    internal nuint Add(Term term)
    {
        _ = Pointer;
        long handle = Interlocked.Increment(ref nextTerm);
        if (handle <= 0)
            throw new InvalidOperationException(NifHostDiagnostics.TermHandleExhausted);
        terms.Add((nuint)handle, term);

        return (nuint)handle;
    }

    internal Term Get(nuint handle)
        => terms.TryGetValue(handle, out var term)
            ? term
            : throw new InvalidOperationException(NifHostDiagnostics.ForeignTermHandle);

    internal void Fail(Exception exception) => PendingException ??= exception;

    internal void ThrowPending()
    {
        if (PendingException is not null)
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(PendingException).Throw();
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
