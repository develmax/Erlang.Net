// SPDX-License-Identifier: Apache-2.0
// Copyright Ericsson AB 2016-2026. All Rights Reserved.
// Modified: OTP-29.1.1 gen_statem formatting and legacy report limits ported to C#.
using System.Text;
using static Erlang.GenStatemMachine;
using static Erlang.GenStatemModule;

namespace Erlang;

internal static class GenStatemLog
{
    internal static async ValueTask<Term> Format(ProcessContext context, Term[] arguments)
    {
        Term reportValue = arguments[0];
        var options = new Dictionary<Term, Term>
        {
            [Term.A(GenStatemStatusNames.CharsLimit)] = Term.A(GenStatemProtocolAtoms.Unlimited),
            [Term.A(GenStatemStatusNames.Depth)] = Term.A(GenStatemProtocolAtoms.Unlimited),
            [Term.A(GenStatemStatusNames.SingleLine)] = Term.A(GenStatemProtocolAtoms.False),
            [Term.A(GenStatemStatusNames.Encoding)] = Term.A(GenStatemProtocolAtoms.Utf8)
        };
        if (arguments.Length == 1)
        {
            Term depth = await Call(context, GenStatemDependencyNames.ErrorLogger, GenStatemFunctionNames.GetFormatDepth);
            options[Term.A(GenStatemStatusNames.Depth)] = depth;
            if (!Is(depth, GenStatemProtocolAtoms.Unlimited))
            {
                reportValue = await Limit(context, (MapTerm)reportValue, depth);
            }
        }
        else
        {
            MapTerm merged;
            using (GenStatemSource.Scope(
                context,
                GenStatemFunctionNames.FormatLog,
                2,
                5098
            ))
            {
                merged = (MapTerm)await Call(
                    context,
                    GenStatemDependencyNames.Maps,
                    GenStatemFunctionNames.Merge,
                    new MapTerm(options),
                    arguments[1]
                );
            }
            foreach (var entry in merged.Entries)
            {
                options[entry.Key] = entry.Value;
            }
        }

        Term Option(string key) => options[Term.A(key)];
        bool single = Is(Option(GenStatemStatusNames.SingleLine), GenStatemProtocolAtoms.True);
        string[] required = single
            ? [GenStatemStatusNames.Label, GenStatemStatusNames.Name, GenStatemStatusNames.Queue, GenStatemStatusNames.State, GenStatemStatusNames.Log, GenStatemStatusNames.Reason, GenStatemStatusNames.ClientInfo, GenStatemStatusNames.ProcessLabel]
            : [GenStatemStatusNames.Label, GenStatemStatusNames.Name, GenStatemStatusNames.Queue, GenStatemStatusNames.Postponed, GenStatemStatusNames.Modules, GenStatemStatusNames.CallbackMode, GenStatemStatusNames.StateEnter, GenStatemStatusNames.State, GenStatemStatusNames.Timeouts, GenStatemStatusNames.Log, GenStatemStatusNames.Reason, GenStatemStatusNames.ClientInfo, GenStatemStatusNames.ProcessLabel];
        if (reportValue is not MapTerm report || !required.All(key => report.TryGet(Term.A(key), out _))
            || !report.Get(Term.A(GenStatemStatusNames.Label)).Equals(Term.Tuple(Term.A(Name), Term.A(GenStatemProtocolAtoms.Terminate)))
            || report.Get(Term.A(GenStatemStatusNames.Reason)) is not TupleTerm { Items.Count: 3 })
        {
            using var source = arguments.Length == 2 ? GenStatemSource.Scope(
                context,
                GenStatemFunctionNames.FormatLog,
                2,
                5106
            ) : null;
            throw GenStatemSource.Error(
                context,
                GenStatemFunctionNames.FormatLogMulti,
                [reportValue, new MapTerm(options)],
                5177,
                ErlangErrorReasons.FunctionClause
            );
        }

        Term Get(string key) => report.Get(Term.A(key));
        Term limit = Option(GenStatemStatusNames.Depth);
        bool bounded = !Is(limit, GenStatemProtocolAtoms.Unlimited);
        string p = GenStatemLogFormats.Value + (single ? GenStatemLogFormats.Value0 : "") + (Is(Option(GenStatemStatusNames.Encoding), GenStatemProtocolAtoms.Latin1) ? "" : GenStatemLogFormats.T) + (bounded ? GenStatemLogFormats.P : GenStatemLogFormats.P2);
        var text = new StringBuilder();
        var args = new List<Term>();

        void Add(string format, params Term[] values)
        {
            text.Append(format);
            foreach (Term value in values)
            {
                args.Add(value);
                if (bounded)
                {
                    args.Add(limit);
                }
            }
        }
        var reason = (TupleTerm)Get(GenStatemStatusNames.Reason);
        Term fixedReason = reason.Items[1];
        Term stack = reason.Items[2];
        if (Is(reason.Items[0], GenStatemProtocolAtoms.Error) && Is(fixedReason, GenStatemProtocolAtoms.Undef) && stack is Cons { Head: TupleTerm { Items.Count: 4 } frame } frames)
        {
            Term loaded = await Call(
                context,
                GenStatemDependencyNames.Code,
                GenStatemFunctionNames.IsLoaded,
                frame.Items[0]
            );
            if (Is(loaded, GenStatemProtocolAtoms.False))
            {
                fixedReason = Term.Tuple(Term.A(GenStatemProtocolAtoms.ModuleCouldNotBeLoaded), frame.Items[0]);
                stack = frames.Tail;
            }
            else
            {
                Term arity = IsList(frame.Items[2]) ? Term.I(Cons.Items(frame.Items[2]).Count()) : frame.Items[2];
                if (Is(
                    await Call(
                        context,
                        GenStatemProtocolAtoms.Erlang,
                        GenStatemProtocolAtoms.FunctionExported,
                        frame.Items[0],
                        frame.Items[1],
                        arity
                    ),
                    GenStatemProtocolAtoms.False
                ))
                {
                    fixedReason = Term.Tuple(Term.A(GenStatemProtocolAtoms.FunctionNotExported), Term.Tuple(frame.Items[0], frame.Items[1], arity));
                    stack = frames.Tail;
                }
            }
        }
        Term queue = Get(GenStatemStatusNames.Queue);
        Term label = Get(GenStatemStatusNames.ProcessLabel);
        Term log = Get(GenStatemStatusNames.Log);
        if (single)
        {
            Add(GenStatemLogFormats.SingleMachinePrefix + p + GenStatemLogFormats.TerminatingSuffix, Get(GenStatemStatusNames.Name));
            if (!Is(label, GenStatemProtocolAtoms.Undefined))
            {
                Add(GenStatemLogFormats.SingleLabelPrefix + p, label);
            }
            Add(GenStatemLogFormats.SingleReasonPrefix + p, fixedReason);
            if (stack is not Nil)
            {
                Add(GenStatemLogFormats.SingleStackPrefix + p, stack);
            }
            if (queue is Cons q)
            {
                Add(GenStatemLogFormats.SingleLastEventPrefix + p, q.Head);
            }
            Add(GenStatemLogFormats.SingleStatePrefix + p, Get(GenStatemStatusNames.State));
            if (log is not Nil)
            {
                Add(GenStatemLogFormats.SingleLogPrefix + p, log);
            }
            text.Append('.');
        }
        else
        {
            Add(GenStatemLogFormats.MultiMachinePrefix + p + GenStatemLogFormats.TerminatingN, Get(GenStatemStatusNames.Name));
            if (!Is(label, GenStatemProtocolAtoms.Undefined))
            {
                Add(GenStatemLogFormats.MultiLabelPrefix + p + GenStatemLogFormats.N, label);
            }
            if (queue is Cons q)
            {
                Add(GenStatemLogFormats.MultiLastEventPrefix + p + GenStatemLogFormats.N, q.Head);
            }
            Add(GenStatemLogFormats.MultiStatePrefix + p + GenStatemLogFormats.N, Get(GenStatemStatusNames.State));
            Add(
                GenStatemLogFormats.MultiReasonPrefix + p + GenStatemLogFormats.ReasonSeparator + p + GenStatemLogFormats.N,
                reason.Items[0],
                fixedReason
            );
            Add(GenStatemLogFormats.MultiModulesPrefix + p + GenStatemLogFormats.N, Get(GenStatemStatusNames.Modules));
            Add(
                GenStatemLogFormats.MultiModePrefix + p + GenStatemLogFormats.N,
                Is(Get(GenStatemStatusNames.StateEnter), GenStatemProtocolAtoms.True) ? Term.List(Get(GenStatemStatusNames.CallbackMode), Term.A(GenStatemProtocolAtoms.StateEnter)) : Get(GenStatemStatusNames.CallbackMode)
            );
            if (queue is Cons { Tail: Cons } queued)
            {
                Add(GenStatemLogFormats.MultiQueuePrefix + p + GenStatemLogFormats.N, queued.Tail);
            }
            if (Get(GenStatemStatusNames.Postponed) is not Nil)
            {
                Add(GenStatemLogFormats.MultiPostponedPrefix + p + GenStatemLogFormats.N, Get(GenStatemStatusNames.Postponed));
            }
            if (stack is not Nil)
            {
                Add(GenStatemLogFormats.StacktraceN + p + GenStatemLogFormats.N, stack);
            }
            Term timeouts = Get(GenStatemStatusNames.Timeouts);
            if (timeouts is not TupleTerm t || t.Items[0] is not Integer n || !n.Value.IsZero)
            {
                Add(GenStatemLogFormats.MultiTimeoutsPrefix + p + GenStatemLogFormats.N, timeouts);
            }
            if (log is not Nil)
            {
                Add(GenStatemLogFormats.LogN + p + GenStatemLogFormats.N, log);
            }
        }
        Term client = Get(GenStatemStatusNames.ClientInfo);
        if (client is TupleTerm { Items.Count: 2 } c)
        {
            if (Is(c.Items[1], GenStatemProtocolAtoms.Dead))
            {
                text.Append(single ? GenStatemLogFormats.Client0pIsDead : GenStatemLogFormats.ClientPIsDeadN);
                args.Add(c.Items[0]);
            }
            else if (Is(c.Items[1], GenStatemProtocolAtoms.Remote))
            {
                text.Append(single ? GenStatemLogFormats.Client0pIsRemoteOnNode0p : GenStatemLogFormats.ClientPIsRemoteOnNodePN);
                args.Add(c.Items[0]);
                args.Add(await Call(
                    context,
                    GenStatemDependencyNames.Erlang,
                    GenStatemFunctionNames.Node,
                    c.Items[0]
                ));
            }
            else
            {
                var caller = (TupleTerm)c.Items[1];
                Term trace = single ? Cons.From(Cons.Items(caller.Items[1]).Take(4)) : caller.Items[1];
                Add(
                    single ? GenStatemLogFormats.SingleClientPrefix + p + GenStatemLogFormats.SingleClientStackPrefix + p + GenStatemLogFormats.Value2 : GenStatemLogFormats.MultiClientPrefix + p + GenStatemLogFormats.StacktraceN2 + p + GenStatemLogFormats.N,
                    caller.Items[0],
                    trace
                );
            }
        }
        Term formatText = Term.String(text.ToString());
        Term formatArgs = Cons.From(args);
        if (arguments.Length == 1)
        {
            return Term.Tuple(formatText, formatArgs);
        }
        Term ioOptions = Is(Option(GenStatemStatusNames.CharsLimit), GenStatemProtocolAtoms.Unlimited) ? Nil.Value : Term.List(Term.Tuple(Term.A(GenStatemProtocolAtoms.CharsLimit), Option(GenStatemStatusNames.CharsLimit)));

        return await Call(
            context,
            GenStatemDependencyNames.IoLib,
            GenStatemFunctionNames.Format,
            formatText,
            formatArgs,
            ioOptions
        );
    }

