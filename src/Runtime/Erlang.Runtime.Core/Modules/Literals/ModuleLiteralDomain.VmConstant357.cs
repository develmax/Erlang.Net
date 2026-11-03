namespace Erlang;

internal sealed partial class ModuleLiteralDomain800
{
    private VmConstantPool357? vmConstantOwner357;

    // Distinct actual producer/lifetime authority; this never admits source code.
    internal void BindVmConstant357(VmConstantPool357 owner)
    {
        lock (gate)
        {
            if (!ReferenceEquals(owner.Runtime357, runtime) || !runtime.OwnsVmConstantPool357(owner)
                || !sealed800 || !codeOwnerOpen || retiring || image is not null || vmConstantOwner357 is not null)
                throw new InvalidOperationException(ModuleLiteral800Names.Invalid);
            vmConstantOwner357 = owner;
        }
    }

    internal ModuleLiteralLease800 AcquireForVmConstant357(VmConstantPool357 owner)
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(!codeOwnerOpen || retiring, this);
            if (!sealed800 || !ReferenceEquals(vmConstantOwner357, owner)
                || !runtime.OwnsVmConstantPool357(owner))
                throw new InvalidOperationException(ModuleLiteral800Names.Invalid);

            return AddHolder800();
        }
    }
}
