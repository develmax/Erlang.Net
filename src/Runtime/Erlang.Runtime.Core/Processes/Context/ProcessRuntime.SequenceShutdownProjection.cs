namespace Erlang;

public sealed partial class ProcessRuntime
{
    // Earlier shutdown victims can remove an existing link. Every possible
    // surviving event already has real packet/output storage. Reindexing only
    // writes its bounded scalar serial fields; no new arbitrary leaf enters.
    private PreparedSequenceTermination? ProjectShutdownSequence(ProcessContext context)
    {
        PreparedSequenceTermination? original = ShutdownPlanFor(context);
        if (original is null)
        {
            if (NeedsSingleSequenceTerminationPreparation(context))
                throw new InvalidOperationException(SequenceTerminationNames.Unprepared);

            return null;
        }
        if (original.IsCurrentBeforeTermination()) return original;
        SequenceTerminationSnapshot source = original.Snapshot;
        if (!ReferenceEquals(context.SequentialTrace.Token, source.Token)
            || context.SequentialTrace.Clock != source.Clock || context.SequentialTrace.LastCount != source.LastCount)
            throw new InvalidOperationException(SequenceTerminationNames.Unprepared);
        Pid[] links = context.Links.ToArray(), origins = context.PendingLinkOrigins.Values.ToArray();
        int[] selected = SelectRows(source.Links, links, 0)
            .Concat(SelectRows(source.PendingOrigins, origins, source.Links.Length)).ToArray();

        return original.ProjectShutdown(links, origins, selected);

        static int[] SelectRows(Pid[] oldValues, Pid[] currentValues, int offset)
        {
            var result = new int[currentValues.Length];
            int next = 0;
            for (int index = 0; index < currentValues.Length; index++)
            {
                while (next < oldValues.Length && !ReferenceEquals(currentValues[index], oldValues[next])) next++;
                if (next == oldValues.Length) throw new InvalidOperationException(SequenceTerminationNames.Unprepared);
                result[index] = checked(offset + next++);
            }

            return result;
        }
    }
}
