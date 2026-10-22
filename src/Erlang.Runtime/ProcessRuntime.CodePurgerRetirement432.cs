namespace Erlang;

public sealed partial class ProcessRuntime
{
    // Called under runtime.gate. Registry operations run on the tracked default-scheduler owner task.
    private Task RetireCodePurger432()
    {
        SourcePurgeFenceSession? session = sourceCodePurgerSession432;
        CodePurgerOperation432? invocation = sourceCodePurgerOperation432;
        sourceCodePurger432 = null;
        sourceCodePurgerImage432 = null;
        sourceCodePurgerReservation432 = null;
        sourceCodePurgerSession432 = null;
        sourceCodePurgerOperation432 = null;
        if (session is null && invocation is null) return codePurgerRetirement432;
        Task abort = session is null ? Task.CompletedTask : RetireCodePurgerSession432(session);
        codePurgerRetirement432 = Task.WhenAll(codePurgerRetirement432, invocation?.Settled.Task ?? Task.CompletedTask, abort);

        return codePurgerRetirement432;
    }

    private Task RetireCodePurgerSession432(SourcePurgeFenceSession session)
    {
        try
        {
            return CodePurgerOwnerFlow432.Start(async () =>
            {
                // Rejected on_load has no source-level soft-abort path.
                await Modules.FinishPurgeAsync(session, session.OnLoad432 is not null).ConfigureAwait(false);
            });
        }
        catch (Exception failure)
        {
            // The registry still owns the actual session and its global close
            // will retire it. Never report this failed owner task as reclaimed.
            return Task.FromException(failure);
        }
    }

    private Task CodePurgerProcessRetirement432(ProcessContext context, Task previous)
    {
        Task own = context.Self.Equals(InitializationPid) ? CloseCodePurger432() :
            ReferenceEquals(sourceCodePurger432, context) ? RetireCodePurger432() : codePurgerRetirement432;

        return own.IsCompletedSuccessfully ? previous : Task.WhenAll(previous, own);
    }

    private Task CloseCodePurger432()
    {
        codePurgerClosed432 = true;

        return RetireCodePurger432();
    }

    private Task DrainCodePurger432()
    {
        lock (gate) return CloseCodePurger432();
    }

    private bool CodePurgerRetirementCompleted432 => codePurgerRetirement432.IsCompletedSuccessfully;
}
