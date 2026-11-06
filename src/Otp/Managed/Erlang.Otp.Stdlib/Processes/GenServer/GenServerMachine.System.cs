// SPDX-License-Identifier: Apache-2.0
// Copyright Ericsson AB 1996-2026. All Rights Reserved.
// Modified: OTP-29.1.1 gen_server sys/debug/status/error protocols ported to C#.
using static Erlang.GenServerModule;

namespace Erlang;

internal sealed partial class GenServerMachine
{
    internal Term Snapshot() => Term.List(
        ServerData(),
        state,
        action,
        timer
    );

    private Term ServerData() => Term.Tuple(
        A(GenServerProtocolAtoms.ServerData),
        Parent,
        tag,
        name,
        module,
        hibernateAfter,
        External(Module, GenServerFunctionNames.HandleCall, 3),
        External(Module, GenServerFunctionNames.HandleCast, 2),
        External(Module, GenServerFunctionNames.HandleInfo, 2),
        External(Module, GenServerFunctionNames.HandleContinue, 2)
    );

    internal static Term[] StateList(
        ProcessContext c,
        string f,
        Term[] args,
        Term value,
        int line
    )
    {
        if (ProperList(value))
        {
            Term[] values = Cons.Items(value).ToArray();
            if (values.Length == GenServerRecordLayout.SystemState.Arity) return values;
        }
        throw GenServerSource.Clause(
            c,
            f,
            args,
            line
        );
    }

    internal static GenServerMachine Restore(ProcessContext c, Term value, Term debug)
    {
        Term[] values = Cons.Items(value).ToArray();
        var data = (TupleTerm)values[GenServerRecordLayout.SystemState.ServerData];

        return new GenServerMachine(c)
        {
            Parent = data.Items[GenServerRecordLayout.ServerData.Parent],
            tag = data.Items[GenServerRecordLayout.ServerData.TimerTag],
            name = data.Items[GenServerRecordLayout.ServerData.Name],
            module = data.Items[GenServerRecordLayout.ServerData.Module],
            hibernateAfter = data.Items[GenServerRecordLayout.ServerData.HibernateAfter],
            state = values[GenServerRecordLayout.SystemState.State],
            action = values[GenServerRecordLayout.SystemState.Action],
            timer = values[GenServerRecordLayout.SystemState.Timer],
            debug = debug
        };
    }

    internal static async ValueTask<Term> CodeChange(ProcessContext c, Term[] a)
    {
        Term[] values = StateList(
            c,
            GenServerFunctionNames.SystemCodeChange,
            a,
            a[0],
            GenServerSourceLines.SystemCodeChangeLine2667
        );
        if (values[GenServerRecordLayout.SystemState.ServerData] is not TupleTerm { Items.Count: GenServerRecordLayout.ServerData.Arity } record || !Is(record.Items[0], GenServerProtocolAtoms.ServerData))
            throw GenServerSource.Clause(
                c,
                GenServerFunctionNames.SystemCodeChange,
                a,
                GenServerSourceLines.SystemCodeChangeLine2667
            );
        Term result;
        using (GenServerSource.Scope(
            c,
            GenServerFunctionNames.SystemCodeChange,
            4,
            GenServerSourceLines.SystemCodeChangeLine2668
        ))
        {
            try
            {
                result = await GenServerModule.Call(
                    c,
                    ((Atom)record.Items[GenServerRecordLayout.ServerData.Module]).Name,
                    GenServerFunctionNames.CodeChange,
                    a[2],
                    values[GenServerRecordLayout.SystemState.State],
                    a[3]
                );
            }
            catch (ErlangException error)
            {
                result = error.ExceptionClass == ErlangExceptionClasses.Throw ? error.Reason
                    : Term.Tuple(
                        A(GenServerStatusNames.ExitSignal),
                        error.ExceptionClass == ErlangExceptionClasses.Error ? Term.Tuple(error.Reason, error.StackTraceTerm) : error.Reason
                    );
            }
        }
        if (result is TupleTerm { Items.Count: 2 } ok && Is(ok.Items[0], GenServerProtocolAtoms.Ok))
        {
            values[GenServerRecordLayout.SystemState.State] = ok.Items[1];

            return Term.Tuple(A(GenServerProtocolAtoms.Ok), Term.List(values));
        }

        return result;
    }

    private async ValueTask Debug(Term evt)
    {
        if (debug is Nil) return;
        var printer = new FunctionTerm(3, (c, a) => PrintEvent((ProcessContext)c, a.ToArray()));
        debug = await Call(
            GenServerDependencyNames.Sys,
            GenServerFunctionNames.HandleDebug,
            debug,
            printer,
            name,
            evt
        );
    }

