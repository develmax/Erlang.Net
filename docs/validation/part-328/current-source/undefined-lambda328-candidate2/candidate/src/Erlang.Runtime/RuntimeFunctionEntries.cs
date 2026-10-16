namespace Erlang;

public sealed class RuntimeFunctionEntries
{
    private readonly object gate = new();
    private readonly Dictionary<(string Module, int Index, int Arity, string Unique), LocalFunctionPublicationCell> entries = new();
    private readonly Dictionary<ExternalFunctionIdentity, FunctionTerm> externalEntries = new();

    public bool IsExternalEntry(FunctionTerm function)
    {
        lock (gate)
            return function.ExternalIdentity is { } identity && externalEntries.TryGetValue(identity, out var entry) && ReferenceEquals(entry, function);
    }

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
    ) => Publish(
        identity,
        arity,
        environmentCount,
        (environment, _) => factory(environment),
        false
    );

    internal LocalFunctionIdentity RegisterSourceFactory(
        LocalFunctionIdentity identity,
        int arity,
        int environmentCount,
        Func<IReadOnlyList<Term>, LocalFunctionInvocation?, FunctionTerm> factory
    ) => Publish(
        identity,
        arity,
        environmentCount,
        factory,
        true
    );

    private LocalFunctionIdentity Publish(
        LocalFunctionIdentity identity,
        int arity,
        int environmentCount,
        Func<IReadOnlyList<Term>, LocalFunctionInvocation?, FunctionTerm> factory,
        bool sourceFactory
    )
    {
        lock (gate)
        {
            var key = Key(identity, arity);
            if (entries.TryGetValue(key, out var cell))
            {
                var previous = cell.Current;
                identity = identity with { OldUnique = previous.Identity.OldUnique, LegacyIndex = previous.Identity.OldIndex };
                cell.Publish(new(identity, environmentCount, factory, sourceFactory));
            }
            else
                entries.Add(key, new(new(identity, environmentCount, factory, sourceFactory)));

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
        LocalFunctionPublicationCell cell;
        lock (gate)
            cell = FindOrAdd(identity, arity);
        FunctionTerm? restored = null;
        restored = new FunctionTerm(
            () => cell.Current.Identity,
            arity,
            values,
            (context, arguments) => InvokeRestored(
                (ProcessContext)context,
                identity,
                arity,
                values,
                restored!,
                arguments
            )
        );

        return restored;
    }

    private static async ValueTask<Term> InvokeRestored(
        ProcessContext context,
        LocalFunctionIdentity identity,
        int arity,
        IReadOnlyList<Term> environment,
        FunctionTerm original,
        IReadOnlyList<Term> arguments
    )
    {
        while (true)
        {
            context.Runtime.ThrowIfHalted();
            var invocation = context.Runtime.Functions.Capture(identity, arity, original);
            var publication = invocation.Publication;
            try
            {
                if (publication.Factory is not null && publication.EnvironmentCount == environment.Count)
                {
                    if (!publication.IsSourceFactory && !context.Runtime.Modules.ValidateLocalFunctionPublication(invocation))
                        throw new LocalFunctionPublicationChangedException(invocation);
                    var temporary = publication.Factory(environment, invocation);

                    return await temporary.Invoke(context, arguments);
                }

                return await context.Runtime.Modules.InvokeUndefinedLambda(
                    context,
                    identity.Module.Name,
                    original,
                    arguments,
                    invocation,
                    publication.Factory is not null
                );
            }
            catch (LocalFunctionPublicationChangedException failure) when (ReferenceEquals(failure.Invocation, invocation))
            {
                await context.ReduceAsync();
            }
        }
    }

    internal LocalFunctionInvocation Capture(LocalFunctionIdentity identity, int arity, FunctionTerm original)
    {
        lock (gate)
        {
            var cell = FindOrAdd(identity, arity);

            return new(original, cell, cell.Current);
        }
    }

    private LocalFunctionPublicationCell FindOrAdd(LocalFunctionIdentity identity, int arity)
    {
        var key = Key(identity, arity);
        if (!entries.TryGetValue(key, out var cell))
        {
            cell = new(new(identity, null, null, false));
            entries.Add(key, cell);
        }

        return cell;
    }

    private static (string Module, int Index, int Arity, string Unique) Key(LocalFunctionIdentity identity, int arity) => (identity.Module.Name, identity.Index, arity, Convert.ToHexString(identity.Unique.ToArray()));
}
