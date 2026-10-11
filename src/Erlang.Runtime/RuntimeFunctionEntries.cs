namespace Erlang;

public sealed class RuntimeFunctionEntries
{
    private readonly object gate = new();
    private readonly Dictionary<(string Module, int Index, int Arity, string Unique), Entry> entries = new();
    private readonly Dictionary<ExternalFunctionIdentity, FunctionTerm> externalEntries = new();

    public FunctionTerm External(ProcessRuntime runtime, ExternalFunctionIdentity identity, bool safe = false)
    {
        lock (gate)
        {
            if (externalEntries.TryGetValue(identity, out FunctionTerm? previous))
                return previous;
            if (safe && !runtime.Modules.HasExport(identity.Module.Name, identity.Function.Name, identity.Arity))
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            var function = new FunctionTerm(
                identity,
                (context, arguments) => ((ProcessContext)context).Runtime.Modules.Call(
                    (ProcessContext)context,
                    identity.Module.Name,
                    identity.Function.Name,
                    arguments.ToArray()
                )
            );
            externalEntries.Add(identity, function);

            return function;
        }
    }

    public LocalFunctionIdentity Register(
        LocalFunctionIdentity identity,
        int arity,
        int environmentCount,
        Func<IReadOnlyList<Term>, FunctionTerm> factory
    )
    {
        lock (gate)
        {
            var key = Key(identity, arity);
            if (entries.TryGetValue(key, out Entry? previous))
                identity = identity with { OldUnique = previous.Identity.OldUnique, LegacyIndex = previous.Identity.OldIndex };
            entries[key] = new Entry(identity, environmentCount, factory);

            return identity;
        }
    }

    public FunctionTerm Restore(
        ProcessRuntime runtime,
        LocalFunctionIdentity identity,
        int arity,
        IReadOnlyList<Term> environment
    )
    {
        var values = Array.AsReadOnly(environment.ToArray());
        Entry entry;
        lock (gate)
        {
            var key = Key(identity, arity);
            if (!entries.TryGetValue(key, out entry!))
                entries[key] = entry = new Entry(identity, null, null);
        }
        if (entry.Factory is not null && entry.EnvironmentCount == values.Count)
            return entry.Factory(values);
        FunctionTerm? unresolved = null;
        unresolved = new FunctionTerm(
            () => Find(identity, arity).Identity,
            arity,
            values,
            (context, arguments) =>
            {
                Entry loaded = Find(identity, arity);
                if (loaded.Factory is not null && loaded.EnvironmentCount == values.Count)
                    return loaded.Factory(values).Invoke(context, arguments);
                if (loaded.Factory is not null || runtime.Modules.Exports.Any(export => export.Module == identity.Module.Name))
                    throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.BadFunction), unresolved!));

                throw new ErlangException(ErlangErrorReasons.UndefinedFunction);
            }
        );

        return unresolved;
    }

    private Entry Find(LocalFunctionIdentity identity, int arity)
    {
        lock (gate)
            return entries[Key(identity, arity)];
    }

    private static (string Module, int Index, int Arity, string Unique) Key(LocalFunctionIdentity identity, int arity) => (identity.Module.Name, identity.Index, arity, Convert.ToHexString(identity.Unique.ToArray()));

    private sealed record Entry(LocalFunctionIdentity Identity, int? EnvironmentCount, Func<IReadOnlyList<Term>, FunctionTerm>? Factory);
}
