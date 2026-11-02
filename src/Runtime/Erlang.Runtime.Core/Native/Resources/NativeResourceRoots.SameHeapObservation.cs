namespace Erlang;

internal static partial class NativeResourceRoots524
{
    // Ordinary binary bytes in the INTERNAL observation DTO are consumed copies.
    // The real SAME heap graph stays rooted by the actual queue frame; do not
    // raise its writable Binary refcount above the one genuine BinRef interest.
    internal static IDisposable? RetainSameHeapObservation(ProcessContext context, Term graph)
        => RetainGraphBeforeOrdinaryBinary(graph, context.Runtime);
}
