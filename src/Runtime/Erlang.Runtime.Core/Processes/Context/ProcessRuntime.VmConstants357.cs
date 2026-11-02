namespace Erlang;

public sealed partial class ProcessRuntime
{
    private VmConstantPool357? vmConstants357;

    private VmConstantPool357 VmConstants357
        => LazyInitializer.EnsureInitialized(ref vmConstants357, () => new VmConstantPool357(this));

    private static bool CanReadVmConstant357(ProcessContext context)
        => context.CurrentOwned506 is
        {
            IsDisposed: false, CanBorrow507: true,
            BindingKind507: OwnedBindingKind507.LocalValueBif354
        } invocation
            && invocation.OwnsContext507(context) && RuntimeActorExecutionTurn.IsOwnedBy(context);

    internal Term OwnAtomBinaryConstant357(ProcessContext context, IReadOnlyList<Term> arguments, Term result)
    {
        if (CanReadVmConstant357(context) && arguments[0] is Atom atom && result is BitString binary
            && AtomUtf8BinaryCache.IsCached357(atom, binary))
            return VmConstants357.AtomBinary(context, atom, binary);

        return result;
    }

    internal MapTerm ErrorHelperInfo357(ProcessContext context)
        => CanReadVmConstant357(context) ? VmConstants357.ErrorInfo(context) : ErlangErrorHelperMetadata.Info;

    internal bool OwnsVmConstantPool357(VmConstantPool357 pool)
        => ReferenceEquals(Volatile.Read(ref vmConstants357), pool);

    internal Task[] CloseVmConstants357() => Volatile.Read(ref vmConstants357)?.Close() ?? [];
}
