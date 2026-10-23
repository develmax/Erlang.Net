using System.Numerics;

namespace Erlang.Otp;

internal sealed partial class ProcLibReportExceptionFormatter
{
    private async ValueTask<Term> Explain(
        Term reason,
        Term exceptionClass,
        IReadOnlyList<TupleTerm> trace,
        Term encoding,
        Term depth,
        BigInteger limit
    )
    {
        bool error = exceptionClass is Atom { Name: ErlangExceptionClasses.Error };
        bool exit = exceptionClass is Atom { Name: ErlangExceptionClasses.Exit };
        if (error && trace.Count == 0)
        {
            if (reason is Atom simple && ProcLibReportExceptionFormatNames.SimpleReasons.TryGetValue(simple.Name, out string? explanation))
                return B(explanation);
            if (reason is TupleTerm { Items: [Atom tag, var value] })
            {
                if (ProcLibReportExceptionFormatNames.ValueReasons.TryGetValue(tag.Name, out string? prefix))
                    return FormatValue(
                        value,
                        prefix,
                        exceptionClass,
                        encoding,
                        depth,
                        limit
                    );
                if (tag.Name == ErlangErrorReasons.BadArity && value is TupleTerm { Items: [FunctionTerm function, var arguments] })
                    return TextFormat(
                        ProcLibReportExceptionFormatNames.BadArityControl,
                        await FunctionName(function, encoding),
                        ArgumentCount(Cons.Items(arguments).Count())
                    );
                if (tag.Name == ProcLibReportExceptionFormatNames.ArgumentLimit)
                    return TextFormat(ProcLibReportExceptionFormatNames.ArgumentLimitText);
                if (tag.Name == ProcLibReportExceptionFormatNames.Unbound)
                    return TextFormat(
                        ProcLibReportExceptionFormatNames.UnboundPrefix + ProcLibReportExceptionFormatNames.WriteControl + ProcLibReportExceptionFormatNames.UnboundSuffix,
                        value
                    );
                if (tag.Name is ProcLibReportExceptionFormatNames.BadField or ProcLibReportExceptionFormatNames.NoValue)
                {
                    Term formatted = TextFormat(
                        (tag.Name == ProcLibReportExceptionFormatNames.BadField
                        ? ProcLibReportExceptionFormatNames.BadFieldPrefix : ProcLibReportExceptionFormatNames.MissingValuePrefix) + ProcLibReportFormatNames.UnicodeTextControl,
                        FormatRecord(value)
                    );

                    return tag.Name == ProcLibReportExceptionFormatNames.BadField ? B(ProcLibReportCharacters.FlattenUnicode(formatted)) : formatted;
                }
            }
            if (reason is TupleTerm { Items: [Atom { Name: ProcLibReportExceptionFormatNames.ShellUndefined }, var functionName, var arity, _] })
                return TextFormat(
                    ProcLibReportExceptionFormatNames.ShellUndefinedControl,
                    ProcLibReportAtomWriter.Write(functionName, encoding),
                    new Integer(Arity(arity))
                );
        }
        if (error && trace.Count == 1)
        {
            TupleTerm frame = trace[0];
            if (reason is Atom { Name: ErlangErrorReasons.FunctionClause })
            {
                if (frame.Items is [var shellFunction, var shellArity])
                    return new Cons(
                        B(ProcLibReportExceptionFormatNames.NoShellClause),
                        TextFormat(ProcLibReportExceptionFormatNames.ShellFunctionControl, shellFunction, shellArity)
                    );
                if (frame.Items is [var module, var function, var arguments, var location])
                {
                    Term call = await ErrorCall(
                        ProcLibReportExceptionFormatNames.NoFunctionClause,
                        exceptionClass,
                        Term.Tuple(module, function),
                        arguments,
                        encoding,
                        depth,
                        limit
                    );

                    return Append(Term.List(call, Term.I(' ')), Location(location));
                }
            }
            if (reason is Atom { Name: ErlangErrorReasons.UndefinedFunction } && frame.Items is [var missingModule, var missingFunction, var missingArity, _])
                return TextFormat(
                    ProcLibReportExceptionFormatNames.UndefinedFunction + ProcLibReportFormatNames.UnicodeTextControl,
                    Mfa(
                        missingModule,
                        missingFunction,
                        Arity(missingArity),
                        encoding
                    )
                );
        }
        if (exit && trace.Count == 0)
        {
            if (reason is Atom atom)
            {
                string? text = atom.Name switch
                {
                    ProcLibReportExceptionFormatNames.RestrictedStarted => ProcLibReportExceptionFormatNames.RestrictedStartedText,
                    ProcLibReportExceptionFormatNames.RestrictedStopped => ProcLibReportExceptionFormatNames.RestrictedStoppedText,
                    ProcLibReportExceptionFormatNames.CallingSelf => ProcLibReportExceptionFormatNames.CallingSelfText,
                    _ => null
                };
                if (text is not null)
                    return B(text);
            }
            if (reason is TupleTerm { Items: [Atom { Name: ProcLibReportExceptionFormatNames.RestrictedBadReturn }, var returned] })
                return FormatValue(
                    returned,
                    ProcLibReportExceptionFormatNames.RestrictedBadReturnPrefix,
                    exceptionClass,
                    encoding,
                    depth,
                    limit
                );
            if (reason is TupleTerm { Items: [Atom { Name: ProcLibReportExceptionFormatNames.RestrictedDisallowed }, TupleTerm { Items: [var function, var arguments] }] })
                return await ErrorCall(
                    ProcLibReportExceptionFormatNames.RestrictedDisallowedPrefix,
                    exceptionClass,
                    function,
                    arguments,
                    encoding,
                    depth,
                    limit
                );
        }
        if (trace.Count != 0)
            throw new ErlangException(ErlangErrorReasons.FunctionClause);
        int column = ProcLibReportExceptionFormatNames.ExceptionColumn + checked((int)Measure(Exited(exceptionClass), ProcLibReportFormatAtoms.Latin1));

        return Pretty(
            reason,
            column,
            encoding,
            depth,
            limit
        ).Text;
    }

