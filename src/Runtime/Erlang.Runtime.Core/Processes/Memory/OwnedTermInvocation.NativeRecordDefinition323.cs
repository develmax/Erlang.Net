namespace Erlang;

internal sealed partial class OwnedTermInvocation506
{
    // Only the canonical records:get_definition body uses this gate for its
    // own fresh scaffold and already-admitted exact image default values.
    // Opaque argument export and all public callback binding rules stay intact.
    internal bool CanAdoptNativeRecordDefinition323(ProcessContext caller)
        => !IsDisposed && CanBorrow507 && BindingKind507 == OwnedBindingKind507.Opaque
            && !Frame.IsDisposed && ReferenceEquals(caller.CurrentOwned506, this) && OwnsContext507(caller)
            && parent is { IsDisposed: false, BindingKind507: OwnedBindingKind507.CompilerGuest or OwnedBindingKind507.PreparedGuest }
            && !parent.Frame.IsDisposed && parent.OwnsContext507(caller) && RuntimeActorExecutionTurn.IsOwnedBy(caller);
}
