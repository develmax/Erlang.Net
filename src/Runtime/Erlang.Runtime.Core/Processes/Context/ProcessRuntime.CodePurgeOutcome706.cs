namespace Erlang;

public sealed partial class ProcessRuntime
{
    internal async ValueTask<Term> PurgeModuleOutcome706(ProcessContext requester, string module)
    {
        var reference = MakeReference();
        Spawn(async worker =>
        {
            Term result;
            try
            {
                var admission = codePurger.WaitAsync(worker.Cancellation);
                try
                {
                    await worker.Mailbox.WaitForControlCompletion(admission, worker.Cancellation);
                    CodePurgeOutcome706 outcome = await PurgeOldCodeOutcome706(worker, module);
                    result = Term.Tuple(
                        Term.A(outcome.Purged ? ErlangBooleanAtoms.True : ErlangBooleanAtoms.False),
                        Term.A(outcome.DidKill ? ErlangBooleanAtoms.True : ErlangBooleanAtoms.False)
                    );
                }
                finally
                {
                    try
                    {
                        await admission;
                        codePurger.Release();
                    }
                    catch (OperationCanceledException) when (worker.Cancellation.IsCancellationRequested)
                    {
                    }
                }
            }
            catch (ProcessHaltTransfer transfer) when (ReferenceEquals(transfer.Runtime, this))
            {
                throw;
            }
            catch (ModuleSourceMutationClosedException closing) when (ReferenceEquals(closing.Registry, Modules))
            {
                throw;
            }
            catch (ErlangException exception)
            {
                result = Term.Tuple(Term.A(ErlangExceptionClasses.Error), exception.Reason);
            }
            catch (OperationCanceledException) when (worker.Cancellation.IsCancellationRequested)
            {
                result = Term.Tuple(Term.A(ErlangExceptionClasses.Error), Term.A(ErlangErrorReasons.NoProcess));
            }
            catch (Exception exception)
            {
                result = Term.Tuple(
                    Term.A(ErlangExceptionClasses.Error),
                    Term.Tuple(Term.A(ProcessExitReasons.ClrError), Term.String(exception.GetType().Name))
                );
            }
            Send(requester.Self, Term.Tuple(Term.A(CodePurgeNames.ReplyTag), reference, result));

            return Term.A(ErlangBooleanAtoms.True);
        });
        var reply = await requester.ReceiveAsync(message => message is TupleTerm { Items.Count: 3 } tuple &&
            tuple.Items[0] is Atom { Name: CodePurgeNames.ReplyTag } && tuple.Items[1].Equals(reference) ? tuple : null);
        if (reply!.Items[2] is TupleTerm { Items: [Atom { Name: ErlangExceptionClasses.Error }, Term reason] })
            throw new ErlangException(reason);

        return reply.Items[2];
    }

    private async ValueTask<CodePurgeOutcome706> PurgeOldCodeOutcome706(ProcessContext worker, string module)
    {
        SourcePurgeFenceSession? session = await Modules.PreparePurgeAsync(worker, module);
        if (session is null)
            return new(false, false);
        bool completed = false;
        try
        {
            bool didKill = await PurgeCodeImageProcessesOutcome706(worker, session.Image);
            bool purged = await Modules.FinishPurgeAsync(session, true);
            completed = true;

            return new(purged, didKill);
        }
        finally
        {
            if (!completed)
                await Modules.FinishPurgeAsync(session, false);
        }
    }

}
