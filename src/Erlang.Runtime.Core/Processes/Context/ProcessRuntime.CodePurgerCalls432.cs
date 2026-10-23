namespace Erlang;

public sealed partial class ProcessRuntime
{
    internal async ValueTask<Term> CodePurge432(ProcessContext context, string module, string operation)
    {
        bool prepare = operation is CodePurgerNames432.Prepare or CodePurgerNames432.PrepareOnLoad;
        if (!prepare && operation is not (CodePurgerNames432.Abort or CodePurgerNames432.Complete))
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        CodePurgerOperation432 invocation = new();
        SourcePurgeFenceSession? session;
        lock (gate)
        {
            RequireCodePurger432(context);
            if (sourceCodePurgerOperation432 is not null || prepare && sourceCodePurgerSession432 is not null
                || !prepare && (sourceCodePurgerSession432 is null || sourceCodePurgerSession432.Module != module))
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            session = sourceCodePurgerSession432;
            if (session?.OnLoad432 is not null && operation == CodePurgerNames432.Abort)
                throw new InvalidOperationException(CodePurgerNames432.OnLoadAbort);
            sourceCodePurgerOperation432 = invocation;
            if (!prepare) sourceCodePurgerSession432 = null;
        }
        Exception? failure = null;
        try
        {
            if (prepare)
            {
                session = await Modules.PreparePurgeAsync432(context, module, operation == CodePurgerNames432.PrepareOnLoad).ConfigureAwait(false);
                bool admitted;
                bool prepared = session is not null;
                lock (gate)
                {
                    admitted = ReferenceEquals(sourceCodePurger432, context) && !context.Terminated && !codePurgerClosed432;
                    if (admitted)
                    {
                        sourceCodePurgerSession432 = session;
                        session = null;
                    }
                }
                if (!admitted) context.Cancellation.ThrowIfCancellationRequested();
                if (!admitted) throw new InvalidOperationException(CodePurgerNames432.Invariant);

                return CoreModules.Boolean(prepared);
            }
            bool result = await Modules.FinishPurgeAsync(session!, operation == CodePurgerNames432.Complete).ConfigureAwait(false);
            session = null;

            return CoreModules.Boolean(result);
        }
        catch (Exception error)
        {
            failure = error;
            throw;
        }
        finally
        {
            Exception? cleanupFailure = null;
            if (session is not null)
            {
                try
                {
                    await Modules.FinishPurgeAsync(session, session.OnLoad432 is not null).ConfigureAwait(false);
                }
                catch (Exception cleanup)
                {
                    cleanupFailure = cleanup;
                }
            }
            lock (gate)
                if (ReferenceEquals(sourceCodePurgerOperation432, invocation)) sourceCodePurgerOperation432 = null;
            if (cleanupFailure is null) invocation.Settled.TrySetResult(); else invocation.Settled.TrySetException(cleanupFailure);
            if (cleanupFailure is not null)
            {
                if (failure is not null) throw new AggregateException(failure, cleanupFailure);
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(cleanupFailure).Throw();
            }
        }
    }
}
