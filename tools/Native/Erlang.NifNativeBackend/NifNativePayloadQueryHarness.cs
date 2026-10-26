using System.Runtime.ExceptionServices;
using System.Text.Json;

namespace Erlang.NifNativeBackend;

internal static class NifNativePayloadQueryHarness
{
    internal static async Task Run(string[] args)
    {
        if (args.Length != NifNativePayloadQueryHarnessNames.LibraryArguments &&
            args.Length != NifNativePayloadQueryHarnessNames.BridgeArguments)
            throw new ArgumentException(NifNativePayloadQueryHarnessNames.Usage);
        var profile = new NifNativeCapabilityProfile(
            args[NifNativePayloadQueryHarnessNames.LibraryPathIndex],
            args[NifNativePayloadQueryHarnessNames.LibraryHashIndex],
            NifNativeNames.SourceSha256,
            args.Length == NifNativePayloadQueryHarnessNames.BridgeArguments ? args[NifNativePayloadQueryHarnessNames.BridgePathIndex] : null,
            args.Length == NifNativePayloadQueryHarnessNames.BridgeArguments ? args[NifNativePayloadQueryHarnessNames.BridgeHashIndex] : null
        );
        NifNativePayloadQueryPhase[] phases = Enum.GetValues<NifNativePayloadQueryPhase>();
        int observed = 0;
        ProcessContext? vetoContext = null;
        ProcessContext? loadedContext = null;
        NifNativeAllocationDomainSnapshot loaded = default;
        ExceptionDispatchInfo? primary = null;

        async Task Observe(
            ProcessContext context,
            NifNativePayloadQueryPhase phase,
            NifNativeAllocationDomainSnapshot expected
        )
        {
            if (primary is not null)
                return;
            try
            {
                Require(observed < phases.Length && phase == phases[observed]);
                if (phase == NifNativePayloadQueryPhase.VetoClosed)
                {
                    vetoContext = context;
                    Require(expected == default);
                }
                else if (phase == NifNativePayloadQueryPhase.Loaded)
                {
                    loadedContext = context;
                    Require(vetoContext is not null && !ReferenceEquals(vetoContext.Runtime, context.Runtime));
                    Require(expected.JoinedOwners == NifNativePayloadQueryHarnessNames.LoadedOwners && expected.Allocations.Count == NifNativePayloadQueryHarnessNames.LoadedAllocations &&
                        expected.Allocations.RequestedBytes > 0 && expected.Allocations.BackingRequestBytes >= expected.Allocations.RequestedBytes);
                    loaded = expected;
                }
                else
                {
                    Require(ReferenceEquals(loadedContext, context));
                    if (phase == NifNativePayloadQueryPhase.Unloaded)
                        Require(expected.JoinedOwners == 0 && expected.Allocations.Count == 0 &&
                            expected.Allocations.RequestedBytes == 0 && expected.Allocations.BackingRequestBytes == 0);
                    else
                        Require(expected == loaded);
                }
                // OwnershipChecks awaits this callback from its sole live actor body.
                // No other receive runs in that body while Gather owns its exact-ref receive.
                AllocatorQueryProvider provider = NativePayloadRequestProvider.Create(context.Runtime);
                Term actual = await AllocatorQueryMailbox.Gather(
                    context,
                    Term.A(NativePayloadRequestProviderNames.Identity),
                    provider
                );
                Term expectedTerm = Expected(expected.Allocations);
                Require(actual.Equals(expectedTerm));
                var instance = (TupleTerm)((Cons)actual).Head;
                TupleTerm[] payload = Cons.Items(instance.Items[NifNativePayloadQueryHarnessNames.InformationIndex]).Cast<TupleTerm>().ToArray();
                ulong requested = (ulong)((Integer)payload[NifNativePayloadQueryHarnessNames.RequestedIndex].Items[NifNativePayloadQueryHarnessNames.ValueIndex]).Value;
                ulong backing = (ulong)((Integer)payload[NifNativePayloadQueryHarnessNames.BackingIndex].Items[NifNativePayloadQueryHarnessNames.ValueIndex]).Value;
                Console.WriteLine(JsonSerializer.Serialize(new
                {
                    Role = NifNativePayloadQueryHarnessNames.QueryRole,
                    NifNativePayloadQueryHarnessNames.SchemaVersion,
                    Phase = phase.ToString(),
                    RuntimeOrdinal = phase == NifNativePayloadQueryPhase.VetoClosed ? NifNativePayloadQueryHarnessNames.VetoRuntime : NifNativePayloadQueryHarnessNames.LoadedRuntime,
                    Scope = NativePayloadRequestProviderNames.Identity,
                    SameLiveContextRuntime = ReferenceEquals(context.Runtime, phase == NifNativePayloadQueryPhase.VetoClosed ? vetoContext!.Runtime : loadedContext!.Runtime),
                    expected.JoinedOwners,
                    ExpectedRequestedBytes = expected.Allocations.RequestedBytes,
                    ExpectedBackingRequestBytes = expected.Allocations.BackingRequestBytes,
                    QueryResult = new Dictionary<string, ulong>(StringComparer.Ordinal)
                    {
                        [NativePayloadRequestProviderNames.RequestedBytes] = requested,
                        [NativePayloadRequestProviderNames.BackingRequestBytes] = backing
                    },
                    FullTermMatched = true,
                    ReviewedFixtureSourceSha256 = NifNativeNames.SourceSha256,
                    TrustedLibrarySha256 = profile.LibrarySha256,
                    TrustedBridgeSha256 = profile.BridgeSha256
                }));
                observed++;
            }
            catch (Exception failure)
            {
                // Gather has already joined its producer/receive. Retain the primary
                // fault while the ownership body completes admission release and unload.
                primary = ExceptionDispatchInfo.Capture(failure);
            }
        }

        try
        {
            await NifNativeOwnershipChecks.Run(profile, Observe);
        }
        catch (Exception cleanup)
        {
            if (primary is null)
                primary = ExceptionDispatchInfo.Capture(cleanup);
            else
                primary.SourceException.Data[NifNativePayloadQueryHarnessNames.CleanupFailure] = cleanup;
        }
        primary?.Throw();
        Require(observed == phases.Length);
        Console.WriteLine(JsonSerializer.Serialize(new
        {
            Role = NifNativePayloadQueryHarnessNames.CompletedRole,
            NifNativePayloadQueryHarnessNames.SchemaVersion,
            Phases = phases.Select(phase => phase.ToString()).ToArray(),
            Source = NifNativeNames.SourceSha256,
            TrustedLibrarySha256 = profile.LibrarySha256,
            TrustedBridgeSha256 = profile.BridgeSha256
        }));
    }

    private static Term Expected(NifNativeAllocationSnapshot allocations)
        => Term.List(Term.Tuple(
            Term.A(AllocatorQueryNames.Instance),
            Term.I(NifNativePayloadQueryHarnessNames.Instance),
            Term.List(
                Term.Tuple(Term.A(NativePayloadRequestProviderNames.RequestedBytes), new Integer(allocations.RequestedBytes)),
                Term.Tuple(Term.A(NativePayloadRequestProviderNames.BackingRequestBytes), new Integer(allocations.BackingRequestBytes))
            )
        ));

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(NifNativePayloadQueryHarnessNames.Failure);
    }
}
