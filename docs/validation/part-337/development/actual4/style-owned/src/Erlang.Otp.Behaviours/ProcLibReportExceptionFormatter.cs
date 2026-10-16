using System.Numerics;
using System.Text;

namespace Erlang.Otp;

internal sealed partial class ProcLibReportExceptionFormatter(ProcessContext context, IProcLibReportTermPrinter printer)
{
    private readonly ProcLibReportFormatControls controls = new(printer);

    public async ValueTask<Term> Render(
        Term exceptionClass,
        Term reason,
        Term stackTrace,
        Term encoding,
        Term depth,
        Term charsLimit
    )
    {
        BigInteger limit = charsLimit is Integer integer ? integer.Value : ProcLibReportExceptionFormatNames.Unlimited;
        var analysis = ProcLibReportExceptionAnalyzer.Analyze(exceptionClass, reason, stackTrace);
        BigInteger stackLimit = limit.Sign < 0 ? limit : limit / ProcLibReportExceptionFormatNames.StackBudgetDivisor;
        Term errorMap = await ExtendedError(reason, analysis.StackFrames);
        Term stack = await FormatStack(
            analysis.StackFrames,
            encoding,
            depth,
            stackLimit,
            errorMap
        );
        Term prefix = Exited(exceptionClass);
        BigInteger remaining = Subtract(Subtract(limit, Measure(prefix, ProcLibReportFormatAtoms.Latin1)), Measure(stack, encoding));
        Term defaultReason = await Explain(
            analysis.Reason,
            exceptionClass,
            analysis.ReasonFrames,
            encoding,
            depth,
            remaining
        );
        if (errorMap is not MapTerm errors)
            throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.BadMap), errorMap));
        Term explanation = errors.TryGet(Term.A(ProcLibReportExceptionFormatNames.Reason), out var replacement) ? replacement! : defaultReason;
        Term text = TextFormat(
            encoding.Equals(ProcLibReportFormatAtoms.Latin1)
                ? ProcLibReportExceptionFormatNames.Latin1PairTextControl
                : ProcLibReportExceptionFormatNames.Latin1UnicodePairTextControl,
            prefix,
            explanation
        );

        return stack is Nil ? text : Term.List(text, Term.I('\n'), stack);
    }

    private async ValueTask<Term> ExtendedError(Term reason, IReadOnlyList<TupleTerm> stack)
    {
        if (stack.Count == 0)
            return EmptyMap();
        TupleTerm frame = stack[0];
        Term entry = KeyFind(Term.A(ProcLibReportExceptionFormatNames.ErrorInfo), frame.Items[3]);
        if (entry is not TupleTerm { Items: [_, MapTerm information] })
            return EmptyMap();
        Term module = information.TryGet(Term.A(ProcLibReportExceptionFormatNames.Module), out var requestedModule) ? requestedModule! : frame.Items[0];
        Term function = information.TryGet(Term.A(ProcLibReportExceptionFormatNames.Function), out var requestedFunction)
            ? requestedFunction! : Term.A(ProcLibReportExceptionFormatNames.FormatError);
        try
        {
            if (module is not Atom moduleAtom || function is not Atom functionAtom)
                throw new ErlangException(ErlangErrorReasons.BadArgument);

            return await context.Runtime.Modules.Call(
                context,
                moduleAtom.Name,
                functionAtom.Name,
                reason,
                Cons.From(stack)
            );
        }
        catch (ErlangException failure) when (failure.ExceptionClass == ErlangExceptionClasses.Error)
        {
            return EmptyMap();
        }
    }

    private async ValueTask<Term> FormatStack(
        IReadOnlyList<TupleTerm> original,
        Term encoding,
        Term depth,
        BigInteger limit,
        Term errorMap
    )
    {
        var output = new List<Term>();
        int number = 1;
        foreach (TupleTerm frame in ProcLibReportExceptionAnalyzer.Trim(original))
        {
            if (limit.IsZero)
                break;
            Term module = frame.Items[0];
            Term function = frame.Items[1];
            Term arguments = frame.Items[2];
            Term separator = number == 1 ? StackIndent()
                : new Cons(Term.I('\n'), StackIndent());
            Term origin = B(number == 1
                ? IsOperator(module, function, Arity(arguments)) ? ProcLibReportExceptionFormatNames.InOperator : ProcLibReportExceptionFormatNames.InFunction
                : ProcLibReportExceptionFormatNames.InCallFrom);
            Term text;
            if (arguments is Integer)
            {
                Term formattedError = await FormatArgumentErrors(errorMap, arguments, separator);
                text = TextFormat(
                    ProcLibReportExceptionFormatNames.ArityFrameControl,
                    separator,
                    origin,
                    Mfa(
                        module,
                        function,
                        Arity(arguments),
                        encoding
                    ),
                    Location(frame.Items[3]),
                    formattedError
                );
            }
            else
            {
                Term calledAs = Term.List(StackIndent(), B(ProcLibReportExceptionFormatNames.CalledAs));
                Term call = await FormatCall(
                    Nil.Value,
                    calledAs,
                    Term.Tuple(module, function),
                    arguments,
                    encoding,
                    depth,
                    limit
                );
                Term formattedError = await FormatArgumentErrors(errorMap, arguments, separator);
                text = TextFormat(
                    ProcLibReportExceptionFormatNames.ArgumentsFrameControl,
                    separator,
                    origin,
                    Mfa(
                        module,
                        function,
                        Arity(arguments),
                        encoding
                    ),
                    calledAs,
                    call,
                    formattedError
                );
            }
            output.Add(text);
            limit = Subtract(limit, Measure(text, encoding));
            errorMap = EmptyMap();
            number++;
        }

        return Cons.From(output);
    }

    private async ValueTask<Term> FormatArgumentErrors(Term errorMap, Term arguments, Term separator)
    {
        if (errorMap is not MapTerm map)
            throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.CaseClause), errorMap));
        BigInteger count = Arity(arguments);
        if (count.Sign < 0)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        var errors = new List<Term>();
        for (BigInteger number = 1; number <= count; number++)
        {
            await context.ReduceAsync();
            if (!map.TryGet(new Integer(number), out var explanation))
                continue;
            string text = ProcLibReportCharacters.FlattenUnicode(explanation!);
            string[] lines = text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length == 0)
                throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.BadMatch), Nil.Value));
            Term prefix = TextFormat(ProcLibReportExceptionFormatNames.ArgumentLabelControl, new Integer(number));
            errors.Add(TextFormat(ProcLibReportExceptionFormatNames.PairTextControl, prefix, S(lines[0])));
            foreach (string line in lines.Skip(1))
                errors.Add(TextFormat(
                    ProcLibReportExceptionFormatNames.PairTextControl,
                    Term.List(Padding(checked((int)Measure(prefix, Term.A(ProcLibReportControlNames.Unicode))))),
                    S(line)
                ));
        }
        if (map.TryGet(Term.A(ProcLibReportExceptionFormatNames.General), out var general))
            errors.Add(TextFormat(ProcLibReportExceptionFormatNames.GeneralErrorControl, general!));

        return errors.Count == 0 ? Nil.Value : Join(
            Term.List(Term.I('\n'), separator, B(ProcLibReportExceptionFormatNames.ThreeSpaces)),
            new[] { (Term)Nil.Value }.Concat(errors)
        );
    }

    private Term Location(Term information)
    {
        Term file = Property(ProcLibReportExceptionFormatNames.File, information);
        Term line = Property(ProcLibReportExceptionFormatNames.Line, information);

        return file.Equals(ProcLibReportFormatAtoms.Undefined) || line.Equals(ProcLibReportFormatAtoms.Undefined)
            ? Nil.Value
            : TextFormat(ProcLibReportExceptionFormatNames.LocationControl, file, printer.Write(line, ProcLibReportFormatAtoms.Latin1));
    }

    private static Term Property(string name, Term list)
    {
        Term key = Term.A(name);
        while (list is Cons item)
        {
            if (item.Head.Equals(key))
                return ProcLibReportFormatAtoms.True;
            if (item.Head is TupleTerm tuple && tuple.Items.Count >= 1 && tuple.Items[0].Equals(key))
                return tuple.Items.Count == 2 ? tuple.Items[1] : ProcLibReportFormatAtoms.Undefined;
            list = item.Tail;
        }
        if (list is not Nil)
            throw new ErlangException(ErlangErrorReasons.FunctionClause);

        return ProcLibReportFormatAtoms.Undefined;
    }

    private static Term KeyFind(Term key, Term list)
    {
        while (list is Cons item)
        {
            if (item.Head is TupleTerm tuple && tuple.Items.Count > 0 && tuple.Items[0].Equals(key))
                return tuple;
            list = item.Tail;
        }
        if (list is not Nil)
            throw new ErlangException(ErlangErrorReasons.BadArgument);

        return ProcLibReportFormatAtoms.False;
    }

    private static Term Exited(Term exceptionClass)
        => exceptionClass is Atom atom ? B(atom.Name switch
        {
            ErlangExceptionClasses.Error => ProcLibReportExceptionFormatNames.ErrorPrefix,
            ErlangExceptionClasses.Exit => ProcLibReportExceptionFormatNames.ExitPrefix,
            ErlangExceptionClasses.Throw => ProcLibReportExceptionFormatNames.ThrowPrefix,
            _ => throw new ErlangException(ErlangErrorReasons.FunctionClause)
        }) : throw new ErlangException(ErlangErrorReasons.FunctionClause);

    private Term TextFormat(string control, params Term[] arguments)
        => controls.Format(control, arguments, ProcLibReportFormatAtoms.Unlimited);

    private (
        Term Text,
        BigInteger Remaining) Pretty(
            Term value,
            int column,
            Term encoding,
            Term depth,
            BigInteger limit
        )
    {
        Term rendered = printer.Render(
            value,
            depth.Equals(ProcLibReportFormatAtoms.Unlimited) ? Term.I(-1) : depth,
            encoding,
            new Integer(limit),
            ProcLibReportExceptionFormatNames.DefaultWidth,
            column
        );

        Term text = Term.List(rendered);

        return (text, Subtract(limit, Measure(text, encoding)));
    }

    private static Term S(string text) => Term.String(text);

    private static Term B(string text) => new BitString(Encoding.UTF8.GetBytes(text));

    private static Term Spaces(int count) => S(new string(' ', count));

    private static Term StackIndent()
        => new Cons(S(ProcLibReportExceptionFormatNames.StackIndentPrefix), S(ProcLibReportExceptionFormatNames.ReasonIndent));

    private static Term Padding(int count)
    {
        if (count <= ProcLibReportExceptionFormatNames.FlatPaddingMaximum)
            return Spaces(count);
        Term half = Padding(count / ProcLibReportExceptionFormatNames.PaddingDivisor);

        return count % ProcLibReportExceptionFormatNames.PaddingDivisor == 0
            ? new Cons(half, half)
            : new Cons(Term.I(ProcLibReportExceptionFormatNames.PaddingCharacter), new Cons(half, half));
    }


    private static string Digits(BigInteger value) => value.ToString(System.Globalization.CultureInfo.InvariantCulture);

    private static MapTerm EmptyMap() => new([]);

    private static BigInteger Measure(Term value, Term encoding) => ProcLibReportCharacters.Measure(value, encoding);

    private static BigInteger Subtract(BigInteger limit, BigInteger used) => limit.Sign < 0 ? limit : BigInteger.Max(limit - used, BigInteger.Zero);

    private static BigInteger Arity(Term value) => value is Integer integer ? integer.Value : Cons.Items(value).Count();

    private static bool IsOperator(Term module, Term function, BigInteger arity)
        => module is Atom { Name: ProcLibNames.Erlang } && function is Atom atom && arity >= int.MinValue && arity <= int.MaxValue && ProcLibReportExceptionCatalog.Operators.Contains((atom.Name, (int)arity));

    private static Term Join(Term separator, IEnumerable<Term> values)
    {
        var output = new List<Term>();
        foreach (Term value in values)
        {
            if (output.Count > 0)
                output.Add(separator);
            output.Add(value);
        }

        return Cons.From(output);
    }
}
