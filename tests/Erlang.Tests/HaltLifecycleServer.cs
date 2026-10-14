using Erlang.Otp;

namespace Erlang.Tests;

internal sealed class HaltLifecycleServer : IGenServer
{
    public int Terminations;
    public int Continuations;

    public ValueTask<Term> Init(ProcessContext context, Term arguments) => ValueTask.FromResult<Term>(Nil.Value);

    public ValueTask<ServerResult> HandleCall(
        ProcessContext context,
        Term request,
        ServerFrom from,
        Term state
    ) => throw new InvalidOperationException(HaltLifecycleHostNames.Failure);

    public ValueTask<ServerResult> HandleInfo(ProcessContext context, Term message, Term state) => throw new InvalidOperationException(HaltLifecycleHostNames.Failure);

    public ValueTask<ServerResult> HandleCast(ProcessContext context, Term request, Term state)
    {
        context.Halt(HaltLifecycleHostNames.TransferStatus);
        Interlocked.Increment(ref Continuations);

        throw new InvalidOperationException(HaltLifecycleHostNames.Failure);
    }

    public ValueTask Terminate(ProcessContext context, Term reason, Term state)
    {
        Interlocked.Increment(ref Terminations);

        return ValueTask.CompletedTask;
    }
}