    private static ValueTask<Term> PrintEvent(ProcessContext c, Term[] a)
    {
        Term evt = a[1];
        string format;
        Term[] values;
        if (evt is TupleTerm { Items.Count: 2 } input && Is(input.Items[0], GenServerProtocolAtoms.In))
        {
            if (input.Items[1] is TupleTerm { Items.Count: 3 } call && Is(call.Items[0], GenServerProtocolAtoms.GenCall) && call.Items[1] is TupleTerm { Items.Count: 2 } from)
            {
                format = GenServerLogFormats.DBGTpGotCallTpFromTwN;
                values = [a[2], call.Items[2], from.Items[0]];
            }
            else if (input.Items[1] is TupleTerm { Items.Count: 2 } cast && Is(cast.Items[0], GenServerProtocolAtoms.GenCast))
            {
                format = GenServerLogFormats.DBGTpGotCastTpN;
                values = [a[2], cast.Items[1]];
            }
            else
            {
                format = GenServerLogFormats.DBGTpGotTpN;
                values = [a[2], input.Items[1]];
            }
        }
        else if (evt is TupleTerm { Items.Count: 4 } output && Is(output.Items[0], GenServerProtocolAtoms.Out) && output.Items[2] is TupleTerm { Items.Count: 2 } to)
        {
            format = GenServerLogFormats.DBGTpSentTpToTwNewStateTpN;
            values = [a[2], output.Items[1], to.Items[0], output.Items[3]];
        }
        else if (evt is TupleTerm { Items.Count: 2 } noReply && Is(noReply.Items[0], GenServerProtocolAtoms.Noreply))
        {
            format = GenServerLogFormats.DBGTpNewStateTpN;
            values = [a[2], noReply.Items[1]];
        }
        else
        {
            format = GenServerLogFormats.DBGTpDbgTpN;
            values = [a[2], evt];
        }

        return GenServerModule.Call(
            c,
            GenServerDependencyNames.Io,
            GenServerFunctionNames.Format,
            a[0],
            Term.String(format),
            Term.List(values)
        );
    }

    internal async ValueTask<Term> Terminate(ErlangException error, bool callbackFailure = false)
    {
        if (terminating || transferred) throw error;
        terminating = true;
        Term callbackReason = error.ExceptionClass == ErlangExceptionClasses.Error ? Term.Tuple(error.Reason, error.StackTraceTerm) : error.Reason;
        if (context.Runtime.Modules.Exports.Contains((Module, GenServerFunctionNames.Terminate, 2)))
        {
            try
            {
                await Callback(
                    GenServerFunctionNames.Terminate,
                    GenServerSourceLines.TryTerminateLine2482,
                    callbackReason,
                    state
                );
            }
            catch (ErlangException replacement)
            {
                await ErrorInfo(replacement, true);
                throw;
            }
        }
        if (!(error.ExceptionClass == ErlangExceptionClasses.Exit && (Is(error.Reason, GenServerProtocolAtoms.Normal) || Is(error.Reason, GenServerProtocolAtoms.Shutdown) || error.Reason is TupleTerm { Items.Count: 2 } shutdown && Is(shutdown.Items[0], GenServerProtocolAtoms.Shutdown))))
            await ErrorInfo(error, callbackFailure);
        throw error;
    }

    private async ValueTask MissingInfo()
    {
        await Log(
            GenServerFunctionNames.Warning,
            Map(
                (GenServerStatusNames.Label, Term.Tuple(A(Name), A(GenServerProtocolAtoms.NoHandleInfo))),
                (GenServerStatusNames.Module, module),
                (GenServerStatusNames.Message, message)
            ),
            GenServerFunctionNames.TryHandleInfo,
            3,
            GenServerSourceLines.TryHandleInfoLine2441,
            GenServerFunctionNames.WarningMsg
        );
    }

