namespace Erlang;

public sealed partial class ProcessRuntime
{
    internal Term ReadNativeCreation()
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(disposed || IsHaltRequested, this);
            uint creation = currentDistributionEntry!.Creation;

            return creation == 0 ? Term.A(CommonAtomNames.Undefined) : new Integer(creation);
        }
    }
}
