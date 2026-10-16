using Erlang.Otp;

namespace Erlang.Tests;

public static class ProcLibInitialCallHostChecks
{
    public static async Task Run()
    {
        await using var runtime = new ProcessRuntime();
        OtpModules.Register(runtime.Modules);
        Term ok = Term.A(ProcLibInitialCallHostNames.Ok);
        Term server = Term.A(ProcLibInitialCallHostNames.GenServer);
        Term supervisor = Term.A(ProcLibInitialCallHostNames.Supervisor);
        Term bridge = Term.A(ProcLibInitialCallHostNames.Bridge);
        Term callback = Term.A(ProcLibInitialCallHostNames.Callback);
        Term placeholder = Nil.Value;
        (Term[] Arguments, Term Expected)[] cases = [
            ([server, placeholder, placeholder, supervisor, Term.Tuple(placeholder, callback, placeholder), placeholder], Term.Tuple(supervisor, callback, Term.I(1))),
            ([server, placeholder, placeholder, placeholder, supervisor, Term.Tuple(placeholder, Term.I(17), placeholder), placeholder], Term.Tuple(supervisor, Term.I(17), Term.I(1))),
            ([server, placeholder, placeholder, bridge, Term.List(callback), placeholder], Term.Tuple(bridge, callback, Term.I(1))),
            ([server, placeholder, placeholder, placeholder, bridge, new Cons(Term.I(19), callback), placeholder], Term.Tuple(bridge, Term.I(19), Term.I(1))),
            ([Term.A(ProcLibInitialCallHostNames.GenEvent)], Term.Tuple(Term.A(ProcLibInitialCallHostNames.GenEvent), Term.A(ProcLibInitialCallHostNames.InitIt), Term.I(6))),
            ([placeholder, placeholder, placeholder, callback, placeholder, placeholder], Term.Tuple(callback, Term.A(ProcLibInitialCallHostNames.Init), Term.I(1))),
            ([placeholder, placeholder, placeholder, placeholder, callback], Term.Tuple(callback, Term.A(ProcLibInitialCallHostNames.Init), Term.I(1))),
            ([placeholder, placeholder, placeholder, placeholder, callback, placeholder, placeholder, placeholder], Term.Tuple(callback, Term.A(ProcLibInitialCallHostNames.Init), Term.I(1))),
            ([placeholder], Term.Tuple(Term.A(ProcLibInitialCallHostNames.Gen), Term.A(ProcLibInitialCallHostNames.InitIt), Term.I(1)))
        ];
        var parent = runtime.Spawn(async context =>
        {
            foreach (var control in cases)
            {
                runtime.Modules.Register(
                    ProcLibInitialCallHostNames.Gen,
                    ProcLibInitialCallHostNames.InitIt,
                    control.Arguments.Length,
                    (child, _) =>
                    {
                        runtime.Send(context.Self, Term.Tuple(child.Self, child.Dictionary[Term.A(ProcLibInitialCallHostNames.InitialCall)]));

                        return ValueTask.FromResult(ok);
                    }
                );
                Term childPid = await runtime.Modules.Call(
                    context,
                    ProcLibInitialCallHostNames.ProcLib,
                    ProcLibInitialCallHostNames.Spawn,
                    Term.A(ProcLibInitialCallHostNames.Gen),
                    Term.A(ProcLibInitialCallHostNames.InitIt),
                    Cons.From(control.Arguments)
                );
                Term? actual = await context.ReceiveAsync(value => value is TupleTerm { Items: [var sender, var result] } && sender.Equals(childPid) ? result : null);
                if (actual is null || !actual.Equals(control.Expected))
                    throw new InvalidOperationException(ProcLibInitialCallHostNames.Failure);
            }

            return ok;
        });
        Term reason = await parent.Completion.WaitAsync(TimeSpan.FromSeconds(30));
        if (!reason.Equals(Term.A(ProcessExitReasons.Normal)))
            throw new InvalidOperationException(ProcLibInitialCallHostNames.Failure);
    }
}
