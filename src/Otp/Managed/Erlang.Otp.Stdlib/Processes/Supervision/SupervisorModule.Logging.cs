// SPDX-License-Identifier: Apache-2.0
// Copyright Ericsson AB 1996-2026. All Rights Reserved.
// Modified: OTP supervisor logger report and formatting callbacks.
namespace Erlang;

public static partial class SupervisorModule
{
    private static Term ExtractChild(SupervisorChild child) => Term.List(
        T(
            A(IsList(child.Pid) ? SupervisorProtocol.NbChildren : SupervisorProtocol.Pid),
            IsList(child.Pid) ? Term.I(Items(child.Pid).Length) : child.Pid
        ),
        T(A(SupervisorProtocol.Id), child.Id),
        T(A(SupervisorProtocol.Mfargs), child.Start),
        T(A(SupervisorProtocol.RestartType), child.Restart),
        T(A(SupervisorProtocol.Significant), child.Significant),
        T(A(SupervisorProtocol.Shutdown), child.Shutdown),
        T(A(SupervisorProtocol.ChildType), child.Type)
    );

    private static async ValueTask Report(
        ProcessContext context,
        SupervisorState state,
        string kind,
        Term reason,
        SupervisorChild child,
        string? progressLevel = null,
        bool startup = false
    )
    {
        string level = progressLevel ?? SupervisorProtocol.Error;
        (string function, int arity, int line) = kind switch
        {
            SupervisorProtocol.Progress => (SupervisorProtocol.ReportProgress, 3, level == SupervisorProtocol.Info ? SupervisorSourceLines.ProgressInfo : SupervisorSourceLines.ProgressDebug),
            SupervisorProtocol.ChildTerminated => (SupervisorSourceNames.DoRestart, 3, Is(child.Restart, SupervisorProtocol.Permanent) ? SupervisorSourceLines.PermanentExit : child.Temporary ? SupervisorSourceLines.TemporaryExit : SupervisorSourceLines.TransientExit),
            SupervisorProtocol.Shutdown => (SupervisorSourceNames.Restart, 2, SupervisorSourceLines.RestartIntensity),
            SupervisorProtocol.StartError when startup => (SupervisorSourceNames.StartChildren, 2, SupervisorSourceLines.StartError),
            SupervisorProtocol.StartError => (SupervisorSourceNames.Restart, 3, state.Simple ? SupervisorSourceLines.DynamicStartError : SupervisorSourceLines.StaticStartError),
            _ when IsList(child.Pid) => (SupervisorSourceNames.TerminateDynamicChildren, 1, SupervisorSourceLines.DynamicShutdownError),
            _ => (SupervisorSourceNames.DoTerminate, 2, SupervisorSourceLines.ShutdownError)
        };
        Term location = new MapTerm(new Dictionary<Term, Term>
        {
            [A(SupervisorProtocol.Mfa)] = T(A(Name), A(function), Term.I(arity)),
            [A(SupervisorProtocol.File)] = Term.String(SupervisorSourceNames.FileName),
            [A(SupervisorProtocol.Line)] = Term.I(line)
        });
        if (!Is(await Call(
            context,
            SupervisorProtocol.Logger,
            SupervisorProtocol.Allow,
            A(level),
            A(Name)
        ), SupervisorProtocol.True))
        {
            return;
        }
        Term report = kind == SupervisorProtocol.Progress
            ? Term.List(T(A(Name), state.Name), T(A(SupervisorProtocol.Started), ExtractChild(child)))
            : Term.List(
                T(A(Name), state.Name),
                T(A(SupervisorProtocol.ErrorContext), A(kind)),
                T(A(SupervisorProtocol.Reason), reason),
                T(A(SupervisorProtocol.Offender), ExtractChild(child))
            );
        Term body = new MapTerm(new Dictionary<Term, Term>
        {
            [A(SupervisorProtocol.Label)] = T(A(Name), A(kind)),
            [A(SupervisorProtocol.Report)] = report
        });
        Term callback = await Call(
            context,
            SupervisorProtocol.Erlang,
            SupervisorProtocol.MakeFun,
            A(Name),
            A(SupervisorProtocol.FormatLog),
            Term.I(2)
        );
        Term legacy = await Call(
            context,
            SupervisorProtocol.Erlang,
            SupervisorProtocol.MakeFun,
            A(Name),
            A(SupervisorProtocol.FormatLog),
            Term.I(1)
        );
        Term metadata = new MapTerm(new Dictionary<Term, Term>
        {
            [A(SupervisorProtocol.Domain)] = Term.List(A(SupervisorProtocol.Otp), A(SupervisorProtocol.Sasl)),
            [A(SupervisorProtocol.ReportCb)] = callback,
            [A(SupervisorProtocol.LoggerFormatter)] = new MapTerm([new(
                A(SupervisorProtocol.Title),
                Term.String(kind == SupervisorProtocol.Progress ? SupervisorLogFormats.ProgressTitle : SupervisorLogFormats.SupervisorTitle)
            )]),
            [A(SupervisorProtocol.ErrorLogger)] = new MapTerm(new Dictionary<Term, Term>
            {
                [A(SupervisorProtocol.Tag)] = A(kind == SupervisorProtocol.Progress ? SupervisorProtocol.InfoReport : SupervisorProtocol.ErrorReport),
                [A(SupervisorProtocol.Type)] = A(kind == SupervisorProtocol.Progress ? SupervisorProtocol.Progress : SupervisorProtocol.SupervisorReport),
                [A(SupervisorProtocol.ReportCb)] = legacy
            })
        });
        await Call(
            context,
            SupervisorProtocol.Logger,
            SupervisorProtocol.MacroLog,
            location,
            A(level),
            body,
            metadata
        );
    }