    private static async ValueTask<MapTerm> Limit(ProcessContext context, MapTerm report, Term depth)
    {
        var values = report.Entries.ToDictionary(pair => pair.Key, pair => pair.Value);

        Term Get(string key) => report.Get(Term.A(key));

        async ValueTask<Term> Cut(Term value) => await Call(
            context,
            GenStatemDependencyNames.IoLib,
            GenStatemFunctionNames.LimitTerm,
            value,
            depth
        );
        Term queue = Get(GenStatemStatusNames.Queue);
        values[Term.A(GenStatemStatusNames.Queue)] = queue is Cons cell ? new Cons(await Cut(cell.Head), await Cut(cell.Tail)) : Nil.Value;
        foreach (string field in new[] { GenStatemStatusNames.Postponed, GenStatemStatusNames.Modules, GenStatemStatusNames.State, GenStatemStatusNames.ProcessLabel })
        {
            Term value = Get(field);
            values[Term.A(field)] = field == GenStatemStatusNames.Postponed && value is Nil ? value : await Cut(value);
        }
        Term timeouts = Get(GenStatemStatusNames.Timeouts);
        values[Term.A(GenStatemStatusNames.Timeouts)] = timeouts is TupleTerm t && t.Items[0] is Integer n && n.Value.IsZero ? timeouts : await Cut(timeouts);
        var log = new List<Term>();
        foreach (Term entry in Cons.Items(Get(GenStatemStatusNames.Log)))
        {
            log.Add(await Cut(entry));
        }
        values[Term.A(GenStatemStatusNames.Log)] = Cons.From(log);
        var reason = (TupleTerm)Get(GenStatemStatusNames.Reason);
        values[Term.A(GenStatemStatusNames.Reason)] = Term.Tuple(reason.Items[0], await Cut(reason.Items[1]), await Cut(reason.Items[2]));
        Term client = Get(GenStatemStatusNames.ClientInfo);
        if (client is TupleTerm { Items.Count: 2 } c && c.Items[1] is TupleTerm { Items.Count: 2 } caller)
        {
            values[Term.A(GenStatemStatusNames.ClientInfo)] = Term.Tuple(c.Items[0], Term.Tuple(caller.Items[0], await Cut(caller.Items[1])));
        }

        return new MapTerm(values);
    }

