using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using Erlang;

namespace Erlang;

internal sealed unsafe partial class NifNativeCallEnvironment : IDisposable
{
    private static readonly ConcurrentDictionary<nint, NifNativeCallEnvironment> Environments = new();
    private static long nextTerm;
    private readonly ConcurrentDictionary<nuint, NifNativeTermValue> terms = new();
    private Dictionary<object, nint>? tupleArrays;
    private HashSet<NifNativeTermNode>? retainedNodes;
    private HashSet<NifNativeBinaryStorage>? binaryStorage;
    private Dictionary<BitString, NifNativeBinaryStorage>? inspectedBinaries;
    private nint pointer;
    private nuint pendingReasonHandle;
    private long pendingReasonVersion;

    internal ProcessRuntime Runtime
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
    internal bool IsIndependent { get; }
    internal bool AccessibleOnCurrentThread => IsIndependent || threadId == Environment.CurrentManagedThreadId;
    internal nint Pointer => pointer != 0 ? pointer : throw new ObjectDisposedException(nameof(NifNativeCallEnvironment));
    internal static int ActiveCount => Environments.Count;

    internal NifNativeCallEnvironment(
        ProcessContext caller,
        ModuleCodeImage image,
        nint privateData,
        NifNativeFailureState failure
    ) : this(
        caller.Runtime,
        image,
        privateData,
        failure,
        false
    )
    {
    }

    internal NifNativeCallEnvironment(ProcessRuntime runtime, ModuleCodeImage image)
        : this(
            runtime,
            image,
            0,
            new NifNativeFailureState(),
            true
        )
    {
    }

    private NifNativeCallEnvironment(
        ProcessRuntime runtime,
        ModuleCodeImage image,
        nint privateData,
        NifNativeFailureState failure,
        bool independent
    )
    {
        Failure = failure;
        Runtime = runtime;
        Image = image;
        PrivateData = privateData;
        IsIndependent = independent;
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
            !environment.AccessibleOnCurrentThread)
            throw new InvalidOperationException(NifNativeDiagnostics.ExpiredEnvironment);

