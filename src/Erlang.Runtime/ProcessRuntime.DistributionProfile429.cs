namespace Erlang;

public sealed partial class ProcessRuntime
{
    private readonly TupleTerm distributionProfile429 = Term.Tuple(
        Term.A(InternalDistributionProfile429Names.Record),
        new Integer(DistributionCapabilityProfile429.Preferred),
        new Integer(DistributionCapabilityProfile429.Mandatory),
        new Integer(DistributionCapabilityProfile429.Addable),
        new Integer(DistributionCapabilityProfile429.Rejectable),
        new Integer(DistributionCapabilityProfile429.StrictOrder)
    );

    internal Term DistributionProfile429(ProcessContext caller)
    {
        RequireBeamfileActor418(caller);

        return distributionProfile429;
    }
}
