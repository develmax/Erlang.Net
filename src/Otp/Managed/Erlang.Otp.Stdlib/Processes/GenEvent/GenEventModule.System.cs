// Derived from Erlang/OTP 29.1.1 gen_event.erl (Apache-2.0), Ericsson AB.
namespace Erlang;

public static partial class GenEventModule
{
    private static async ValueTask<Term> SystemState(ProcessContext context, string function, Term[] arguments)
    {
        Term[] state = StateArguments(
            context,
            function,
            arguments,
            arguments[function == GenEventApi.SystemReplaceState ? 1 : 0]
        );
        List<GenEventHandler> handlers = ReadHandlers(state[GenEventManagerStateLayout.HandlersIndex]);
        var states = new List<Term>();
        for (int index = 0; index < handlers.Count; index++)
        {
            GenEventHandler handler = handlers[index];
            Term current = Term.Tuple(handler.Module, handler.Id, handler.State);
            if (function == GenEventSourceFunctions.SystemCodeChange && handler.Module.Equals(arguments[1]))
            {
                if (handler.Module is not Atom module)
                {
                    throw BadArgument();
                }
                Term changed;
                using (GenEventSource.Frame(
                    context,
                    GenEventSourceFunctions.SystemCodeChange,
                    GenEventSourceArities.SystemCodeChange,
                    GenEventSourceLines.CodeChangeIteration
                ))
                using (context.EnterGeneratedFunction(
                    GenEventDependencyModules.Lists,
                    GenEventSourceFunctions.FilterMap,
                    GenEventSourceArities.FilterMap,
                    Term.List(
                        Term.Tuple(A(GenEventSourceFields.File), Term.String(GenEventSourceFields.ListsFile)),
                        Term.Tuple(A(GenEventSourceFields.Line), Term.I(GenEventSourceLines.ListsFilterMapCallback))
                    ),
                    new object()
                ))
                using (GenEventSource.Frame(
                    context,
                    GenEventSourceFunctions.CodeChangeCallback,
                    GenEventSourceArities.CodeChangeCallback,
                    GenEventSourceLines.CodeChangeInvocation
                ))
                {
                    changed = await External(
                        context,
                        module.Name,
                        GenEventCallbacks.CodeChange,
                        arguments[2],
                        handler.State,
                        arguments[3]
                    );
                }
                if (changed is not TupleTerm { Items.Count: 2 } ok || !Is(ok.Items[0], GenEventResults.Ok))
                {
                    throw GenEventSource.Capture(
                        context,
                        Term.Tuple(A(ErlangErrorReasons.BadMatch), changed),
                        [
                        Term.Tuple(
                            A(Name),
                            A(GenEventSourceFunctions.CodeChangeCallback),
                            Term.I(GenEventSourceArities.CodeChangeCallback),
                            GenEventSource.Location(GenEventSourceLines.CodeChangeResultMatch)
                        ),
                        Term.Tuple(
                            A(GenEventDependencyModules.Lists),
                            A(GenEventSourceFunctions.FilterMap),
                            Term.I(GenEventSourceArities.FilterMap),
                            Term.List(
                                Term.Tuple(A(GenEventSourceFields.File), Term.String(GenEventSourceFields.ListsFile)),
                                Term.Tuple(A(GenEventSourceFields.Line), Term.I(GenEventSourceLines.ListsFilterMapCallback))
                            )
                        ),
                        Term.Tuple(
                            A(Name),
                            A(GenEventSourceFunctions.SystemCodeChange),
                            Term.I(GenEventSourceArities.SystemCodeChange),
                            GenEventSource.Location(GenEventSourceLines.CodeChangeIteration)
                        )
                    ]
                    );
                }
                handlers[index] = handler with { State = ok.Items[1] };
            }
            else if (function == GenEventApi.SystemReplaceState)
            {
                try
                {
                    if (arguments[0] is not FunctionTerm callback)
                    {
                        throw new ErlangException(Term.Tuple(A(ErlangErrorReasons.BadFunction), arguments[0]));
                    }
                    Term replacement = await callback.Invoke(context, [current]);
                    if (replacement is TupleTerm { Items.Count: GenEventStateEntryLayout.FieldCount } next
                        && next.Items[GenEventStateEntryLayout.ModuleIndex].Equals(handler.Module) && next.Items[GenEventStateEntryLayout.IdIndex].Equals(handler.Id))
                    {
                        handlers[index] = handler with { State = next.Items[GenEventStateEntryLayout.StateIndex] };
                        current = replacement;
                    }
                }
                catch (ErlangException)
                {
                    // OTP preserves this individual handler after any fun/match failure.
                }
            }
            states.Add(current);
        }
        Term encoded = State(
            state[GenEventManagerStateLayout.NameIndex],
            handlers,
            state[GenEventManagerStateLayout.TimeoutIndex],
            Is(state[GenEventManagerStateLayout.HibernateIndex], ErlangBooleanAtoms.True)
        );

        return function switch
        {
            GenEventSourceFunctions.SystemCodeChange => Term.Tuple(A(GenEventResults.Ok), encoded),
            GenEventApi.SystemReplaceState => Term.Tuple(A(GenEventResults.Ok), Cons.From(states), encoded),
            _ => Term.Tuple(A(GenEventResults.Ok), Cons.From(states))
        };
    }

