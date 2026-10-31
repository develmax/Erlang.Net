namespace Erlang;

public static partial class ErlangOperatorValues
{
    internal static async ValueTask<bool> TrySendCompiler593Async(ProcessContext context, Term target, Term message)
    {
        if (await context.Runtime.TrySendSelfOnHeapMailboxAsync(context, target, message).ConfigureAwait(false))
            return true;
        var source = context.CurrentOwned506;
        if (!context.OwnedExecutionEnabled683 || source is not { IsDisposed: false, CanBorrow507: true }
            || source.BindingKind507 is not (OwnedBindingKind507.CompilerGuest or OwnedBindingKind507.PreparedGuest)
            || !source.OwnsContext507(context) || !RuntimeActorExecutionTurn.IsOwnedBy(context)
            || !context.Runtime.CanBeginMessageSnapshot593(context, target))
            return await TrySendAsync(context, target, message);

        PreparedSendArguments593 outgoing = PrepareInternalSend593(
            context,
            source.Frame,
            target,
            message
        );

        return await TrySendPrepared593Async(context, outgoing).ConfigureAwait(false);
    }

    internal static async ValueTask<bool> TrySendBeam593Async(
        ProcessContext context,
        BeamRegisterState registers,
        Term target,
        Term message
    )
    {
        if (await context.Runtime.TrySendSelfOnHeapMailboxAsync(context, target, message).ConfigureAwait(false))
            return true;
        if (!registers.HasOwnedRoots507 || !RuntimeActorExecutionTurn.IsOwnedBy(context)
            || !context.Runtime.CanBeginMessageSnapshot593(context, target))
            return await TrySendLegacyBeam593Async(
                context,
                registers,
                target,
                message
            );

        PreparedSendArguments593 outgoing = registers.PrepareSend593(target, message);

        return await TrySendPreparedCore593Async(context, outgoing, registers).ConfigureAwait(false);
    }

    private static ValueTask<bool> TrySendLegacyBeam593Async(
        ProcessContext context,
        BeamRegisterState registers,
        Term target,
        Term message
    )
        => TrySendAsync(context, registers.Export507(target), registers.Export507(message));

    internal static PreparedSendArguments593 PrepareInternalSend593(
        ProcessContext context,
        ProcessTermFrame506 caller,
        Term target,
        Term message
    )
    {
        var borrowed = context.TermHeap506.Borrow(caller, Term.Tuple(target, message));
        PreparedTermTransfer506 prepared = context.TermHeap506.PrepareBorrowAtomics586(borrowed, exportHostAtomics586: false);
        PreparedSendArguments593? outgoing = null;
        try
        {
            outgoing = new(prepared);
            outgoing.SetOriginal593(target, message);
            outgoing.AttachPublication593(MailboxPublicationScope593.Capture(context.Runtime, prepared.Portable));

            return outgoing;
        }
        catch (Exception original)
        {
            try
            {
                if (outgoing is not null) outgoing.Dispose(); else prepared.Dispose();
            }
            catch (Exception cleanup)
            {
                throw new AggregateException(original, cleanup);
            }
            throw;
        }
    }

    internal static ValueTask<bool> TrySendPrepared593Async(ProcessContext context, PreparedSendArguments593 outgoing)
        => TrySendPreparedCore593Async(context, outgoing, null);

    private static async ValueTask<bool> TrySendPreparedCore593Async(ProcessContext context, PreparedSendArguments593 outgoing, BeamRegisterState? legacyBeam)
    {
        bool sent;
        Exception? primary = null;
        try
        {
            sent = await context.Runtime.TrySendBackedLocalAsync(
                outgoing.Target,
                outgoing.Message,
                context,
                recordSavedCall: true
            ).ConfigureAwait(false);
            if (!sent) sent = context.Runtime.TrySendMessageSnapshot593(context, outgoing);
        }
        catch (Exception error)
        {
            primary = error;
            throw;
        }
        finally
        {
            try
            {
                await outgoing.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception cleanup) when (primary is not null)
            {
                throw new AggregateException(primary, cleanup);
            }
        }

        // The final under-R check may observe a changed trace/target policy.
        // No new output happened; use ORIGINAL operands on the old whole path.
        if (sent) return true;

        return legacyBeam is null
            ? await TrySendAsync(context, outgoing.OriginalTarget593, outgoing.OriginalMessage593).ConfigureAwait(false)
            : await TrySendLegacyBeam593Async(
                context,
                legacyBeam,
                outgoing.OriginalTarget593,
                outgoing.OriginalMessage593
            ).ConfigureAwait(false);
    }
}
