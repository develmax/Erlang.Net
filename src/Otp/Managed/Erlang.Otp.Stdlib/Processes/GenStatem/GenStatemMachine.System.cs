// SPDX-License-Identifier: Apache-2.0
// Copyright Ericsson AB 2016-2026. All Rights Reserved.
// Modified: OTP-29.1.1 sys callback record protocol and termination ported to C#.
using static Erlang.GenStatemModule;

namespace Erlang;

internal sealed partial class GenStatemMachine
{
    internal Term Snapshot()
    {
        Term mode = Is(callbackMode, GenStatemProtocolAtoms.StateFunctions) ? callbackMode : External(Module, GenStatemFunctionNames.HandleEvent, 4);
        Term parameters = Term.Tuple(
            Term.A(GenStatemProtocolAtoms.Params),
            mode,
            Boolean(stateEnter),
            Parent,
            Cons.From(modules),
            name,
            hibernateAfter
        );
        Term state = Term.Tuple(
            Term.A(GenStatemProtocolAtoms.State),
            StateData,
            Cons.From(postponed.AsEnumerable().Reverse()),
            new MapTerm(timers),
            Cons.From(zeroQueue),
            Boolean(hibernate)
        );

        return Term.Tuple(parameters, state);
    }

    private static FunctionTerm External(string module, string function, int arity)
        => new(
            new ExternalFunctionIdentity(Term.A(module), Term.A(function), arity),
            (execution, arguments) => GenStatemModule.Call(
                (ProcessContext)execution,
                module,
                function,
                arguments.ToArray()
            )
        );

    internal static GenStatemMachine Restore(ProcessContext context, Term snapshot, Term debug)
    {
        var pair = (TupleTerm)snapshot;
        var parameters = (TupleTerm)pair.Items[GenStatemRecordLayout.SystemState.Parameters];
        var state = (TupleTerm)pair.Items[GenStatemRecordLayout.SystemState.State];
        var machine = new GenStatemMachine(context)
        {
            callbackMode = parameters.Items[GenStatemRecordLayout.Parameters.CallbackMode] is FunctionTerm ? Term.A(GenStatemProtocolAtoms.HandleEventFunction) : parameters.Items[GenStatemRecordLayout.Parameters.CallbackMode],
            stateEnter = Is(parameters.Items[GenStatemRecordLayout.Parameters.StateEnter], GenStatemProtocolAtoms.True),
            Parent = parameters.Items[GenStatemRecordLayout.Parameters.Parent],
            modules = Cons.Items(parameters.Items[GenStatemRecordLayout.Parameters.Modules]).ToList(),
            name = parameters.Items[GenStatemRecordLayout.Parameters.Name],
            hibernateAfter = parameters.Items[GenStatemRecordLayout.Parameters.HibernateAfter],
            StateData = state.Items[GenStatemRecordLayout.State.StateData],
            hibernate = Is(state.Items[GenStatemRecordLayout.State.Hibernate], GenStatemProtocolAtoms.True),
            debug = debug
        };
        machine.postponed.AddRange(Cons.Items(state.Items[GenStatemRecordLayout.State.Postponed]).Reverse());
        foreach (var timer in ((MapTerm)state.Items[GenStatemRecordLayout.State.Timers]).Entries)
        {
            machine.timers.Add(timer.Key, timer.Value);
        }
        foreach (Term zero in Cons.Items(state.Items[GenStatemRecordLayout.State.ZeroQueue]))
        {
            machine.zeroQueue.Enqueue(zero);
        }

        return machine;
    }

    internal static async ValueTask<Term> CodeChange(ProcessContext context, Term[] a)
    {
        var machine = Restore(context, a[0], Nil.Value);
        Term result = await machine.Callback(
            GenStatemFunctionNames.CodeChange,
            a[2],
            machine.State,
            machine.Data,
            a[3]
        );
        if (result is TupleTerm { Items.Count: 3 } ok && Is(ok.Items[0], GenStatemProtocolAtoms.Ok))
        {
            await machine.SetCallbackMode();
            machine.StateData = Term.Tuple(ok.Items[1], ok.Items[2]);

            return Term.Tuple(Term.A(GenStatemProtocolAtoms.Ok), machine.Snapshot());
        }
        if (result is TupleTerm { Items.Count: 2 } bad && Is(bad.Items[0], GenStatemProtocolAtoms.Ok))
        {
            throw new ErlangException(Term.Tuple(Term.A(GenStatemProtocolAtoms.CaseClause), result));
        }

        return result;
    }

