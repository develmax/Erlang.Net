namespace Erlang.Otp;

public interface IGenServer
{
    ValueTask<Term> Init(ProcessContext context, Term arguments);

    async ValueTask<ServerInitialization> Initialize(ProcessContext context, Term arguments) => new(await Init(context, arguments));

    ValueTask<ServerResult> HandleContinue(ProcessContext context, Term continuation, Term state) => throw new ErlangException(ErlangErrorReasons.UndefinedFunction);

    ValueTask<ServerResult> HandleCall(
        ProcessContext context,
        Term request,
        ServerFrom from,
        Term state
    );

    ValueTask<ServerResult> HandleCast(ProcessContext context, Term request, Term state);

    ValueTask<ServerResult> HandleInfo(ProcessContext context, Term message, Term state);

    ValueTask Terminate(ProcessContext context, Term reason, Term state) => ValueTask.CompletedTask;
}