    private static Term[] StateArguments(
        ProcessContext context,
        string function,
        IReadOnlyList<Term> arguments,
        Term state
    )
    {
        var fields = new List<Term>();
        Term tail = state;
        while (tail is Cons cell && fields.Count <= GenEventManagerStateLayout.FieldCount)
        {
            fields.Add(cell.Head);
            tail = cell.Tail;
        }
        if (fields.Count == GenEventManagerStateLayout.FieldCount && tail is Nil)
        {
            return fields.ToArray();
        }
        int line = function switch
        {
            GenEventApi.SystemContinue => GenEventSourceLines.ContinueClause,
            GenEventApi.SystemTerminate => GenEventSourceLines.TerminateClause,
            GenEventSourceFunctions.SystemCodeChange => GenEventSourceLines.CodeChangeClause,
            GenEventApi.SystemGetState => GenEventSourceLines.GetStateClause,
            _ => GenEventSourceLines.ReplaceStateClause
        };
        throw GenEventSource.Error(
            context,
            function,
            arguments,
            ErlangErrorReasons.FunctionClause,
            line
        );
    }

    private static async ValueTask<Term> FormatStatus(ProcessContext context, Term option, Term data)
    {
        Term[] args = Cons.Items(data).ToArray();
        Term[] state = Cons.Items(args[GenEventStatusDataLayout.ManagerStateIndex]).ToArray();
        Term header = await External(
            context,
            GenEventDependencyModules.Gen,
            GenEventDependencyFunctions.FormatStatusHeader,
            Term.String(GenEventLogText.StatusHeader),
            state[GenEventManagerStateLayout.NameIndex]
        );
        Term logs = await External(
            context,
            GenEventDependencyModules.Sys,
            GenEventDependencyFunctions.GetLog,
            args[GenEventStatusDataLayout.DebugIndex]
        );
        var formatted = new List<Term>();
        foreach (GenEventHandler handler in ReadHandlers(state[GenEventManagerStateLayout.HandlersIndex]))
        {
            var status = (MapTerm)await External(
                context,
                GenEventDependencyModules.Gen,
                GenEventDependencyFunctions.FormatStatus,
                handler.Module,
                option,
                Map((GenEventStatusFields.Log, logs), (GenEventStatusFields.State, handler.State)),
                Term.List(args[GenEventStatusDataLayout.DictionaryIndex], handler.State)
            );
            formatted.Add((handler with { State = Get(status, GenEventStatusFields.FormattedStatus, Get(status, GenEventStatusFields.State)) }).Encode());
            logs = Get(status, GenEventStatusFields.Log);
        }

        return Term.List(
            Term.Tuple(A(GenEventStatusFields.Header), header),
            Term.Tuple(
                A(GenEventStatusFields.Data),
                Term.List(
                    Term.Tuple(Term.String(GenEventLogText.Status), args[GenEventStatusDataLayout.StatusIndex]),
                    Term.Tuple(Term.String(GenEventLogText.LoggedEvents), logs),
                    Term.Tuple(Term.String(GenEventLogText.Parent), args[GenEventStatusDataLayout.ParentIndex])
                )
            ),
            Term.Tuple(A(GenEventStatusFields.Items), Term.Tuple(Term.String(GenEventLogText.InstalledHandlers), Cons.From(formatted)))
        );
    }
}
