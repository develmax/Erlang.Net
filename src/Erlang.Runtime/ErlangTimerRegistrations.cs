namespace Erlang;

internal static class ErlangTimerRegistrations
{
    public static void Register(ModuleRegistry registry)
    {
        foreach (string operation in new[] { TimerModuleNames.SendAfter, TimerModuleNames.StartTimer })
        {
            registry.Register(
                ErlangModuleNames.Module,
                operation,
                4,
                async (context, arguments) =>
            {
                var options = ErlangTimerOptions.Parse(arguments[3], true, false);
                var reference = (ReferenceTerm)await context.Runtime.Modules.Call(context, ErlangModuleNames.Module, ErlangModuleNames.MakeReference);

                return context.Runtime.Timers.Start(
                    reference,
                    arguments[0],
                    arguments[1],
                    arguments[2],
                    operation == TimerModuleNames.StartTimer,
                    options.Absolute
                );
            }
            );
        }
        foreach (string operation in new[] { TimerModuleNames.ReadTimer, TimerModuleNames.CancelTimer })
        {
            registry.Register(
                ErlangModuleNames.Module,
                operation,
                2,
                async (context, arguments) =>
            {
                bool cancel = operation == TimerModuleNames.CancelTimer;
                var options = ErlangTimerOptions.Parse(arguments[1], false, cancel);
                Term result = await context.Runtime.Modules.Call(
                    context,
                    ErlangModuleNames.Module,
                    operation,
                    arguments[0]
                );
                if (options.Async)
                {
                    if (options.Info)
                        context.Runtime.Send(context.Self, Term.Tuple(Term.A(cancel ? TimerReplyAtoms.Cancel : TimerReplyAtoms.Read), arguments[0], result));

                    return Term.A(TimerReplyAtoms.Ok);
                }

                return options.Info ? result : arguments[0] is ReferenceTerm reference && reference.Node == context.Runtime.Node && reference.Creation == 0 ? Term.A(TimerReplyAtoms.Ok) : Term.A(ErlangBooleanAtoms.False);
            }
            );
        }
    }
}
