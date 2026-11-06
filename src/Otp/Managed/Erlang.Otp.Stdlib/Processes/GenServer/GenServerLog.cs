// SPDX-License-Identifier: Apache-2.0
// Copyright Ericsson AB 1996-2026. All Rights Reserved.
// Modified: OTP-29.1.1 gen_server Logger and legacy error_logger formatting ported to C#.
using System.Text;
using static Erlang.GenServerModule;

namespace Erlang;

internal static class GenServerLog
{
    internal static async ValueTask<Term> Format(ProcessContext c, Term[] a)
    {
        var options = new Dictionary<Term, Term> { [A(GenServerStatusNames.CharsLimit)] = A(GenServerProtocolAtoms.Unlimited), [A(GenServerStatusNames.Depth)] = A(GenServerProtocolAtoms.Unlimited), [A(GenServerStatusNames.SingleLine)] = A(GenServerProtocolAtoms.False), [A(GenServerStatusNames.Encoding)] = A(GenServerProtocolAtoms.Utf8) };
        Term reportValue = a[0];
        if (a.Length == 1)
        {
            Term limit = await Call(c, GenServerDependencyNames.ErrorLogger, GenServerFunctionNames.GetFormatDepth);
            options[A(GenServerStatusNames.Depth)] = limit;
            if (!Is(limit, GenServerProtocolAtoms.Unlimited)) reportValue = await Limit(c, reportValue, limit);
        }
        else
        {
            using var scope = GenServerSource.Scope(
                c,
                GenServerFunctionNames.FormatLog,
                2,
                GenServerSourceLines.FormatLogLine2863
            );
            var merged = (MapTerm)await Call(
                c,
                GenServerDependencyNames.Maps,
                GenServerFunctionNames.Merge,
                new MapTerm(options),
                a[1]
            );
            foreach (var entry in merged.Entries) options[entry.Key] = entry.Value;
        }
        using var reportScope = a.Length == 2 ? GenServerSource.Scope(
            c,
            GenServerFunctionNames.FormatLog,
            2,
            GenServerSourceLines.FormatReport
        ) : null;
        bool single = Is(options[A(GenServerStatusNames.SingleLine)], GenServerProtocolAtoms.True);
        Term depth = options[A(GenServerStatusNames.Depth)];
        bool unlimited = Is(depth, GenServerProtocolAtoms.Unlimited);
        string p = GenServerLogFormats.Directive + (single ? GenServerLogFormats.SingleLineWidth : GenServerLogFormats.Empty) + (Is(options[A(GenServerStatusNames.Encoding)], GenServerProtocolAtoms.Latin1) ? GenServerLogFormats.Empty : GenServerLogFormats.UnicodeModifier) + (unlimited ? GenServerLogFormats.UnlimitedTerm : GenServerLogFormats.LimitedTerm);
        var format = new StringBuilder();
        var values = new List<Term>();

        void Printed(Term value)
        {
            values.Add(value);
            if (!unlimited) values.Add(depth);
        }
        if (reportValue is not MapTerm report || !report.TryGet(A(GenServerStatusNames.Label), out Term? label) || label is not TupleTerm { Items.Count: 2 } kind || !Is(kind.Items[0], Name))
            throw GenServerSource.Clause(
                c,
                single ? GenServerFunctionNames.FormatLogSingle : GenServerFunctionNames.FormatLogMulti,
                [reportValue, new MapTerm(options)],
                single ? GenServerSourceLines.FormatLogSingleLine2874 : GenServerSourceLines.FormatLogMultiLine2931
            );

        if (!Is(options[A(GenServerStatusNames.SingleLine)], GenServerProtocolAtoms.True) && !Is(options[A(GenServerStatusNames.SingleLine)], GenServerProtocolAtoms.False))
        {
            using var formatFrame = GenServerSource.Scope(
                c,
                GenServerFunctionNames.FormatLogMulti,
                2,
                Is(kind.Items[1], GenServerProtocolAtoms.NoHandleInfo) ? GenServerSourceLines.WarningDirective : GenServerSourceLines.TerminateDirective
            );
            using var directiveFrame = GenServerSource.Scope(
                c,
                GenServerFunctionNames.PrintDirective,
                1,
                GenServerSourceLines.PrintDirective
            );
            throw GenServerSource.Clause(
                c,
                GenServerFunctionNames.SingleLine,
                [options[A(GenServerStatusNames.SingleLine)]],
                GenServerSourceLines.SingleLine
            );
        }

        Term Get(string key)
        {
            if (!report.TryGet(A(key), out Term? value)) throw GenServerSource.Clause(
                c,
                single ? GenServerFunctionNames.FormatLogSingle : GenServerFunctionNames.FormatLogMulti,
                [reportValue, new MapTerm(options)],
                single ? GenServerSourceLines.FormatLogSingleLine2874 : GenServerSourceLines.FormatLogMultiLine2931
            );

            return value!;
        }
        if (Is(kind.Items[1], GenServerProtocolAtoms.NoHandleInfo))
        {
            if (single)
            {
                format.Append(GenServerLogFormats.UndefinedHandleInfoIn).Append(p).Append(GenServerLogFormats.UnhandledMessage).Append(p).Append('.');
                Printed(Get(GenServerStatusNames.Module));
                Printed(Get(GenServerStatusNames.Message));
            }
            else
            {
                format.Append(GenServerLogFormats.UndefinedHandleInfoInPNUnhandledMessage).Append(p).Append(GenServerLogFormats.N);
                values.Add(Get(GenServerStatusNames.Module));
                Printed(Get(GenServerStatusNames.Message));
            }
        }
        else if (Is(kind.Items[1], GenServerProtocolAtoms.Terminate))
        {
            Term name = Get(GenServerStatusNames.Name), msg = Get(GenServerStatusNames.LastMessage), state = Get(GenServerStatusNames.State), log = Get(GenServerStatusNames.Log), client = Get(GenServerStatusNames.ClientInfo), processLabel = Get(GenServerStatusNames.ProcessLabel);
            Term reason = await FixReason(c, Get(GenServerStatusNames.Reason));
            if (single)
            {
                format.Append(GenServerLogFormats.GenericServer).Append(p).Append(GenServerLogFormats.Terminating);
                Printed(name);
                if (!Is(processLabel, GenServerProtocolAtoms.Undefined))
                {
                    format.Append(GenServerLogFormats.Label).Append(p);
                    Printed(processLabel);
                }
                format.Append(GenServerLogFormats.Reason).Append(p).Append(GenServerLogFormats.LastMessage).Append(p).Append(GenServerLogFormats.State).Append(p).Append('.');
                Printed(reason);
                Printed(msg);
                Printed(state);
                if (log is not Nil)
                {
                    format.Append(GenServerLogFormats.Log).Append(p);
                    Printed(log);
                }
            }
            else
            {
                format.Append(GenServerLogFormats.GenericServerText).Append(p).Append(GenServerLogFormats.TerminatingText);
                Printed(name);
                if (!Is(processLabel, GenServerProtocolAtoms.Undefined))
                {
                    format.Append(GenServerLogFormats.ProcessLabel).Append(p).Append(GenServerLogFormats.N);
                    Printed(processLabel);
                }
                format.Append(GenServerLogFormats.LastMessageInWas).Append(p).Append(GenServerLogFormats.NWhenServerState).Append(p).Append(GenServerLogFormats.NReasonForTerminationN).Append(p).Append(GenServerLogFormats.N);
                Printed(msg);
                Printed(state);
                Printed(reason);
                if (log is not Nil)
                {
                    format.Append(GenServerLogFormats.LogN);
                    bool first = true;
                    foreach (Term evt in Cons.Items(log))
                    {
                        if (!first) format.Append(GenServerLogFormats.EntrySeparator);
                        format.Append(p);
                        Printed(evt);
                        first = false;
                    }
                    format.Append(GenServerLogFormats.LogEnd);
                }
            }
            if (!Is(client, GenServerProtocolAtoms.Undefined))
            {
                if (client is not TupleTerm { Items.Count: 2 } cl) throw GenServerSource.Clause(
                    c,
                    single ? GenServerFunctionNames.FormatClientLogSingle : GenServerFunctionNames.FormatClientLog,
                    [client, new MapTerm(options)],
                    single ? GenServerSourceLines.FormatClientLogSingleLine3025 : GenServerSourceLines.FormatClientLogLine3045
                );
                if (Is(cl.Items[1], GenServerProtocolAtoms.Dead))
                {
                    format.Append(single ? GenServerLogFormats.Client0pIsDead : GenServerLogFormats.ClientPIsDeadN);
                    values.Add(cl.Items[0]);
                }
                else if (Is(cl.Items[1], GenServerProtocolAtoms.Remote))
                {
                    format.Append(single ? GenServerLogFormats.Client0pIsRemoteOnNode0p : GenServerLogFormats.ClientPIsRemoteOnNodePN);
                    values.Add(cl.Items[0]);
                    values.Add(await Call(
                        c,
                        GenServerDependencyNames.Erlang,
                        GenServerFunctionNames.Node,
                        cl.Items[0]
                    ));
                }
                else if (cl.Items[1] is TupleTerm { Items.Count: 2 } info)
                {
                    Term stack = info.Items[1];
                    if (single) stack = await Call(
                        c,
                        GenServerDependencyNames.Lists,
                        GenServerFunctionNames.Sublist,
                        stack,
                        Term.I(4)
                    );
                    format.Append(single ? GenServerLogFormats.Client : GenServerLogFormats.ClientText).Append(p).Append(single ? GenServerLogFormats.Stacktrace : GenServerLogFormats.StacktraceN).Append(p).Append(single ? GenServerLogFormats.Text : GenServerLogFormats.N);
                    Printed(info.Items[0]);
                    Printed(stack);
                }
                else throw GenServerSource.Clause(
                    c,
                    single ? GenServerFunctionNames.FormatClientLogSingle : GenServerFunctionNames.FormatClientLog,
                    [client, new MapTerm(options)],
                    single ? GenServerSourceLines.FormatClientLogSingleLine3025 : GenServerSourceLines.FormatClientLogLine3045
                );
            }
        }
        else throw GenServerSource.Clause(
            c,
            single ? GenServerFunctionNames.FormatLogSingle : GenServerFunctionNames.FormatLogMulti,
            [reportValue, new MapTerm(options)],
            single ? GenServerSourceLines.FormatLogSingleLine2874 : GenServerSourceLines.FormatLogMultiLine2931
        );
        if (a.Length == 1) return Term.Tuple(Term.String(format.ToString()), Cons.From(values));
        Term ioOptions = Is(options[A(GenServerStatusNames.CharsLimit)], GenServerProtocolAtoms.Unlimited) ? Nil.Value : Term.List(Term.Tuple(A(GenServerStatusNames.CharsLimit), options[A(GenServerStatusNames.CharsLimit)]));
        reportScope?.Dispose();

        return await Call(
            c,
            GenServerDependencyNames.IoLib,
            GenServerFunctionNames.Format,
            Term.String(format.ToString()),
            Cons.From(values),
            ioOptions
        );
    }