    internal static async ValueTask<Term> PrintEvent(ProcessContext context, IReadOnlyList<Term> arguments)
    {
        var e = (TupleTerm)arguments[1];
        Term name = arguments[2];
        string tag = ((Atom)e.Items[0]).Name;
        string format;
        Term[] args;
        switch (tag)
        {
            case GenStatemProtocolAtoms.In:
                format = GenStatemLogFormats.DBGTpReceiveTsInStateTpN;
                args = [name, await EventString(context, e.Items[1]), e.Items[2]];
                break;
            case GenStatemProtocolAtoms.Out:
                format = GenStatemLogFormats.DBGTpSendTpToTwN;
                args = [name, e.Items[1], ((TupleTerm)e.Items[2]).Items[0]];
                break;
            case GenStatemProtocolAtoms.Enter:
                format = GenStatemLogFormats.DBGTpEnterTpInStateTpN;
                args = [name, e.Items[1], e.Items[2]];
                break;
            case GenStatemProtocolAtoms.Module:
                format = GenStatemLogFormats.DBGTpModuleTpInStateTpN;
                args = [name, e.Items[1], e.Items[2]];
                break;
            case GenStatemFunctionNames.StartTimer:
                format = GenStatemLogFormats.DBGTpStartTimerTpInStateTpN;
                args = [name, e.Items[1], e.Items[2]];
                break;
            case GenStatemProtocolAtoms.InsertTimeout:
                format = GenStatemLogFormats.DBGTpInsertTimeoutTpInStateTpN;
                args = [name, e.Items[1], e.Items[2]];
                break;
            case GenStatemFunctionNames.Terminate:
                format = GenStatemLogFormats.DBGTpTerminateTpInStateTpN;
                args = [name, e.Items[1], e.Items[2]];
                break;
            default:
                bool same = e.Items[2].Equals(e.Items[3]);
                Term state = await Call(
                    context,
                    GenStatemDependencyNames.IoLib,
                    GenStatemFunctionNames.Format,
                    Term.String(same ? GenStatemLogFormats.Tp : GenStatemLogFormats.TpTp),
                    same ? Term.List(e.Items[2]) : Term.List(e.Items[2], e.Items[3])
                );
                format = GenStatemLogFormats.DBGTpTwTsInStateTsN;
                args = [name, e.Items[0], await EventString(context, e.Items[1]), state];
                break;
        }

        return await Call(
            context,
            GenStatemDependencyNames.Io,
            GenStatemFunctionNames.Format,
            arguments[0],
            Term.String(format),
            Cons.From(args)
        );
    }

    private static ValueTask<Term> EventString(ProcessContext context, Term value)
    {
        var e = (TupleTerm)value;
        if (e.Items[0] is TupleTerm { Items.Count: 2 } call && Is(call.Items[0], GenStatemProtocolAtoms.Call) && call.Items[1] is TupleTerm from)
        {
            return Call(
                context,
                GenStatemDependencyNames.IoLib,
                GenStatemFunctionNames.Format,
                Term.String(GenStatemLogFormats.CallTpFromTw),
                Term.List(e.Items[1], from.Items[0])
            );
        }

        return Call(
            context,
            GenStatemDependencyNames.IoLib,
            GenStatemFunctionNames.Format,
            Term.String(GenStatemLogFormats.TwTp),
            Term.List(e.Items[0], e.Items[1])
        );
    }
}
