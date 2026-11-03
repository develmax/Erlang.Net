namespace Erlang;

internal sealed partial class OwnedTermInvocation506 : IDisposable
{
    private static readonly AsyncLocal<OwnedTermInvocation506?> active = new();
    private readonly OwnedTermInvocation506? previousActive;
    internal static OwnedTermInvocation506? Active => active.Value;

    internal static bool HasActiveFor506(ProcessRuntime runtime)
    {
        for (OwnedTermInvocation506? invocation = active.Value; invocation is not null; invocation = invocation.previousActive)
            if (!invocation.IsDisposed && ReferenceEquals(invocation.Runtime, runtime))
                return true;

        return false;
    }

    internal ProcessRuntime Runtime => context.Runtime;
    private readonly ProcessContext context;
    private readonly OwnedTermInvocation506? previous;
    private readonly OwnedTermInvocation506? parent;
    private readonly int destination;
    private readonly int resultSlot;
    private readonly int controlSlot;
    private readonly int[] argumentSlots;
    private readonly bool trampoline;
    private volatile bool disposed;
    internal bool IsDisposed => disposed;
    internal ProcessTermFrame506 Frame { get; }
    internal Term[] Arguments { get; }

    internal OwnedTermInvocation506(
        ProcessContext context,
        IReadOnlyList<Term> arguments,
        bool hostBoundary,
        bool trampoline = false
    )
    {
        this.context = context;
        previous = context.CurrentOwned506;
        previousActive = active.Value;
        parent = hostBoundary ? null : previous;
        if (parent is { IsDisposed: true })
            throw new ObjectDisposedException(nameof(OwnedTermInvocation506));
        this.trampoline = trampoline;
        CanBorrow507 = !hostBoundary && parent is not null && RuntimeActorExecutionTurn.IsOwnedBy(context);
        BindingKind507 = CanBorrow507 ? parent!.BindingKind507 : OwnedBindingKind507.Opaque;
        destination = parent is null ? -1 : parent.resultSlot;
        Frame = context.TermHeap506.Enter();
        try
        {
            resultSlot = Frame.Reserve();
            controlSlot = Frame.Reserve();
            argumentSlots = arguments.Select(_ => Frame.Reserve()).ToArray();
            Arguments = new Term[arguments.Count];
            for (int index = 0; index < arguments.Count; index++)
            {
                if (CanBorrow507)
                    Frame.SetLocalGraph507(argumentSlots[index], arguments[index]);
                else
                    Frame.Set(argumentSlots[index], arguments[index]);
                Arguments[index] = Frame.Read(argumentSlots[index]);
            }
            context.CurrentOwned506 = this;
            active.Value = this;
        }
        catch
        {
            Frame.Dispose();
            throw;
        }
    }

    internal Term Export(Term value)
    {
        Term portable = context.TermHeap506.Export(Frame, value);

        return NativeResourceRoots524.Contains524(portable)
            ? NativeTermHostScope524.AdmitCurrent524(Runtime, portable) : portable;
    }

    internal Term Complete(Term value)
    {
        if (parent is null)
            return Export(value);
        // Publish into the caller root before this frame can release its roots.
        parent.Frame.Set(destination, value);

        return parent.Frame.Read(destination);
    }

    internal Term Preserve(Term value) => parent is null ? Export(value)
        : CanBorrow507 ? parent.Frame.RetainLocalGraph507(value) : parent.Frame.Retain(value);

    internal ErlangException Transfer(ErlangException error) => error.WithTransferredTerms506(Preserve);

    internal Term HoldControl506(Term value)
    {
        if (CanBorrow507)
            Frame.SetLocalGraph507(controlSlot, value);
        else
            Frame.Set(controlSlot, value);

        return Frame.Read(controlSlot);
    }

    internal Exception TransferControl506(Exception error)
    {
        var carrier = (IOwnedControlTransfer506)error;
        if (parent is null)
            return carrier.TransferTerms506(Export);

        return carrier.TransferTerms506(parent.HoldControl506);
    }

    internal void AdmitContinuationInputs506()
    {
        // The child has already rooted all arguments and, for anonymous code,
        // every environment and constraint value. Only now release old inputs.
        var caller = parent;
        if (caller is null || !caller.trampoline)
            return;
        caller.Frame.Clear(caller.controlSlot);
        caller.Frame.Clear(caller.resultSlot);
        foreach (int slot in caller.argumentSlots)
            caller.Frame.Clear(slot);
    }

    public void Dispose()
    {
        if (disposed)
            return;
        disposed = true;
        context.CurrentOwned506 = previous;
        active.Value = previousActive;
        Frame.Dispose();
    }
}
