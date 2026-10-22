namespace Erlang;

internal static class DistSpawn430Native
{
    internal static bool TryCapture(
        ProcessRuntime? owner,
        string module,
        string function,
        int arity,
        out ErlangFunction body
    )
    {
        body = null!;
        if (owner is null || module != DistSpawn430Names.Module || function != DistSpawn430Names.Function || arity != DistSpawn430Names.Arity) return false;
        body = async (c, a) =>
        {
            if (!ReferenceEquals(c.Runtime, owner) || a.Count != DistSpawn430Names.Arity) throw new InvalidOperationException(DistSpawn430Names.Foreign);
            owner.RequireInternalPortActor416(c);
            if (a[0] is not Atom node || node.Name == owner.Node || a[1] is not TupleTerm { Items: [Atom moduleName, Atom functionName, var arguments] }) return Term.A(DistSpawn430Names.BadArgument);
            Term tail = arguments;
            while (tail is Cons arg)
            {
                c.Cancellation.ThrowIfCancellationRequested();
                tail = arg.Tail;
            }
            if (tail is not Nil) return Term.A(DistSpawn430Names.BadArgument);
            bool request = a[3].Equals(Term.A(DistSpawn430Names.Request));
            var values = new List<Term>();
            tail = a[2];
            ProcessSpawnRequestOptions parsed = ProcessSpawnRequestOptions.Parse(values, true);
            while (tail is Cons option)
            {
                c.Cancellation.ThrowIfCancellationRequested();
                if (!request && option.Head is TupleTerm { Items: [Atom { Name: ProcessSpawnRequestNames.Reply or ProcessSpawnRequestNames.ReplyTag }, _] }) return Term.A(DistSpawn430Names.BadArgument);
                values.Add(option.Head);
                parsed = ProcessSpawnRequestOptions.Parse(values, true);
                tail = option.Tail;
                if (parsed.BadOptions) break;
            }
            if (parsed.BadOptions)
            {
                if (!request) return Term.A(DistSpawn430Names.BadArgument);

                return owner.FailNativeDistSpawn430(
                    c,
                    parsed,
                    DistSpawn430Names.BadOptions,
                    false,
                    true
                );
            }
            if (tail is not Nil) return Term.A(DistSpawn430Names.BadArgument);
            var description = SequenceSpawnDescription.Module(
                moduleName,
                functionName,
                arguments,
                a[2]
            );

            return await owner.DistributionPending.StartNativeDistSpawn430(
                c,
                node,
                description,
                parsed,
                request
            );
        };

        return true;
    }
}
