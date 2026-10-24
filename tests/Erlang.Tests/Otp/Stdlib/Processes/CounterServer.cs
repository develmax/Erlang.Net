using System.Diagnostics;
using System.Numerics;
using System.Text.Json;
using Erlang;
using Erlang.Compiler;
using Erlang.Otp;

internal sealed class CounterServer : IGenServer
{
    public bool Terminated
    {
        get; private set;
    }

    public ValueTask<Term> Init(ProcessContext context, Term arguments) => arguments.Equals(Term.A("fail")) ? throw new ErlangException("init_failed") : ValueTask.FromResult(arguments);

    public ValueTask<ServerResult> HandleCall(
        ProcessContext context,
        Term request,
        ServerFrom from,
        Term state
    )
        => request.Equals(Term.A("crash")) ? throw new ErlangException("boom") : ValueTask.FromResult(request.Equals(Term.A("stop")) ? new ServerResult(state, Term.A("ok"), Term.A("normal")) : new ServerResult(state, request.Equals(Term.A("noreply")) ? null : state));

    public ValueTask<ServerResult> HandleCast(ProcessContext context, Term request, Term state) => ValueTask.FromResult(new ServerResult(CoreModules.Arithmetic("+", state, request)));

    public ValueTask<ServerResult> HandleInfo(ProcessContext context, Term message, Term state) => ValueTask.FromResult(new ServerResult(CoreModules.Arithmetic("+", state, message)));

    public ValueTask Terminate(ProcessContext context, Term reason, Term state)
    {
        Terminated = true;

        return ValueTask.CompletedTask;
    }
}
