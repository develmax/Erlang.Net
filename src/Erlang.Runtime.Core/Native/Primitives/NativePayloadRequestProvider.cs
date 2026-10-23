namespace Erlang;

internal static class NativePayloadRequestProvider
{
    internal static AllocatorQueryProvider Create(ProcessRuntime runtime)
    {
        ArgumentNullException.ThrowIfNull(runtime);

        return new(
            new HashSet<string>(StringComparer.Ordinal) { NativePayloadRequestProviderNames.Identity },
            NativePayloadRequestProviderParameters.CoherentCaptureReplyCount,
            (
                context,
                _,
                _,
                post,
                cancellation
            ) => Produce(
                runtime,
                context,
                post,
                cancellation
            ),
            CancellationToken.None
        );
    }

    private static async Task Produce(
        ProcessRuntime runtime,
        ProcessContext context,
        Action<Term, Term> post,
        CancellationToken cancellation
    )
    {
        if (!ReferenceEquals(context.Runtime, runtime))
            throw new InvalidOperationException(NativePayloadRequestProviderDiagnostics.DifferentRuntime);
        cancellation.ThrowIfCancellationRequested();
        NifNativeAllocationDomainSnapshot captured = await runtime.CaptureNativeAllocationsAsync(cancellation).ConfigureAwait(false);
        cancellation.ThrowIfCancellationRequested();
        Term payload = Term.List(
            Term.Tuple(Term.A(NativePayloadRequestProviderNames.RequestedBytes), new Integer(captured.Allocations.RequestedBytes)),
            Term.Tuple(Term.A(NativePayloadRequestProviderNames.BackingRequestBytes), new Integer(captured.Allocations.BackingRequestBytes))
        );
        post(
            Nil.Value,
            Term.List(Term.Tuple(
                Term.A(NativePayloadRequestProviderNames.Identity),
                Term.I(NativePayloadRequestProviderParameters.RuntimeAggregateInstance),
                payload
            ))
        );
    }
}
