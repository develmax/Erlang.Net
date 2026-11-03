using System.Runtime.InteropServices;

namespace Erlang;

internal sealed unsafe class OrdinaryBinaryMutation : IDisposable
{
    internal OrdinaryBinaryBacking Owner { get; }
    internal nint Expected { get; }
    internal long Generation { get; }
    internal int Capacity { get; }
    internal nuint Flags { get; }
    internal nuint Apparent { get; }
    internal byte[]? Complete { get; }
    private nint replacement;
    internal nint Replacement => replacement;
    internal nuint[] PointerCell { get; }

    internal OrdinaryBinaryMutation(
        OrdinaryBinaryBacking owner,
        nint expected,
        long generation,
        int capacity,
        nuint flags,
        nuint apparent,
        byte[]? complete,
        nint replacement
    )
    {
        Owner = owner;
        Expected = expected;
        Generation = generation;
        Capacity = capacity;
        Flags = flags;
        Apparent = apparent;
        Complete = complete;
        PointerCell = [(nuint)(replacement != 0 ? replacement : expected)];
        this.replacement = replacement;
    }

    internal nint TakeReplacement() => Interlocked.Exchange(ref replacement, 0);

    public void Dispose()
    {
        nint value = Interlocked.Exchange(ref replacement, 0);
        if (value != 0) NativeMemory.Free((void*)value);
    }
}
