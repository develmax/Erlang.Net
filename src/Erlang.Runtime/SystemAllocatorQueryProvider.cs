namespace Erlang;

internal static class SystemAllocatorQueryProvider
{
    internal static AllocatorQueryProvider Create(ProcessRuntime runtime, AllocatorQueryKind kind)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        if (kind is not AllocatorQueryKind.Full and not AllocatorQueryKind.Sizes)
            throw new ArgumentOutOfRangeException(nameof(kind), SystemAllocatorQueryDiagnostics.InvalidQueryKind);

        return new(
            AllocatorBackendCatalog.RecognizedNames,
            SystemAllocatorQueryNames.ActualProducerReplies,
            (
                context,
                _,
                allocators,
                post,
                cancellation
            ) => Produce(
                runtime,
                kind,
                context,
                allocators,
                post,
                cancellation
            ),
            CancellationToken.None
        );
    }

    private static Task Produce(
        ProcessRuntime runtime,
        AllocatorQueryKind kind,
        ProcessContext context,
        Term allocators,
        Action<Term, Term> post,
        CancellationToken cancellation
    )
    {
        if (!ReferenceEquals(context.Runtime, runtime))
            throw new InvalidOperationException(SystemAllocatorQueryDiagnostics.DifferentRuntime);
        cancellation.ThrowIfCancellationRequested();
        // Gather validates the entire request first. Never turn a recognized but
        // unimplemented allocator into disabled=false or an empty stats record.
        foreach (Atom allocator in Cons.Items(allocators))
        {
            if (AllocatorBackendCatalog.Capability(allocator.Name) != AllocatorBackendCapability.Supported)
                throw new ErlangException(Term.A(SystemAllocatorQueryNames.NotSupported));
        }
        Term information = kind == AllocatorQueryKind.Sizes ? Nil.Value : Term.List(Term.Tuple(
            Term.A(SystemAllocatorQueryNames.Options),
            Term.List(
                Term.Tuple(Term.A(SystemAllocatorQueryNames.Enabled), Term.A(ErlangBooleanAtoms.True)),
                Term.Tuple(Term.A(SystemAllocatorQueryNames.Mechanism), Term.A(SystemAllocatorQueryNames.NativeMemory))
            )
        ));
        cancellation.ThrowIfCancellationRequested();
        post(
            Nil.Value,
            Cons.From(Cons.Items(allocators).Select(allocator => Term.Tuple(
            allocator,
            Term.I(SystemAllocatorQueryNames.SystemInstance),
            information
        )))
        );

        return Task.CompletedTask;
    }
}