    private static async ValueTask UnexpectedMessage(ProcessContext context, Term message)
    {
        if (Is(
            await Call(
                context,
                SupervisorProtocol.Logger,
                SupervisorProtocol.Allow,
                A(SupervisorProtocol.Error),
                A(Name)
            ),
            SupervisorProtocol.True
        ))
        {
            Term location = new MapTerm(new Dictionary<Term, Term>
            {
                [A(SupervisorProtocol.Mfa)] = T(A(Name), A(SupervisorProtocol.HandleInfo), Term.I(2)),
                [A(SupervisorProtocol.File)] = Term.String(SupervisorSourceNames.FileName),
                [A(SupervisorProtocol.Line)] = Term.I(SupervisorSourceLines.UnexpectedMessage)
            });
            Term metadata = new MapTerm(new Dictionary<Term, Term>
            {
                [A(SupervisorProtocol.Domain)] = Term.List(A(SupervisorProtocol.Otp)),
                [A(SupervisorProtocol.ErrorLogger)] = new MapTerm([new(A(SupervisorProtocol.Tag), A(SupervisorProtocol.Error))])
            });
            await Call(
                context,
                SupervisorProtocol.Logger,
                SupervisorProtocol.MacroLog,
                location,
                A(SupervisorProtocol.Error),
                Term.String(SupervisorLogFormats.UnexpectedMessage),
                Term.List(message),
                metadata
            );
        }
    }

