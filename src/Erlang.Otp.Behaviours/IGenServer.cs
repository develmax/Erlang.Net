

namespace Erlang.Otp;

public interface IGenServer
{
    ValueTask<Term> Init(ProcessContext context, Term arguments);
    ValueTask<ServerResult> HandleCall(ProcessContext context, Term request, ServerFrom from, Term state);
    ValueTask<ServerResult> HandleCast(ProcessContext context, Term request, Term state);
    ValueTask<ServerResult> HandleInfo(ProcessContext context, Term message, Term state);
    ValueTask Terminate(ProcessContext context, Term reason, Term state) => ValueTask.CompletedTask;
}