    private async ValueTask ErrorInfo(ErlangException error, bool stack)
    {
        if (Is(name, GenServerProtocolAtoms.ApplicationController)) return;
        Term log = await Call(GenServerDependencyNames.Sys, GenServerFunctionNames.GetLog, debug);
        var status = (MapTerm)await Call(
            GenServerDependencyNames.Gen,
            GenServerFunctionNames.FormatStatus,
            module,
            A(GenServerProtocolAtoms.Terminate),
            Map(
                (GenServerStatusNames.Reason, error.Reason),
                (GenServerStatusNames.State, state),
                (GenServerStatusNames.Message, message),
                (GenServerStatusNames.Log, log)
            ),
            Term.List(await Call(GenServerDependencyNames.Erlang, GenServerFunctionNames.Get), state)
        );
        Term visible = status.TryGet(A(GenServerStatusNames.ExitSignal), out Term? crash) ? crash! : status.TryGet(A(GenServerStatusNames.LegacyStatus), out Term? old) ? old! : status.Get(A(GenServerStatusNames.State));
        Term reason = status.Get(A(GenServerStatusNames.Reason));
        if (stack) reason = Term.Tuple(reason, error.StackTraceTerm);
        Term report = Map(
            (GenServerStatusNames.Label, Term.Tuple(A(Name), A(GenServerProtocolAtoms.Terminate))),
            (GenServerStatusNames.Name, name),
            (GenServerStatusNames.LastMessage, status.Get(A(GenServerStatusNames.Message))),
            (GenServerStatusNames.State, visible),
            (GenServerStatusNames.Log, await FormatLogState(status.Get(A(GenServerStatusNames.Log)))),
            (GenServerStatusNames.Reason, reason),
            (GenServerStatusNames.ClientInfo, await ClientStack()),
            (GenServerStatusNames.ProcessLabel, await Call(GenServerDependencyNames.ProcLib, GenServerFunctionNames.GetLabel, context.Self))
        );
        await Log(
            GenServerFunctionNames.Error,
            report,
            GenServerFunctionNames.ErrorInfo,
            7,
            GenServerSourceLines.ErrorInfoLine2786,
            GenServerFunctionNames.Error
        );
    }

    private async ValueTask Log(
        string level,
        Term report,
        string function,
        int arity,
        int line,
        string legacyTag
    )
    {
        if (Is(await Call(
            GenServerDependencyNames.Logger,
            GenServerFunctionNames.Allow,
            A(level),
            A(Name)
        ), GenServerProtocolAtoms.False)) return;
        Term meta = Map(
            (GenServerStatusNames.Domain, Term.List(A(GenServerProtocolAtoms.Otp))),
            (GenServerStatusNames.ReportCb, External(Name, GenServerFunctionNames.FormatLog, 2)),
            (GenServerStatusNames.ErrorLogger, Map(
                (GenServerStatusNames.Tag, A(legacyTag)),
                (GenServerStatusNames.ReportCb, External(Name, GenServerFunctionNames.FormatLog, 1))
            ))
        );
        Term location = Map(
            (GenServerStatusNames.Mfa, Term.Tuple(A(Name), A(function), Term.I(arity))),
            (GenServerStatusNames.Line, Term.I(line)),
            (GenServerStatusNames.File, Term.String(GenServerSourceNames.GenServerErl))
        );
        await Call(
            GenServerDependencyNames.Logger,
            GenServerFunctionNames.MacroLog,
            location,
            A(level),
            report,
            meta
        );
    }

    private async ValueTask<Term> ClientStack()
    {
        if (Is(from, GenServerProtocolAtoms.Undefined)) return from;
        Term client = from is TupleTerm { Items.Count: 2 } pair ? pair.Items[0] : from;
        if (client is not Pid) throw GenServerSource.Clause(
            context,
            GenServerFunctionNames.ClientStacktrace,
            [client],
            GenServerSourceLines.ClientStacktraceLine2799
        );
        if (!(await Call(GenServerDependencyNames.Erlang, GenServerFunctionNames.Node, client)).Equals(await Call(GenServerDependencyNames.Erlang, GenServerFunctionNames.Node))) return Term.Tuple(client, A(GenServerProtocolAtoms.Remote));
        Term info = await Call(
            GenServerDependencyNames.Erlang,
            GenServerFunctionNames.ProcessInfo,
            client,
            Term.List(A(GenServerProtocolAtoms.CurrentStacktrace), A(GenServerProtocolAtoms.RegisteredName))
        );
        if (Is(info, GenServerProtocolAtoms.Undefined)) return Term.Tuple(client, A(GenServerProtocolAtoms.Dead));
        Term[] properties = Cons.Items(info).ToArray();
        Term registered = ((TupleTerm)properties[1]).Items[1];

        return Term.Tuple(client, Term.Tuple(registered is Nil ? client : registered, ((TupleTerm)properties[0]).Items[1]));
    }

