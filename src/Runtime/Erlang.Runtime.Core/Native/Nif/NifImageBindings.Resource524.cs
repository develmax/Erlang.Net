namespace Erlang;

internal sealed partial class NifImageBindings
{
    internal NifResourceImagePin524 RetainResourceImage524(NifResourceImagePin524? authority = null)
    {
        // Allocate before admitting a new physical library reference.
        var pin = new NifResourceImagePin524(this);
        lock (gate)
        {
            bool inherited = authority is not null && authority.IsActiveFor524(this);
            if (!inherited && phase != NifImageBindingPhase.Ready)
                throw new InvalidOperationException(NativeResource524Names.ImageUnavailable);
            checked
            {
                activeCalls++;
            }
            pin.Admit524();
        }

        return pin;
    }
}