        return environment;
    }

    internal static NifNativeCallEnvironment ResolveTermOwner(nuint handle, nint environmentHint)
    {
        // The argument is an optional index hint, never dereferenced or required.
        if (Environments.TryGetValue(environmentHint, out var hinted) &&
            hinted.AccessibleOnCurrentThread && hinted.pointer != 0 && hinted.terms.ContainsKey(handle))
            return hinted;
        foreach (var entry in Environments)
        {
            var environment = entry.Value;
            if (environment.AccessibleOnCurrentThread && environment.pointer != 0 &&
                environment.terms.ContainsKey(handle))
                return environment;
        }

        throw new InvalidOperationException(NifNativeDiagnostics.ForeignTermHandle);
    }

    internal static NifNativeCallEnvironment CurrentInvocation()
    {
        var environment = NifNativeCallbackScope.Current.InvocationEnvironment
            ?? throw new InvalidOperationException(NifNativeInvocationNames.Unbound);

        return Resolve(environment.Pointer);
    }

    internal int BorrowedTupleArrayCount => tupleArrays?.Count ?? 0;

    internal nuint* BorrowTupleElements(NifNativeTermValue value)
    {
        _ = Pointer;
        if (!value.TryTupleArity(out int arity))
            throw new InvalidOperationException(NifNativeBinaryNames.TupleShape);
        tupleArrays ??= new(ReferenceEqualityComparer.Instance);
        object key = (object?)value.Pending ?? value.StaticTerm!;
        if (tupleArrays.TryGetValue(key, out nint existing))
            return (nuint*)existing;
        nuint bytes = checked((nuint)Math.Max(arity, 1) * (nuint)sizeof(nuint));
        nuint* array = (nuint*)NativeMemory.Alloc(bytes);
        if (array == null)
            throw new OutOfMemoryException();
        try
        {
            for (int index = 0; index < arity; index++)
            {
                NifNativeTermValue child = value.Pending is NifNativeTupleNode pending
                    ? pending.Elements[index]
                    : new(((TupleTerm)value.StaticTerm!).Items[index]);
                array[index] = AddValue(child);
            }
            tupleArrays.Add(key, (nint)array);

            return array;
        }
        catch
        {
            NativeMemory.Free(array);
            throw;
        }
    }

    internal nuint Add(Term term) => AddValue(new NifNativeTermValue(term));

    internal nuint AddValue(NifNativeTermValue value)
    {
        _ = Pointer;
        RetainGuestValue516(value);
        if (value.Pending is { } pending)
            RetainGraph(pending);
        long handle = Interlocked.Increment(ref nextTerm);
        if (handle <= 0)
            throw new InvalidOperationException(NifNativeDiagnostics.TermHandleExhausted);
        if (!terms.TryAdd((nuint)handle, value))
            throw new InvalidOperationException(NifNativeDiagnostics.TermHandleExhausted);

        return (nuint)handle;
    }

    internal NifNativeTermValue GetValue(nuint handle)
        => terms.TryGetValue(handle, out var value)
            ? value
            : throw new InvalidOperationException(NifNativeDiagnostics.ForeignTermHandle);

    internal Term Get(nuint handle) => NifNativeTermGraph.Resolve(GetValue(handle));

    private void RetainGraph(NifNativeTermNode root)
    {
        retainedNodes ??= new(ReferenceEqualityComparer.Instance);
        if (retainedNodes.Contains(root))
            return;
        var stack = new Stack<(NifNativeTermNode Node, bool Exit)>();
        stack.Push((root, false));
        while (stack.TryPop(out var frame))
        {
            if (retainedNodes.Contains(frame.Node))
                continue;
            if (frame.Exit)
            {
                if (frame.Node is NifNativeBinaryNode binary)
                    RetainBinaryStorage(binary.Storage);
                retainedNodes.Add(frame.Node);
                continue;
            }
            stack.Push((frame.Node, true));
            for (int index = frame.Node.ChildCount - 1; index >= 0; index--)
                if (frame.Node.Child(index).Pending is { } child && !retainedNodes.Contains(child))
                    stack.Push((child, false));
        }
    }

    internal void RetainBinaryStorage(NifNativeBinaryStorage storage)
    {
        _ = Pointer;
        binaryStorage ??= new(ReferenceEqualityComparer.Instance);
        if (!binaryStorage.Add(storage))
            return;
        try
        {
            storage.RetainEnvironment();
        }
        catch
        {
            binaryStorage.Remove(storage);
            throw;
        }
    }

    internal NifNativeBinaryStorage CreateBinaryStorage(nuint size)
    {
        _ = Pointer;
        var storage = NifNativeBinaryStorage.TryAllocate(size, nativeOwned: false) ?? throw new OutOfMemoryException();
        try
        {
            RetainBinaryStorage(storage);

            return storage;
        }
        catch
        {
            storage.ReleaseUnowned();
            throw;
        }
    }

    internal NifNativeBinaryStorage InspectBinaryStorage(BitString binary)
    {
        inspectedBinaries ??= new(ReferenceEqualityComparer.Instance);
        if (inspectedBinaries.TryGetValue(binary, out var existing))
            return existing;
        var storage = CreateBinaryStorage((nuint)binary.ByteSpan.Length);
        binary.ByteSpan.CopyTo(new Span<byte>(storage.Pointer, binary.ByteSpan.Length));
        inspectedBinaries.Add(binary, storage);

        return storage;
    }

    internal void Raise(nuint reason)
    {
        var value = GetValue(reason);
        if (value.IsPending)
            Failure.RaiseDeferred(() => NifNativeTermGraph.Resolve(value));
        else
            Failure.Raise(value.StaticTerm!);
        pendingReasonHandle = reason;
        pendingReasonVersion = Failure.PendingVersion;
    }

    internal nuint PendingReasonHandle
    {
        get
        {
            if (!Failure.HasGuestException)
                return 0;
            if (pendingReasonHandle == 0 || pendingReasonVersion != Failure.PendingVersion)
            {
                pendingReasonHandle = Add(Failure.GuestPendingReason!);
                pendingReasonVersion = Failure.PendingVersion;
            }

            return pendingReasonHandle;
        }
    }

    internal void ClearIndependent()
    {
        if (!IsIndependent)
            throw new InvalidOperationException(NifNativeEnvironmentNames.ProcessBound);
        _ = Pointer;
        ClearTerms();
    }

    private void ClearTerms()
    {
        ClearMapIterators504();
        if (tupleArrays is not null)
        {
            foreach (var entry in tupleArrays)
                NativeMemory.Free((void*)entry.Value);
            tupleArrays.Clear();
        }
        if (binaryStorage is not null)
        {
            foreach (var storage in binaryStorage)
                storage.ReleaseEnvironment();
            binaryStorage.Clear();
        }
        inspectedBinaries?.Clear();
        retainedNodes?.Clear();
        terms.Clear();
        pendingReasonHandle = 0;
        pendingReasonVersion = 0;
        ClearGuestRootInventory516();
        Interlocked.Exchange(ref guestRoots516, null)?.Dispose();
    }

    public void Dispose()
    {
        nint previous = Interlocked.Exchange(ref pointer, 0);
        if (previous == 0)
            return;
        Environments.TryRemove(previous, out _);
        try
        {
            ClearTerms();
        }
        finally
        {
            NativeMemory.Free((void*)previous);
        }
    }
}