    private Term FormatValue(
        Term value,
        string prefix,
        Term exceptionClass,
        Term encoding,
        Term depth,
        BigInteger limit
    )
    {
        int exited = checked((int)Measure(Exited(exceptionClass), ProcLibReportFormatAtoms.Latin1));
        int column = exited + ProcLibReportExceptionFormatNames.ExceptionColumn + prefix.Length;
        Term first = Pretty(
            value,
            column,
            encoding,
            depth,
            limit
        ).Text;
        int count = CountNewlines(first);
        Term result = first;
        if (count > 1)
        {
            Term second = Pretty(
                value,
                ProcLibReportExceptionFormatNames.ExceptionColumn + exited,
                encoding,
                depth,
                limit
            ).Text;
            if (CountNewlines(second) < count)
                result = Append(Term.List(Term.I('\n'), S(ProcLibReportExceptionFormatNames.ReasonIndent), Spaces(exited)), second);
        }

        return new Cons(B(prefix), result);
    }

    private ValueTask<Term> ErrorCall(
        string error,
        Term exceptionClass,
        Term function,
        Term arguments,
        Term encoding,
        Term depth,
        BigInteger limit
    )
    {
        int exited = checked((int)Measure(Exited(exceptionClass), ProcLibReportFormatAtoms.Latin1));
        Term prefix = new Cons(S(ProcLibReportExceptionFormatNames.ReasonIndent), Spaces(exited));

        return FormatCall(
            B(error),
            prefix,
            function,
            arguments,
            encoding,
            depth,
            limit
        );
    }

    private Term FormatRecord(Term record)
    {
        if (record is not TupleTerm { Items: [TupleTerm { Items: [var module, var name] }, var field] })
            throw new ErlangException(ErlangErrorReasons.FunctionClause);

        return B(ProcLibReportCharacters.FlattenUnicode(TextFormat(
            ProcLibReportExceptionFormatNames.RecordControl,
            field,
            module,
            name
        )));
    }

    private Term ArgumentCount(BigInteger count)
        => count == 0 ? B(ProcLibReportExceptionFormatNames.NoArguments)
            : count == 1 ? B(ProcLibReportExceptionFormatNames.OneArgument)
            : count == 2 ? B(ProcLibReportExceptionFormatNames.TwoArguments)
            : TextFormat(ProcLibReportExceptionFormatNames.ArgumentCountControl, new Integer(count));
}