    internal async ValueTask<Term> Terminate(ErlangException error)
    {
        terminating = true;
        if (context.Runtime.Modules.Exports.Contains((Module, GenStatemFunctionNames.Terminate, 3)))
        {
            try
            {
                await Callback(
                    GenStatemFunctionNames.Terminate,
                    error.Reason,
                    State,
                    Data
                );
            }
            catch (ErlangException replacement)
            {
                await ErrorInfo(replacement);
                throw;
            }
        }
        if (Is(error.Reason, GenStatemProtocolAtoms.Normal) || Is(error.Reason, GenStatemProtocolAtoms.Shutdown)
            || error.Reason is TupleTerm { Items.Count: 2 } shutdown && Is(shutdown.Items[0], GenStatemProtocolAtoms.Shutdown))
        {
            await Debug(Term.Tuple(Term.A(GenStatemProtocolAtoms.Terminate), error.Reason, State));
        }
        else
        {
            await ErrorInfo(error);
        }
        throw error;
    }

    private static MapTerm Map(params (string Key, Term Value)[] entries)
        => new(entries.Select(pair => new KeyValuePair<Term, Term>(Term.A(pair.Key), pair.Value)));

    private async ValueTask ErrorInfo(ErlangException error)
    {
        Term timeouts = ListTimeouts();
        Term log = await Call(GenStatemDependencyNames.Sys, GenStatemFunctionNames.GetLog, debug);
        var status = (MapTerm)await Call(
            GenStatemDependencyNames.Gen,
            GenStatemFunctionNames.FormatStatus,
            modules[0],
            Term.A(GenStatemProtocolAtoms.Terminate),
            Map(
                (GenStatemStatusNames.Reason, error.Reason),
                (GenStatemStatusNames.State, State),
                (GenStatemStatusNames.Data, Data),
                (GenStatemStatusNames.Queue, Cons.From(CurrentQueue())),
                (GenStatemStatusNames.Postponed, Cons.From(postponed.AsEnumerable().Reverse())),
                (GenStatemStatusNames.Timeouts, timeouts),
                (GenStatemStatusNames.Log, log)
            ),
            Term.List(await Call(GenStatemDependencyNames.Erlang, GenStatemFunctionNames.Get), State, Data)
        );
        Term newState = status.TryGet(Term.A(GenStatemStatusNames.Status), out Term? formatted) ? formatted! : Term.Tuple(status.Get(Term.A(GenStatemStatusNames.State)), status.Get(Term.A(GenStatemStatusNames.Data)));
        if (Is(
            await Call(
                GenStatemDependencyNames.Logger,
                GenStatemFunctionNames.Allow,
                Term.A(GenStatemProtocolAtoms.Error),
                Term.A(Name)
            ),
            GenStatemProtocolAtoms.False
        ))
        {
            return;
        }
        Term report = Map(
            (GenStatemStatusNames.Label, Term.Tuple(Term.A(Name), Term.A(GenStatemProtocolAtoms.Terminate))),
            (GenStatemStatusNames.Name, name),
            (GenStatemStatusNames.Queue, status.Get(Term.A(GenStatemStatusNames.Queue))),
            (GenStatemStatusNames.Postponed, status.Get(Term.A(GenStatemStatusNames.Postponed))),
            (GenStatemStatusNames.Modules, Cons.From(modules)),
            (GenStatemStatusNames.CallbackMode, callbackMode),
            (GenStatemStatusNames.StateEnter, Boolean(stateEnter)),
            (GenStatemStatusNames.State, newState),
            (GenStatemStatusNames.Timeouts, Term.Tuple(Term.I(Cons.Items(timeouts).Count()), status.Get(Term.A(GenStatemStatusNames.Timeouts)))),
            (GenStatemStatusNames.Log, status.Get(Term.A(GenStatemStatusNames.Log))),
            (GenStatemStatusNames.Reason, Term.Tuple(Term.A(error.ExceptionClass), status.Get(Term.A(GenStatemStatusNames.Reason)), error.StackTraceTerm)),
            (GenStatemStatusNames.ClientInfo, await ClientStack()),
            (GenStatemStatusNames.ProcessLabel, await Call(GenStatemDependencyNames.ProcLib, GenStatemFunctionNames.GetLabel, context.Self))
        );
        Term metadata = Map(
            (GenStatemStatusNames.Domain, Term.List(Term.A(GenStatemProtocolAtoms.Otp))),
            (GenStatemStatusNames.ReportCb, External(Name, GenStatemFunctionNames.FormatLog, 2)),
            (GenStatemStatusNames.ErrorLogger, Map(
                (GenStatemStatusNames.Tag, Term.A(GenStatemProtocolAtoms.Error)),
                (GenStatemStatusNames.ReportCb, External(Name, GenStatemFunctionNames.FormatLog, 1))
            ))
        );
        Term location = Map(
            (GenStatemStatusNames.Mfa, Term.Tuple(Term.A(Name), Term.A(GenStatemFunctionNames.ErrorInfo), Term.I(GenStatemSourceArities.ErrorInfo))),
            (GenStatemStatusNames.Line, Term.I(GenStatemSourceLines.ErrorInfoLog)),
            (GenStatemStatusNames.File, Term.String(GenStatemSourceNames.FileName))
        );
        await Call(
            GenStatemDependencyNames.Logger,
            GenStatemFunctionNames.MacroLog,
            location,
            Term.A(GenStatemProtocolAtoms.Error),
            report,
            metadata
        );
    }

