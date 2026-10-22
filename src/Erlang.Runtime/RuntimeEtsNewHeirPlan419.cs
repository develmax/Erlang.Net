namespace Erlang;

internal sealed record RuntimeEtsNewHeirPlan419(Term Options, Pid? Heir, Term? Gift)
{
    internal static RuntimeEtsNewHeirPlan419 Parse(ProcessRuntime runtime, Term options, CancellationToken cancellation)
    {
        var retained = new List<Term>();
        Pid? heir = null;
        Term? gift = null;
        Term rest = options;
        while (rest is Cons cell)
        {
            cancellation.ThrowIfCancellationRequested();
            if (cell.Head is TupleTerm pair && pair.Items.Count > 0 && pair.Items[0] is Atom { Name: EtsHeirNames419.Heir })
            {
                // Creation options are last-wins, unlike setopts duplicate refusal.
                if (pair.Items.Count == 2 && pair.Items[1] is Atom { Name: EtsHeirNames419.None })
                {
                    heir = null;
                    gift = null;
                }
                else if (pair.Items.Count is 2 or 3 && pair.Items[1] is Pid pid && runtime.IsEtsInternalPid419(pid))
                {
                    heir = pid;
                    gift = pair.Items.Count == 3 ? pair.Items[2] : null;
                }
                else throw new RuntimeEtsCoreFailure407(RuntimeEtsCoreCause407.None);
            }
            else retained.Add(cell.Head);
            rest = cell.Tail;
        }
        if (rest is not Nil) throw new RuntimeEtsCoreFailure407(RuntimeEtsCoreCause407.None);
        cancellation.ThrowIfCancellationRequested();

        return new(Term.List(retained.ToArray()), heir, gift);
    }
}
