namespace Erlang;

internal sealed partial class ErlDriverAsyncServices420
{
    private readonly ErlDriverNativeAsyncAccounting1126? accounting1151;
    private readonly bool accountingConfigured1151;
    internal bool IsAccountingConfigured1151 => accountingConfigured1151;
    internal ErlDriverNativeAsyncAccounting1126? AccountingOwner1151 => accounting1151;
    private uint AsyncLanes1151 => accounting1151?.Count ?? pool!.Lanes;

    internal static ErlDriverAsyncServices420 CreateAccounted1151(
        Func<nint, ErlDriverPortReadLease420?> acquire,
        ErlDriverThreadBridge420 bridge,
        ErlDriverTermTokens420 tokens,
        uint asyncThreads,
        ErlDriverPortJobs420 jobs,
        ErlDriverPdlServices420 pdl,
        ErlDriverServiceFault420 fault
    ) => new(
        acquire,
        bridge,
        tokens,
        asyncThreads,
        jobs,
        pdl,
        fault,
        true
    );

    private void SubmitAsync1151(
        uint lane,
        ulong owner,
        ulong identity,
        nint invoke,
        nint data
    )
    {
        if (accounting1151 is null)
        {
            pool!.Submit(
                lane,
                owner,
                identity,
                invoke,
                data
            );

            return;
        }
        using var lease = accounting1151.Acquire();
        lease.Submit(
            lane,
            owner,
            identity,
            invoke,
            data
        );
    }
}