    private async ValueTask<Term> ClientStack()
    {
        if (CurrentQueue().FirstOrDefault() is not TupleTerm { Items.Count: 2 } e
            || e.Items[0] is not TupleTerm { Items.Count: 2 } type || !Is(type.Items[0], GenStatemProtocolAtoms.Call)
            || type.Items[1] is not TupleTerm { Items.Count: 2 } from || from.Items[0] is not Pid pid)
        {
            return Term.A(GenStatemProtocolAtoms.Undefined);
        }
        Term node = await Call(GenStatemDependencyNames.Erlang, GenStatemFunctionNames.Node, pid);
        if (!node.Equals(await Call(GenStatemDependencyNames.Erlang, GenStatemFunctionNames.Node)))
        {
            return Term.Tuple(pid, Term.A(GenStatemProtocolAtoms.Remote));
        }
        Term info = await Call(
            GenStatemDependencyNames.Erlang,
            GenStatemFunctionNames.ProcessInfo,
            pid,
            Term.List(Term.A(GenStatemProtocolAtoms.CurrentStacktrace), Term.A(GenStatemProtocolAtoms.RegisteredName))
        );
        if (Is(info, GenStatemProtocolAtoms.Undefined))
        {
            return Term.Tuple(pid, Term.A(GenStatemProtocolAtoms.Dead));
        }
        Term[] properties = Cons.Items(info).ToArray();
        Term stack = ((TupleTerm)properties[0]).Items[1];
        Term registered = ((TupleTerm)properties[1]).Items[1];

        return Term.Tuple(pid, Term.Tuple(registered is Nil ? pid : registered, stack));
    }

    private IEnumerable<Term> CurrentQueue() => currentEvent is null ? events : new[] { currentEvent }.Concat(events);