    private async ValueTask<Term> FormatLogState(Term log)
    {
        if (context.Runtime.Modules.Exports.Contains((Module, GenServerFunctionNames.FormatStatus, 1))) return log;
        var output = new List<Term>();
        foreach (Term evt in Cons.Items(log))
        {
            if (evt is TupleTerm tuple && (tuple.Items.Count == 4 && Is(tuple.Items[0], GenServerProtocolAtoms.Out) || tuple.Items.Count == 2 && Is(tuple.Items[0], GenServerProtocolAtoms.Noreply)))
            {
                Term oldState = tuple.Items[^1];
                var formatted = (MapTerm)await Call(
                    GenServerDependencyNames.Gen,
                    GenServerFunctionNames.FormatStatus,
                    module,
                    A(GenServerProtocolAtoms.Terminate),
                    Map((GenServerStatusNames.State, oldState)),
                    Term.List(await Call(GenServerDependencyNames.Erlang, GenServerFunctionNames.Get), oldState)
                );
                Term[] fields = tuple.Items.ToArray();
                fields[^1] = formatted.Get(A(GenServerStatusNames.State));
                output.Add(Term.Tuple(fields));
            }
            else output.Add(evt);
        }

        return Cons.From(output);
    }

    internal static async ValueTask<Term> FormatStatus(ProcessContext c, Term[] args)
    {
        if (!ProperList(args[1])) throw GenServerSource.Failure(
            c,
            GenServerFunctionNames.FormatStatus,
            2,
            GenServerSourceLines.FormatStatusLine3082,
            Term.Tuple(A(GenServerProtocolAtoms.Badmatch), args[1])
        );
        Term[] a = Cons.Items(args[1]).ToArray();
        if (a.Length != 5 || !ProperList(a[4])) throw GenServerSource.Failure(
            c,
            GenServerFunctionNames.FormatStatus,
            2,
            GenServerSourceLines.FormatStatusLine3082,
            Term.Tuple(A(GenServerProtocolAtoms.Badmatch), args[1])
        );
        Term[] state = Cons.Items(a[4]).ToArray();
        if (state.Length != 4 || state[0] is not TupleTerm { Items.Count: GenServerRecordLayout.ServerData.Arity } data || !Is(data.Items[0], GenServerProtocolAtoms.ServerData) || !data.Items[GenServerRecordLayout.ServerData.Parent].Equals(a[2]))
            throw GenServerSource.Failure(
                c,
                GenServerFunctionNames.FormatStatus,
                2,
                GenServerSourceLines.FormatStatusLine3082,
                Term.Tuple(A(GenServerProtocolAtoms.Badmatch), args[1])
            );
        var m = Restore(c, a[4], a[3]);
        Term header = await m.Call(
            GenServerDependencyNames.Gen,
            GenServerFunctionNames.FormatStatusHeader,
            Term.String(GenServerStatusNames.StatusForGenericServer),
            m.name
        );
        var formatted = (MapTerm)await m.Call(
            GenServerDependencyNames.Gen,
            GenServerFunctionNames.FormatStatus,
            m.module,
            args[0],
            Map(
                (GenServerStatusNames.State, m.state),
                (GenServerStatusNames.Log, await m.Call(GenServerDependencyNames.Sys, GenServerFunctionNames.GetLog, m.debug))
            ),
            Term.List(a[0], m.state)
        );
        Term details;
        if (formatted.TryGet(A(GenServerStatusNames.ExitSignal), out Term? crash)) details = Term.List(Term.Tuple(A(GenServerProtocolAtoms.Data), Term.List(Term.Tuple(Term.String(GenServerStatusNames.StateText), crash!))));
        else if (formatted.TryGet(A(GenServerStatusNames.LegacyStatus), out Term? old)) details = List(old!) ? old! : Term.List(old!);
        else details = Term.List(Term.Tuple(
            A(GenServerProtocolAtoms.Data),
            Term.List(Term.Tuple(Term.String(GenServerStatusNames.StateText), formatted.Get(A(GenServerStatusNames.State))))
        ));

        return Cons.From(
            [Term.Tuple(A(GenServerProtocolAtoms.Header),header),Term.Tuple(
                A(GenServerProtocolAtoms.Data),
                Term.List(
                    Term.Tuple(Term.String(GenServerStatusNames.Status),a[1]),
                    Term.Tuple(Term.String(GenServerStatusNames.Parent),a[2]),
                    Term.Tuple(Term.String(GenServerStatusNames.LoggedEvents),await m.FormatLogState(formatted.Get(A(GenServerStatusNames.Log))))
                )
            )],
            details
        );
    }
}