    private static async ValueTask<Term> FormatLog(ProcessContext context, IReadOnlyList<Term> args)
    {
        bool legacy = args.Count == 1;
        Term depth;
        MapTerm options;
        if (legacy)
        {
            depth = await Call(context, SupervisorProtocol.ErrorLogger, SupervisorProtocol.GetFormatDepth);
            options = new MapTerm([]);
        }
        else
        {
            if (args[1] is not MapTerm supplied)
            {
                throw new ErlangException(T(A(SupervisorProtocol.Badmap), args[1]));
            }
            options = supplied;
            depth = Get(options, SupervisorProtocol.Depth, A(SupervisorProtocol.Unlimited));
        }
        if (args[0] is not MapTerm report)
        {
            throw new ErlangException(SupervisorProtocol.FunctionClause);
        }
        Term label = Get(report, SupervisorProtocol.Label, A(SupervisorProtocol.Undefined));
        Term[] rows = Items(Get(report, SupervisorProtocol.Report, Nil.Value));
        if (label is not TupleTerm { Items.Count: 2 } labelPair || !Is(labelPair.Items[0], Name) || rows.Length is not (2 or 4))
        {
            throw new ErlangException(SupervisorProtocol.FunctionClause);
        }
        bool progress = Is(labelPair.Items[1], SupervisorProtocol.Progress);
        string[] keys = progress ? [Name, SupervisorProtocol.Started] : [Name, SupervisorProtocol.ErrorContext, SupervisorProtocol.Reason, SupervisorProtocol.Offender];
        if (rows.Length != keys.Length)
        {
            throw new ErlangException(SupervisorProtocol.FunctionClause);
        }
        var values = new List<Term>();
        for (int i = 0; i < rows.Length; i++)
        {
            var pair = Tuple(rows[i], 2);
            if (!Is(pair.Items[0], keys[i]))
            {
                throw new ErlangException(SupervisorProtocol.FunctionClause);
            }
            values.Add(pair.Items[1]);
        }
        if (legacy && !Is(depth, SupervisorProtocol.Unlimited))
        {
            for (int i = 1; i < values.Count; i++)
            {
                values[i] = await Call(
                    context,
                    SupervisorProtocol.IoLib,
                    SupervisorProtocol.LimitTerm,
                    values[i],
                    depth
                );
            }
        }
        Term singleOption = legacy ? A(SupervisorProtocol.False) : Get(options, SupervisorProtocol.SingleLine, A(SupervisorProtocol.False));
        if (!Is(singleOption, SupervisorProtocol.True, SupervisorProtocol.False))
        {
            throw SupervisorSource.FunctionClause(
                context,
                SupervisorSourceNames.Single,
                [singleOption],
                2487
            );
        }
        bool single = Is(singleOption, SupervisorProtocol.True);
        Term encoding = legacy ? A(SupervisorProtocol.Utf8) : Get(options, SupervisorProtocol.Encoding, A(SupervisorProtocol.Utf8));
        string p = SupervisorLogFormats.Directive + (single ? SupervisorLogFormats.SingleLineWidth : SupervisorLogFormats.Empty) + (Is(encoding, SupervisorProtocol.Latin1) ? SupervisorLogFormats.Empty : SupervisorProtocol.T) + (Is(depth, SupervisorProtocol.Unlimited) ? SupervisorProtocol.UnlimitedPrint : SupervisorProtocol.DepthPrint);
        var formatArgs = new List<Term>();

        void AddPrinted(Term value)
        {
            formatArgs.Add(value);
            if (!Is(depth, SupervisorProtocol.Unlimited))
            {
                formatArgs.Add(depth);
            }
        }
        string format;
        if (!single)
        {
            format = string.Concat(keys.Select(key => SupervisorLogFormats.Indent + (key == Name ? Name : key == SupervisorProtocol.Started ? SupervisorProtocol.Started : key) + SupervisorLogFormats.LabelSeparator + p + SupervisorLogFormats.Newline));
            foreach (Term value in values)
            {
                AddPrinted(value);
            }
        }
        else
        {
            format = progress ? SupervisorLogFormats.Supervisor + p + SupervisorLogFormats.Period : SupervisorLogFormats.Supervisor + p + SupervisorLogFormats.Context + p + SupervisorLogFormats.Reason + p + SupervisorLogFormats.Period;
            foreach (Term value in values.Take(values.Count - 1))
            {
                AddPrinted(value);
            }
            Term[] child = Items(values[^1]);

            Term Find(string key) => child.OfType<TupleTerm>().First(pair => pair.Items.Count == 2 && Is(pair.Items[0], key)).Items[1];
            bool pid = child.OfType<TupleTerm>().Any(pair => pair.Items.Count == 2 && Is(pair.Items[0], SupervisorProtocol.Pid));
            format += pid ? SupervisorLogFormats.ChildPid : SupervisorLogFormats.ChildCount;
            formatArgs.Add(Term.String(progress ? SupervisorLogFormats.Started : SupervisorLogFormats.Offender));
            formatArgs.Add(Find(SupervisorProtocol.Id));
            formatArgs.Add(Find(pid ? SupervisorProtocol.Pid : SupervisorProtocol.NbChildren));
        }
        if (legacy)
        {
            return T(Term.String(format), Cons.From(formatArgs));
        }
        Term limit = Get(options, SupervisorProtocol.CharsLimit, A(SupervisorProtocol.Unlimited));

        return await Call(
            context,
            SupervisorProtocol.IoLib,
            SupervisorProtocol.Format,
            Term.String(format),
            Cons.From(formatArgs),
            Is(limit, SupervisorProtocol.Unlimited) ? Nil.Value : Term.List(T(A(SupervisorProtocol.CharsLimit), limit))
        );
    }
}