    internal static async ValueTask<Term> FormatStatus(ProcessContext context, Term option, Term arguments)
    {
        Term[] a = Cons.Items(arguments).ToArray();
        var machine = Restore(context, a[GenStatemRecordLayout.StatusArguments.State], a[GenStatemRecordLayout.StatusArguments.Debug]);
        Term header = await machine.Call(
            GenStatemDependencyNames.Gen,
            GenStatemFunctionNames.FormatStatusHeader,
            Term.String(GenStatemStatusLabels.Header),
            machine.name
        );
        Term timeouts = machine.ListTimeouts();
        var status = Map(
            (GenStatemStatusNames.State, machine.State),
            (GenStatemStatusNames.Data, machine.Data),
            (GenStatemStatusNames.Postponed, Cons.From(machine.postponed.AsEnumerable().Reverse())),
            (GenStatemStatusNames.Log, await machine.Call(GenStatemDependencyNames.Sys, GenStatemFunctionNames.GetLog, machine.debug)),
            (GenStatemStatusNames.Timeouts, timeouts)
        );
        var formatted = (MapTerm)await machine.Call(
            GenStatemDependencyNames.Gen,
            GenStatemFunctionNames.FormatStatus,
            machine.modules[0],
            option,
            status,
            Term.List(a[GenStatemRecordLayout.StatusArguments.Dictionary], machine.State, machine.Data)
        );
        Term details;
        if (formatted.TryGet(Term.A(GenStatemStatusNames.EXIT), out Term? crash))
        {
            details = Term.List(Term.Tuple(
                Term.A(GenStatemProtocolAtoms.Data),
                Term.List(Term.Tuple(Term.String(GenStatemStatusLabels.State), Term.Tuple(machine.State, crash!)))
            ));
            formatted = status;
        }
        else if (formatted.TryGet(Term.A(GenStatemStatusNames.Status), out Term? oldStatus))
        {
            details = Cons.From(Listify(oldStatus!));
        }
        else
        {
            details = Term.List(Term.Tuple(
                Term.A(GenStatemProtocolAtoms.Data),
                Term.List(Term.Tuple(
                    Term.String(GenStatemStatusLabels.State),
                    Term.Tuple(formatted.Get(Term.A(GenStatemStatusNames.State)), formatted.Get(Term.A(GenStatemStatusNames.Data)))
                ))
            ));
        }

        return Cons.From(
            [
            Term.Tuple(Term.A(GenStatemProtocolAtoms.Header), header),
            Term.Tuple(
                Term.A(GenStatemProtocolAtoms.Data),
                Term.List(
                    Term.Tuple(Term.String(GenStatemStatusLabels.Status), a[GenStatemRecordLayout.StatusArguments.Status]),
                    Term.Tuple(Term.String(GenStatemStatusLabels.Parent), a[GenStatemRecordLayout.StatusArguments.Parent]),
                    Term.Tuple(Term.String(GenStatemStatusLabels.Modules), Cons.From(machine.modules)),
                    Term.Tuple(
                        Term.String(GenStatemStatusLabels.Timeouts),
                        Term.Tuple(Term.I(Cons.Items(timeouts).Count()), formatted.Get(Term.A(GenStatemStatusNames.Timeouts)))
                    ),
                    Term.Tuple(Term.String(GenStatemStatusLabels.LoggedEvents), formatted.Get(Term.A(GenStatemStatusNames.Log))),
                    Term.Tuple(Term.String(GenStatemStatusLabels.Postponed), formatted.Get(Term.A(GenStatemStatusNames.Postponed)))
                )
            )
        ],
            details
        );
    }

    internal static Term BehaviourInfo(Term query)
    {
        (string Name, int Arity)[] required = [(GenStatemFunctionNames.Init, 1), (GenStatemFunctionNames.CallbackMode, 0), (GenStatemFunctionNames.StateName, 3), (GenStatemFunctionNames.HandleEvent, 4), (GenStatemFunctionNames.Terminate, 3), (GenStatemFunctionNames.CodeChange, 4), (GenStatemFunctionNames.FormatStatus, 2), (GenStatemFunctionNames.FormatStatus, 1)];
        (string Name, int Arity)[] optional = [(GenStatemFunctionNames.FormatStatus, 1), (GenStatemFunctionNames.FormatStatus, 2), (GenStatemFunctionNames.Terminate, 3), (GenStatemFunctionNames.CodeChange, 4), (GenStatemFunctionNames.StateName, 3), (GenStatemFunctionNames.HandleEvent, 4)];
        var selected = Is(query, GenStatemProtocolAtoms.Callbacks) ? required : Is(query, GenStatemProtocolAtoms.OptionalCallbacks) ? optional : null;

        return selected is null ? Term.A(GenStatemProtocolAtoms.Undefined) : Cons.From(selected.Select(pair => Term.Tuple(Term.A(pair.Name), Term.I(pair.Arity))));
    }
}
