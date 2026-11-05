// Derived from Erlang/OTP 29.1.1 gen_event.erl (Apache-2.0), Ericsson AB.
namespace Erlang;

public static partial class GenEventModule
{
    private static async ValueTask<Term> FormatLog(ProcessContext context, Term[] arguments)
    {
        if (arguments[0] is not MapTerm original)
        {
            throw FunctionClause();
        }
        MapTerm report = original;
        Term unlimited = A(GenEventFormatOptions.Unlimited);
        Term depth = unlimited;
        Term limit = unlimited;
        bool single = false;
        bool latin = false;
        if (arguments.Length == 1)
        {
            depth = await External(context, GenEventDependencyModules.ErrorLogger, GenEventDependencyFunctions.GetFormatDepth);
            if (!depth.Equals(unlimited))
            {
                string[] keys = IsTermination(report)
                    ? [GenEventLogFields.LastMessage, GenEventLogFields.State, GenEventLogFields.Reason, GenEventLogFields.ProcessLabel] : [GenEventLogFields.Message];
                foreach (string key in keys)
                {
                    report = new MapTerm(report.Entries.Append(new KeyValuePair<Term, Term>(
                        A(key),
                        await External(
                            context,
                            GenEventDependencyModules.IoLib,
                            GenEventDependencyFunctions.LimitTerm,
                            Get(report, key),
                            depth
                        )
                    )));
                }
            }
        }
        else
        {
            if (arguments[1] is not MapTerm options)
            {
                throw new ErlangException(Term.Tuple(A(ErlangErrorReasons.BadMap), arguments[1]));
            }
            depth = Get(options, GenEventFormatOptions.Depth, unlimited);
            limit = Get(options, GenEventFormatOptions.CharsLimit, unlimited);
            single = Is(Get(options, GenEventFormatOptions.SingleLine, A(ErlangBooleanAtoms.False)), ErlangBooleanAtoms.True);
            latin = Is(Get(options, GenEventFormatOptions.Encoding, A(GenEventFormatOptions.Utf8)), GenEventFormatOptions.Latin1);
        }
        bool bounded = !depth.Equals(unlimited);
        string p = GenEventLogText.TermDirective(single, latin, bounded);
        var values = new List<Term>();

        void Value(Term value)
        {
            values.Add(value);
            if (bounded)
            {
                values.Add(depth);
            }
        }
        string format;
        if (IsTermination(report))
        {
            Term label = Get(report, GenEventLogFields.ProcessLabel);
            bool hasLabel = !Is(label, GenEventResults.Undefined);
            Term reason = await FixReason(context, Get(report, GenEventLogFields.Reason));
            format = GenEventLogText.Termination(p, single, hasLabel);
            Value(Get(report, GenEventLogFields.Handler));
            Value(Get(report, GenEventLogFields.Name));
            if (hasLabel)
            {
                Value(label);
            }
            Value(Get(report, GenEventLogFields.LastMessage));
            Value(Get(report, GenEventLogFields.State));
            Value(reason);
        }
        else
        {
            if (Get(report, GenEventLogFields.Label) is not TupleTerm { Items.Count: 2 } label || !Is(label.Items[0], Name) || !Is(label.Items[1], GenEventLogNames.MissingInfoLabel))
            {
                throw FunctionClause();
            }
            format = GenEventLogText.MissingInfo(p, single);
            if (single)
            {
                Value(Get(report, GenEventLogFields.Module));
            }
            else
            {
                values.Add(Get(report, GenEventLogFields.Module));
            }
            Value(Get(report, GenEventLogFields.Message));
        }
        Term encodedFormat = Term.String(format);
        Term encodedArguments = Cons.From(values);
        if (arguments.Length == 1)
        {
            return Term.Tuple(encodedFormat, encodedArguments);
        }
        Term ioOptions = limit.Equals(unlimited) ? Nil.Value : Term.List(Term.Tuple(A(GenEventFormatOptions.CharsLimit), limit));

        return await External(
            context,
            GenEventDependencyModules.IoLib,
            GenEventDependencyFunctions.Format,
            encodedFormat,
            encodedArguments,
            ioOptions
        );
    }

    private static bool IsTermination(MapTerm report)
        => Get(report, GenEventLogFields.Label) is TupleTerm { Items.Count: 2 } label
            && Is(label.Items[0], Name) && Is(label.Items[1], GenEventLogNames.TerminationLabel);

    private static async ValueTask<Term> FixReason(ProcessContext context, Term reason)
    {
        if (reason is not TupleTerm { Items.Count: 2 } exit || !Is(exit.Items[0], GenEventProtocol.Exit))
        {
            return reason;
        }
        if (exit.Items[1] is TupleTerm { Items.Count: 2 } undef && Is(undef.Items[0], ErlangErrorReasons.UndefinedFunction)
            && undef.Items[1] is Cons { Head: TupleTerm { Items.Count: GenEventStackFrameLayout.FieldCount } frame })
        {
            Term loaded = await External(
                context,
                GenEventDependencyModules.Code,
                GenEventDependencyFunctions.IsLoaded,
                frame.Items[GenEventStackFrameLayout.ModuleIndex]
            );
            if (Is(loaded, ErlangBooleanAtoms.False))
            {
                return Term.Tuple(A(GenEventLogNames.ModuleUnavailable), undef.Items[1]);
            }
            Term exported = await External(
                context,
                GenEventDependencyModules.Erlang,
                GenEventDependencyFunctions.FunctionExported,
                frame.Items[GenEventStackFrameLayout.ModuleIndex],
                frame.Items[GenEventStackFrameLayout.FunctionIndex],
                Term.I(Cons.Items(frame.Items[GenEventStackFrameLayout.ArgumentsIndex]).Count())
            );

            return Is(exported, ErlangBooleanAtoms.True) ? exit.Items[1] : Term.Tuple(A(GenEventLogNames.FunctionUnavailable), undef.Items[1]);
        }

        return exit.Items[1];
    }
}
