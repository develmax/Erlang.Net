namespace Erlang;

// This capability can only create a NEW private heap. It cannot bless an
// existing process/module heap for unadmitted native descriptor publication.
internal sealed class ReadonlyTraceArena817
{
    internal ReadonlyTraceSource817 Source { get; }
    internal ProcessTermHeap506 Heap { get; }
    private ProcessTermFrame506? frame;
    internal ProcessTermFrame506 Frame => frame ?? throw new InvalidOperationException(ReadonlyTrace817Names.Invalid);
    internal List<OrdinaryBinaryLease> Leases { get; } = new();

    private ReadonlyTraceArena817(ReadonlyTraceSource817 source)
    {
        Source = source;
        Heap = new(source.Runtime);
    }

    internal static async ValueTask<ReadonlyTraceArena817> CreateAsync(ReadonlyTraceSource817 source)
    {
        source.Runtime.AssertSequenceTracePreparation();
        source.RequireValidated();
        var arena = new ReadonlyTraceArena817(source);
        try
        {
            arena.frame = arena.Heap.Enter();

            return arena;
        }
        catch (Exception original)
        {
            try
            {
                await arena.Heap.RetireAsync().ConfigureAwait(false);
            }
            catch (Exception cleanup)
            {
                throw new AggregateException(original, cleanup);
            }
            throw;
        }
    }
}
