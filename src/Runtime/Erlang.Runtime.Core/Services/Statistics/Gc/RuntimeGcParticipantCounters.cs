namespace Erlang;

// Native counters wrap per participant. The Erlang gather helper sums each
// sampled subtotal with arbitrary precision, rather than wrapping one VM total.
internal sealed class RuntimeGcParticipantCounters
{
    private readonly Counter[] normal;
    private readonly Counter dirty = new();

    internal RuntimeGcParticipantCounters(int normalSchedulers)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(normalSchedulers);
        normal = new Counter[normalSchedulers];
        for (int index = 0; index < normal.Length; index++)
            normal[index] = new();
    }

    internal int NormalSchedulers => normal.Length;

    internal void RecordNormal(int schedulerIdentity, long reclaimedWords)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(reclaimedWords);
        Normal(schedulerIdentity).Record(reclaimedWords);
    }

    internal void RecordDirty(long reclaimedWords)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(reclaimedWords);
        dirty.Record(reclaimedWords);
    }

    internal RuntimeGcParticipantSample SampleNormal(int schedulerIdentity, bool includeDirty)
    {
        Counter own = Normal(schedulerIdentity);
        lock (own.Gate)
        {
            ulong collections = own.Collections;
            ulong words = own.ReclaimedWords;
            if (includeDirty)
                lock (dirty.Gate)
                {
                    collections = unchecked(collections + dirty.Collections);
                    words = unchecked(words + dirty.ReclaimedWords);
                }

            return new(collections, words);
        }
    }

    private Counter Normal(int schedulerIdentity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(schedulerIdentity, ProcessSchedulerNames.First);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(schedulerIdentity, normal.Length);

        return normal[schedulerIdentity - ProcessSchedulerNames.First];
    }

    private sealed class Counter
    {
        internal object Gate { get; } = new();
        internal ulong Collections;
        internal ulong ReclaimedWords;

        internal void Record(long words)
        {
            lock (Gate)
            {
                Collections = unchecked(Collections + 1);
                ReclaimedWords = unchecked(ReclaimedWords + (ulong)words);
            }
        }
    }
}
