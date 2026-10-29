using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Erlang;

// Internal constant-only component. No public producer or registry capability
// selects this type. Final storage contains actual native words, never an AST.
internal sealed unsafe class MatchSavedProgram634
{
    private static int nativeAllocations;
    private readonly object gate = new();
    private readonly ProcessRuntime runtime;
    private readonly nint fragment;
    private readonly int fragmentWords;
    private nint memory;
    private int readers;
    private bool closing;
    private int physicallyFreed;
    private readonly TaskCompletionSource retired = new(TaskCreationOptions.RunContinuationsAsynchronously);

    internal MatchSavedProgram634(ProcessRuntime runtime, MatchSavedFragment604 source)
    {
        if (RuntimeInformation.ProcessArchitecture != Architecture.X64 || !BitConverter.IsLittleEndian
            || sizeof(NativeResourceMagicHeader527) != MatchSavedNames634.MagicBytes
            || sizeof(MatchProgramHeader600) != MatchSavedNames634.ProgramBytes
            || sizeof(MatchFragmentHeader604) != MatchFragmentNames604.PrefixBytes)
            throw new PlatformNotSupportedException(MatchSavedNames634.Invalid);
        this.runtime = runtime;
        nuint bytes = checked((nuint)(MatchSavedNames634.MagicBytes + MatchSavedNames634.ProgramBytes)
            + MatchSavedNames634.TextWords * sizeof(ulong));
        void* allocation = NativeMemory.AllocZeroed(bytes);
        if (allocation == null)
            throw new OutOfMemoryException();
        Interlocked.Increment(ref nativeAllocations);
        MatchFragmentTransfer634 transferred;
        try
        {
            // No fallible allocation follows this one-way transfer. Constructor
            // rollback before transfer leaves source responsible for its bytes.
            transferred = source.Transfer634(runtime);
        }
        catch
        {
            NativeMemory.Free(allocation);
            Interlocked.Decrement(ref nativeAllocations);
            throw;
        }
        fragmentWords = transferred.Words;
        // Pinned do_emit_constant keeps immediates directly in text, with no
        // term_save fragment. Release the empty construction fragment here.
        if (fragmentWords == 0)
        {
            NativeMemory.Free((void*)transferred.Address);
            fragment = 0;
        }
        else
        {
            fragment = transferred.Address;
        }
        memory = (nint)allocation;
        var binary = (NativeResourceMagicHeader527*)allocation;
        binary->Flags = NativeResourceMagic527.Magic;
        binary->ApparentSize = bytes - MatchSavedNames634.MagicBytes;
        binary->OriginalSize = bytes - (nuint)NativeResourceMagic527.BinaryBytesOffset527;
        binary->Allocator = MatchSavedNames634.Allocator;
        binary->Destructor = Destructor634;
        var program = (MatchProgramHeader600*)(memory + MatchSavedNames634.MagicBytes);
        program->TermSave = fragment;
        program->SavedProgram = MatchFragmentNames604.Nil;
        program->HeapSize = sizeof(ulong);
        ulong* text = (ulong*)((byte*)program + MatchSavedNames634.ProgramBytes);
        text[0] = (ulong)MatchOpcode600.matchCatch;
        text[1] = (ulong)MatchOpcode600.matchPushC;
        text[MatchSavedNames634.RootOperand] = transferred.Root;
        text[3] = (ulong)MatchOpcode600.matchReturn;
        text[4] = (ulong)MatchOpcode600.matchHalt;
    }

    private static nint Destructor634 => (nint)(delegate* unmanaged[Cdecl]<nint, int>)&Destroy634;

    internal static int NativeAllocationCount634 => Volatile.Read(ref nativeAllocations);

    internal bool OwnedBy634(ProcessRuntime expected) => ReferenceEquals(runtime, expected);

    internal bool PhysicalFreed634 => Volatile.Read(ref physicallyFreed) != 0;

    internal MatchSavedReader634 Acquire634() => runtime.ReadSavedMatch634(this);

    internal MatchSavedReader634 AcquireRegistered634()
    {
        var reader = new MatchSavedReader634(this);
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(closing || memory == 0, MatchSavedNames634.Closed);
            readers = checked(readers + 1);
        }

