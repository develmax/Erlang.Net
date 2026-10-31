namespace Erlang.NifNativeBackend;

public static class MailboxPortableInputChecks
{
    public static async Task PhysicalOccurrences()
    {
        foreach (bool owned in new[] { false, true })
        {
            await using var runtime = NativeOwned516Profile.Create(owned);
            await CpuStatisticsAccess.Actor<Term>(
                runtime,
                async context =>
            {
                Term list = Nil.Value;
                for (int index = 0; index < MailboxPortableInputCaseNames.ListLength; index++)
                    list = new Cons(Term.I(index), list);
                Term source = Term.Tuple(
                    list,
                    list,
                    new BitString([1]),
                    new MapTerm([])
                );
                Require(MailboxPortableInput.CanReuse(source));
                Require(!MailboxPortableInput.CanReuse(context.Self));
                Require(!MailboxPortableInput.CanReuse(new BitString(new byte[MailboxPortableInputCaseNames.ReferenceBinaryLength])));
                var record = new NativeRecordTerm(
                    Term.A(NativeRecordHeapCaseNames.Module),
                    Term.A(NativeRecordHeapCaseNames.Record),
                    false,
                    []
                );
                Require(!MailboxPortableInput.CanReuse(record));
                await using var prepared = await runtime.MailboxRoots593.PrepareMailboxMessageAsync(
                    runtime,
                    context,
                    ProcessMessageQueueDataMode411.OnHeap,
                    source
                );
                var value = (TupleTerm)prepared.Value;
                Require(value.Equals(source) && !ReferenceEquals(value, source)
                    && ReferenceEquals(prepared.Graph.ObservationValue, source));
                Require(ReferenceEquals(MailboxBackingProbe.Storage(value)!.Owner, context.TermHeap506));
                Require(!ReferenceEquals(value.Items[0], value.Items[1])
                    && !ReferenceEquals(value.Items[0], list));
                Require(!MailboxPortableInput.CanReuse(value));
                Require(Cons.Items(value.Items[0]).Count() == MailboxPortableInputCaseNames.ListLength
                    && Cons.Items(value.Items[1]).Count() == MailboxPortableInputCaseNames.ListLength);

                return Nil.Value;
            }
            );
        }
    }

    private static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(MailboxPortableInputCaseNames.Failure);
    }
}