    private static async ValueTask<Term> FixReason(ProcessContext c, Term reason)
    {
        if (reason is not TupleTerm { Items.Count: 2 } pair || !Is(pair.Items[0], GenServerProtocolAtoms.Undef) || pair.Items[1] is not Cons stack || stack.Head is not TupleTerm { Items.Count: 4 } frame) return reason;
        if (Is(await Call(
            c,
            GenServerDependencyNames.Code,
            GenServerFunctionNames.IsLoaded,
            frame.Items[0]
        ), GenServerProtocolAtoms.False)) return Term.Tuple(A(GenServerProtocolAtoms.ModuleCouldNotBeLoaded), pair.Items[1]);
        Term exported = await Call(
            c,
            GenServerDependencyNames.Erlang,
            GenServerFunctionNames.FunctionExported,
            frame.Items[0],
            frame.Items[1],
            Term.I(Cons.Items(frame.Items[2]).Count())
        );

        return Is(exported, GenServerProtocolAtoms.True) ? reason : Term.Tuple(A(GenServerProtocolAtoms.FunctionNotExported), pair.Items[1]);
    }

    private static async ValueTask<Term> Limit(ProcessContext c, Term value, Term depth)
    {
        if (value is not MapTerm report || !report.TryGet(A(GenServerStatusNames.Label), out Term? label) || label is not TupleTerm { Items.Count: 2 } kind || !Is(kind.Items[0], Name))
            throw GenServerSource.Clause(
                c,
                GenServerFunctionNames.LimitReport,
                [value, depth],
                GenServerSourceLines.LimitReportLine2830
            );
        var entries = report.Entries.ToDictionary(e => e.Key, e => e.Value);

        async ValueTask Change(string key) => entries[A(key)] = await Call(
            c,
            GenServerDependencyNames.IoLib,
            GenServerFunctionNames.LimitTerm,
            report.Get(A(key)),
            depth
        );
        if (Is(kind.Items[1], GenServerProtocolAtoms.NoHandleInfo)) await Change(GenServerStatusNames.Message);
        else if (Is(kind.Items[1], GenServerProtocolAtoms.Terminate))
        {
            foreach (string key in new[] { GenServerStatusNames.LastMessage, GenServerStatusNames.State, GenServerStatusNames.Reason, GenServerStatusNames.ProcessLabel }) await Change(key);
            var log = new List<Term>();
            foreach (Term evt in Cons.Items(report.Get(A(GenServerStatusNames.Log)))) log.Add(await Call(
                c,
                GenServerDependencyNames.IoLib,
                GenServerFunctionNames.LimitTerm,
                evt,
                depth
            ));
            entries[A(GenServerStatusNames.Log)] = Cons.From(log);
            Term client = report.Get(A(GenServerStatusNames.ClientInfo));
            if (client is TupleTerm { Items.Count: 2 } cl && cl.Items[1] is TupleTerm { Items.Count: 2 } info)
                entries[A(GenServerStatusNames.ClientInfo)] = Term.Tuple(
                    cl.Items[0],
                    Term.Tuple(info.Items[0], await Call(
                        c,
                        GenServerDependencyNames.IoLib,
                        GenServerFunctionNames.LimitTerm,
                        info.Items[1],
                        depth
                    ))
                );
        }
        else throw GenServerSource.Clause(
            c,
            GenServerFunctionNames.LimitReport,
            [value, depth],
            GenServerSourceLines.LimitReportLine2830
        );

        return new MapTerm(entries);
    }
}