        return reader;
    }

    internal Term CopyResult634(MatchSavedReader634 reader, ProcessRuntime expected, CancellationToken cancellation)
    {
        if (!ReferenceEquals(expected, runtime))
            throw new ArgumentException(MatchSavedNames634.Foreign, nameof(expected));
        if (!reader.Owns634(this))
            throw new ObjectDisposedException(MatchSavedNames634.Closed);
        // The selected reader's private gate serializes disposal with this copy.
        cancellation.ThrowIfCancellationRequested();
        Validate634(memory);
        ulong* text = (ulong*)(memory + MatchSavedNames634.MagicBytes + MatchSavedNames634.ProgramBytes);
        if (text[0] != (ulong)MatchOpcode600.matchCatch || text[1] != (ulong)MatchOpcode600.matchPushC
            || text[3] != (ulong)MatchOpcode600.matchReturn || text[4] != (ulong)MatchOpcode600.matchHalt)
            throw new InvalidOperationException(MatchSavedNames634.Invalid);

        // A complete detached result is created BEFORE releasing the program
        // reader. This domain has no offheap/function/resource result pointers.
        return MatchFragmentReader604.Decode604(
            runtime,
            fragment,
            fragmentWords,
            text[MatchSavedNames634.RootOperand],
            cancellation
        );
    }

    internal nint TextAddress634(MatchSavedReader634 reader)
    {
        if (!reader.Owns634(this))
            throw new ObjectDisposedException(MatchSavedNames634.Closed);

        return memory + MatchSavedNames634.MagicBytes + MatchSavedNames634.ProgramBytes;
    }

    internal nint FragmentAddress634(MatchSavedReader634 reader)
    {
        if (!reader.Owns634(this))
            throw new ObjectDisposedException(MatchSavedNames634.Closed);

        return fragment;
    }

    private void Validate634(nint address)
    {
        if (address == 0)
            throw new ObjectDisposedException(MatchSavedNames634.Closed);
        var binary = (NativeResourceMagicHeader527*)address;
        var program = (MatchProgramHeader600*)(address + MatchSavedNames634.MagicBytes);
        nuint apparent = MatchSavedNames634.ProgramBytes + MatchSavedNames634.TextWords * sizeof(ulong);
        if (binary->Flags != NativeResourceMagic527.Magic || binary->ApparentSize != apparent
            || binary->OriginalSize != apparent + (nuint)MatchSavedNames634.MagicBytes - (nuint)NativeResourceMagic527.BinaryBytesOffset527
            || binary->Allocator != MatchSavedNames634.Allocator || binary->Destructor != Destructor634
            || program->TermSave != fragment || program->Bindings != 0 || program->SavedProgramBuffer != 0
            || program->SavedProgram != MatchFragmentNames604.Nil || program->TraceSession != 0
            || program->StackOffset != 0 || program->HeapSize != sizeof(ulong))
            throw new InvalidOperationException(MatchSavedNames634.Invalid);
        // Walk the actual native term_save chain, proving each extent before a
        // dereference. Non-immediates own one node; immediates own none. Unknown
        // next pointers/cycles or offheap entries are rejected, never followed.
        nint current = program->TermSave;
        int visited = 0;
        while (current != 0)
        {
            if (current != fragment || visited != 0)
                throw new InvalidOperationException(MatchSavedNames634.Invalid);
            var saved = (MatchFragmentHeader604*)current;
            if (saved->OffHeapFirst != 0 || saved->OffHeapOverhead != 0
                || saved->AllocatedWords != (nuint)fragmentWords || saved->UsedWords != (nuint)fragmentWords)
                throw new InvalidOperationException(MatchSavedNames634.Invalid);
            current = saved->Next;
            visited++;
        }
        if (visited != (fragment == 0 ? 0 : 1))
            throw new InvalidOperationException(MatchSavedNames634.Invalid);
    }

    internal Task Close634()
    {
        nint release;
        lock (gate)
        {
            closing = true;
            release = Detach634();
        }
        Release634(release);

        return retired.Task;
    }

    internal void ReleaseReader634()
    {
        nint release;
        lock (gate)
        {
            if (readers <= 0)
                throw new InvalidOperationException(MatchSavedNames634.Invalid);
            readers--;
            release = Detach634();
        }
        Release634(release);
    }

    private nint Detach634()
    {
        if (!closing || readers != 0 || memory == 0)
            return 0;
        nint release = memory;
        memory = 0;

        return release;
    }

    private void Release634(nint release)
    {
        if (release == 0)
            return;
        Exception? failure = null;
        bool fragmentFreed = false;
        try
        {
            Validate634(release);
            // Exact known entry point only, after extent/pointer validation.
            var destructor = (delegate* unmanaged[Cdecl]<nint, int>)Destructor634;
            if (destructor(release) != 1)
                throw new InvalidOperationException(MatchSavedNames634.Invalid);
            fragmentFreed = true;
        }
        catch (Exception error)
        {
            failure = error;
        }
        finally
        {
            // Corruption refusal cannot leak the actual known owned allocation.
            // It never frees the modified pointer supplied by a native header.
            if (!fragmentFreed && fragment != 0)
                NativeMemory.Free((void*)fragment);
            NativeMemory.Free((void*)release);
            Interlocked.Decrement(ref nativeAllocations);
            Volatile.Write(ref physicallyFreed, 1);
            try
            {
                runtime.ForgetSavedMatch634(this, failure);
            }
            catch (Exception cleanup)
            {
                failure = failure is null ? cleanup : new AggregateException(failure, cleanup);
            }
        }
        if (failure is null)
        {
            retired.TrySetResult();
        }
        else
        {
            retired.TrySetException(failure);
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int Destroy634(nint binary)
    {
        // Sole internal entry calls only after known-allocation validation and
        // reader quiescence. No managed exception crosses this native boundary.
        var program = (MatchProgramHeader600*)(binary + MatchSavedNames634.MagicBytes);
        var saved = (MatchFragmentHeader604*)program->TermSave;
        if (saved == null)
            return 1;
        if (saved->Next != 0 || saved->OffHeapFirst != 0 || saved->OffHeapOverhead != 0)
            return 0;
        NativeMemory.Free(saved);
        program->TermSave = 0;

        return 1;
    }
}
