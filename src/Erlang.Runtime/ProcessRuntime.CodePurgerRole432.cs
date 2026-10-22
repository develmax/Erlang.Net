namespace Erlang;

public sealed partial class ProcessRuntime
{
    private ProcessContext? sourceCodePurger432;
    private PreparedBeamImage? sourceCodePurgerImage432;
    private object? sourceCodePurgerReservation432;
    private SourcePurgeFenceSession? sourceCodePurgerSession432;
    private CodePurgerOperation432? sourceCodePurgerOperation432;
    private Task codePurgerRetirement432 = Task.CompletedTask;
    private bool codePurgerClosed432;

    internal void RequireCodePurger432(ProcessContext context)
    {
        lock (gate)
        {
            if (!ReferenceEquals(context.Runtime, this)) throw new InvalidOperationException(CodePurgerNames432.Foreign);
            RequireEtsFoundationActor(context);
            context.Cancellation.ThrowIfCancellationRequested();
            if (!ReferenceEquals(sourceCodePurger432, context)) throw new ErlangException(CodePurgerNames432.NotSupported);
        }
    }

    internal bool TrySpawnCodePurger432(
        ProcessContext parent,
        string module,
        string function,
        IReadOnlyList<Term> arguments,
        out ProcessHandle? handle
    )
    {
        handle = null;
        if (module != CodePurgerNames432.Module || function != CodePurgerNames432.Start || arguments.Count != 0) return false;
        if (Modules.SourceOwner(module, function, 0) is not PreparedBeamImage image || !CodePurgerSource432.Matches(image, Modules)) return false;
        object reservation = new();
        lock (gate)
        {
            RequireEtsFoundationActor(parent);
            parent.Cancellation.ThrowIfCancellationRequested();
            if (codePurgerClosed432 || sourceCodePurgerReservation432 is not null || !codePurgerRetirement432.IsCompletedSuccessfully) return false;
            sourceCodePurgerReservation432 = reservation;
            sourceCodePurgerImage432 = image;
            try
            {
                handle = SpawnTraced(
                    async child =>
                {
                    lock (gate)
                    {
                        if (!ReferenceEquals(sourceCodePurgerReservation432, reservation) || !ReferenceEquals(sourceCodePurger432, child))
                            throw new InvalidOperationException(CodePurgerNames432.Invariant);
                    }

                    // Hibernate unwinds this body without ending the actual actor.
                    return await BeamRegisterInterpreter.Execute(
                        child,
                        image.Code,
                        function,
                        arguments,
                        image.ExecutionIdentity
                    );
                },
                    parent,
                    false,
                    SequenceSpawnDescription.Module(
                        Term.A(module),
                        Term.A(function),
                        Nil.Value,
                        Nil.Value
                    ),
                    systemProcess: true
                );
                sourceCodePurger432 = processes[handle.Pid];
            }
            catch
            {
                sourceCodePurger432 = null;
                sourceCodePurgerImage432 = null;
                sourceCodePurgerReservation432 = null;
                throw;
            }
        }

        return true;
    }

    private void StartCodePurgerBeforeBoot432()
    {
        if (Modules.SourceOwner(CodePurgerNames432.Module, CodePurgerNames432.Start, 0) is not PreparedBeamImage image
            || !CodePurgerSource432.Matches(image, Modules)) return;
        ProcessContext parent;
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            ThrowIfHalted();
            parent = processes[InitializationPid];
        }
        if (!TrySpawnCodePurger432(
            parent,
            CodePurgerNames432.Module,
            CodePurgerNames432.Start,
            [],
            out _
        ))
            throw new InvalidOperationException(CodePurgerNames432.Invariant);
    }
}
