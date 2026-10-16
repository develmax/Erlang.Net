using System.Numerics;

namespace Erlang.Otp;

internal sealed class ProcLibReportFormatting(IProcLibReportPrimitives primitives)
{
    public ValueTask<Term> Format(Term report)
        => Format(report, ProcLibReportFormatAtoms.Latin1, ProcLibReportFormatAtoms.Unlimited);

    public ValueTask<Term> Format(Term report, Term encoding)
        => Format(report, encoding, ProcLibReportFormatAtoms.Unlimited);

    public ValueTask<Term> Format(Term report, Term encoding, Term depth)
        => DoFormat(report, new(
            encoding,
            depth,
            ProcLibReportFormatAtoms.False,
            ProcLibReportFormatAtoms.Unlimited
        ));

    public ValueTask<Term> ReportCallback(Term report, Term extra)
    {
        if (report is not MapTerm map ||
            !map.TryGet(ProcLibReportFormatAtoms.Label, out var label) ||
            !label!.Equals(ProcLibReportFormatAtoms.CrashLabel) ||
            !map.TryGet(ProcLibReportFormatAtoms.Report, out var crashReport))
            throw new ErlangException(ErlangErrorReasons.FunctionClause);
        if (extra is not MapTerm options)
            throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.BadMap), extra));

        return DoFormat(crashReport!, ProcLibReportFormatOptions.Merge(options));
    }

    private async ValueTask<Term> DoFormat(Term report, ProcLibReportFormatOptions options)
    {
        if (report is not Cons { Head: var own, Tail: Cons { Head: var links, Tail: Nil } })
            throw new ErlangException(ErlangErrorReasons.FunctionClause);
        bool single = options.IsSingleLine;
        string indent = single ? ProcLibReportFormatNames.Empty : ProcLibReportFormatNames.Indent;
        string newline = Newline(single, ProcLibReportFormatNames.Space);
        string separator = Newline(single, ProcLibReportFormatNames.ReportSeparator);
        Term partLimit = options.CharsLimit;
        Term limit = options.CharsLimit;
        if (limit is Integer integer)
        {
            int count = Cons.Items(own).Count();
            int hardcoded = indent.Length + ProcLibReportFormatNames.CrasherBudgetHeading.Length +
                newline.Length + separator.Length + separator.Length * count;
            BigInteger remaining = BigInteger.Max(integer.Value - hardcoded, ProcLibReportFormatNames.MinimumBudget);
            BigInteger exceptionLimit = remaining / ProcLibReportFormatNames.BudgetParts;
            if (count == 0)
                throw new ErlangException(ErlangErrorReasons.BadArithmetic);
            partLimit = new Integer((remaining - exceptionLimit) / count);
            limit = new Integer(remaining);
        }
        else if (!limit.Equals(ProcLibReportFormatAtoms.Unlimited))
        {
            throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.CaseClause), limit));
        }
        Term linkFormat = FormatLinks(
            links,
            Term.String(indent),
            options,
            partLimit
        );
        BigInteger linkSize = primitives.Measure(linkFormat, options.Encoding);
        Term ownFormat = await FormatOwn(
            own,
            Term.String(indent + indent),
            options,
            linkSize,
            partLimit,
            limit
        );

        return primitives.Format(
            ProcLibReportFormatNames.TextControl + ProcLibReportFormatNames.CrasherHeading + newline +
                ProcLibReportFormatNames.UnicodeTextControl + separator + ProcLibReportFormatNames.UnicodeTextControl,
            [Term.String(indent), ownFormat, linkFormat],
            ProcLibReportFormatAtoms.Unlimited
        );
    }

    private async ValueTask<Term> FormatOwn(
        Term own,
        Term indent,
        ProcLibReportFormatOptions options,
        BigInteger linkSize,
        Term partLimit,
        Term limit
    )
    {
        if (SeparateErrorInfo(own) is not { } split)
            return FormatReport(
                own,
                indent,
                options,
                SubtractLimit(limit, linkSize)
            );
        if (split.Error is not TupleTerm { Items: [var exceptionClass, var reason, var stack] })
            throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.CaseClause), Term.Tuple(split.First, split.Error, split.Remainder)));
        Term first = FormatReport(
            split.First,
            indent,
            options,
            partLimit
        );
        Term rest = FormatReport(
            split.Remainder,
            indent,
            options,
            partLimit
        );
        string separator = Newline(options.IsSingleLine, ProcLibReportFormatNames.PartSeparator);
        BigInteger otherSize = primitives.Measure(first, options.Encoding) + primitives.Measure(rest, options.Encoding) -
            separator.Length * (Cons.Items(first).Count() + Cons.Items(rest).Count()) + linkSize;
        Term error = await FormatException(
            exceptionClass,
            reason,
            stack,
            options,
            SubtractLimit(limit, otherSize)
        );

        return Join(separator, [first, error, rest]);
    }

    private static (Term First, Term Error, Term Remainder)? SeparateErrorInfo(Term report)
    {
        var first = new List<Term>();
        Term cursor = report;
        while (cursor is Cons item)
        {
            if (item.Head is not TupleTerm tuple || tuple.Items.Count == 0)
                return null;
            if (tuple.Items[0].Equals(ProcLibReportFormatAtoms.ErrorInfo))
            {
                if (tuple.Items is [_, var error])
                    return (Cons.From(first), error, item.Tail);

                return null;
            }
            first.Add(item.Head);
            cursor = item.Tail;
        }

        return null;
    }

    private Term FormatLinks(
        Term links,
        Term indent,
        ProcLibReportFormatOptions options,
        Term partLimit
    )
    {
        if (partLimit is Integer)
        {
            var pids = new List<Term>();
            Term cursor = links;
            while (cursor is Cons item)
            {
                if (item.Head is TupleTerm { Items: [var tag, Cons { Head: TupleTerm { Items: [var pidTag, var pid] } }] } &&
                    tag.Equals(ProcLibReportFormatAtoms.Neighbour) && pidTag.Equals(ProcLibReportFormatAtoms.Pid))
                    pids.Add(pid);
                cursor = item.Tail;
            }
            if (cursor is not Nil)
                throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.BadGenerator), cursor));
            string width = options.IsSingleLine ? ProcLibReportFormatNames.ZeroWidth : ProcLibReportFormatNames.Empty;

            return primitives.Format(
                Term.Text(indent) + ProcLibReportFormatNames.NeighboursControl + width + PrettyControl(options),
                PrettyArguments(Cons.From(pids), options),
                partLimit
            );
        }
        Term reports = FormatLinkReports(
            links,
            Term.String(Term.Text(indent) + Term.Text(indent)),
            options,
            partLimit
        );
        Term joined = Join(Newline(options.IsSingleLine, ProcLibReportFormatNames.ReportSeparator), Cons.Items(reports));

        return Term.List(
            indent,
            Term.String(ProcLibReportFormatNames.NeighboursHeading),
            Term.String(Newline(options.IsSingleLine, ProcLibReportFormatNames.Space)),
            joined
        );
    }

    private Term FormatLinkReports(
        Term links,
        Term indent,
        ProcLibReportFormatOptions options,
        Term limit
    )
    {
        var result = new List<Term>();
        Term cursor = links;
        while (cursor is Cons item)
        {
            Term report = item.Head is TupleTerm { Items: [var tag, var value] } && tag.Equals(ProcLibReportFormatAtoms.Neighbour)
                ? value
                : item.Head;
            indent = options.IsSingleLine ? Nil.Value : indent;
            Term linkIndent = Term.List(Term.String(ProcLibReportFormatNames.Indent), indent);
            result.Add(Term.List(
                indent,
                Term.String(ProcLibReportFormatNames.NeighbourHeading),
                Term.String(Newline(options.IsSingleLine, ProcLibReportFormatNames.Space)),
                FormatReport(
                    report,
                    linkIndent,
                    options,
                    limit
                )
            ));
            cursor = item.Tail;
        }
        Term tail = FormatReport(
            cursor,
            indent,
            options,
            limit
        );
        for (int index = result.Count - 1; index >= 0; index--)
            tail = new Cons(result[index], tail);

        return tail;
    }

    private Term FormatReport(
        Term report,
        Term indent,
        ProcLibReportFormatOptions options,
        Term limit
    )
    {
        if (report is Cons or Nil)
        {
            var parts = new List<Term>();
            while (report is Cons { Head: TupleTerm { Items: [var tag, var data] }, Tail: var tail })
            {
                if (tag.Equals(ProcLibReportFormatAtoms.InitialCall))
                    parts.Add(FormatInitialCall(
                        indent,
                        data,
                        options,
                        limit
                    ));
                else if (!tag.Equals(ProcLibReportFormatAtoms.ProcessLabel) || !data.Equals(ProcLibReportFormatAtoms.Undefined))
                    parts.Add(FormatTag(
                        indent,
                        tag,
                        data,
                        options,
                        limit
                    ));
                report = tail;
            }

            return Join(Newline(options.IsSingleLine, ProcLibReportFormatNames.PartSeparator), parts);
        }
        string width = options.IsSingleLine ? ProcLibReportFormatNames.ZeroWidth : ProcLibReportFormatNames.Empty;
        Term prefix = options.IsSingleLine ? Nil.Value : indent;

        return primitives.Format(
            ProcLibReportFormatNames.TextControl + ProcLibReportFormatNames.ControlPrefix + width + PrettyControl(options),
            new[] { prefix }.Concat(PrettyArguments(report, options)).ToArray(),
            limit
        );
    }

    private Term FormatInitialCall(
        Term indent,
        Term initialCall,
        ProcLibReportFormatOptions options,
        Term limit
    )
    {
        if (initialCall is not TupleTerm { Items: [var module, var function, var arguments] })
            throw new ErlangException(ErlangErrorReasons.FunctionClause);
        indent = options.IsSingleLine ? Nil.Value : indent;
        try
        {
            int arity = Cons.Items(arguments).Count();

            return Term.List(
                indent,
                Term.String(ProcLibReportFormatNames.InitialCallHeading),
                primitives.WriteAtom(module, options.Encoding),
                Term.I(ProcLibReportFormatNames.ModuleSeparatorCharacter),
                primitives.WriteAtom(function, options.Encoding),
                Term.I(ProcLibReportFormatNames.AritySeparatorCharacter),
                Term.String(arity.ToString(System.Globalization.CultureInfo.InvariantCulture))
            );
        }
        catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Error)
        {
            return FormatTag(
                indent,
                ProcLibReportFormatAtoms.InitialCall,
                initialCall,
                options,
                limit
            );
        }
    }

    private Term FormatTag(
        Term indent,
        Term tag,
        Term data,
        ProcLibReportFormatOptions options,
        Term limit
    )
    {
        string width = options.IsSingleLine ? ProcLibReportFormatNames.ZeroWidth : ProcLibReportFormatNames.Empty;
        Term prefix = options.IsSingleLine ? Nil.Value : indent;

        return primitives.Format(
            ProcLibReportFormatNames.TextControl + ProcLibReportFormatNames.ControlPrefix + width +
                ProcLibReportFormatNames.TagControlSuffix + width + ProcLibReportFormatNames.TermIndentControl + PrettyControl(options),
            new[] { prefix, tag }.Concat(PrettyArguments(data, options)).ToArray(),
            limit
        );
    }

    private async ValueTask<Term> FormatException(
        Term exceptionClass,
        Term reason,
        Term stack,
        ProcLibReportFormatOptions options,
        Term limit
    )
    {
        if (options.IsSingleLine)
        {
            if (exceptionClass is not Atom atom)
                throw new ErlangException(ErlangErrorReasons.BadArgument);

            return Term.List(
                Term.String(atom.Name),
                Term.String(ProcLibReportFormatNames.ClassSeparator),
                primitives.Format(
                    ProcLibReportFormatNames.ControlPrefix + ProcLibReportFormatNames.ZeroWidth + PrettyControl(options),
                    PrettyArguments(Term.Tuple(reason, stack), options),
                    limit
                )
            );
        }

        return Term.List(
            Term.String(ProcLibReportFormatNames.ExceptionIndent),
            await primitives.FormatException(
                exceptionClass,
                reason,
                stack,
                options.Encoding,
                options.Depth,
                limit
            )
        );
    }

    private static string PrettyControl(ProcLibReportFormatOptions options)
        => (options.Encoding.Equals(ProcLibReportFormatAtoms.Latin1) ? ProcLibReportFormatNames.Empty : ProcLibReportFormatNames.UnicodeModifier) +
            (options.Depth.Equals(ProcLibReportFormatAtoms.Unlimited) ? ProcLibReportFormatNames.Pretty : ProcLibReportFormatNames.DepthPretty);

    private static IReadOnlyList<Term> PrettyArguments(Term value, ProcLibReportFormatOptions options)
        => options.Depth.Equals(ProcLibReportFormatAtoms.Unlimited) ? [value] : [value, options.Depth];

    private static Term SubtractLimit(Term limit, BigInteger used)
        => limit is Integer integer ? new Integer(BigInteger.Max(integer.Value - used, ProcLibReportFormatNames.MinimumBudget)) : limit;

    private static string Newline(bool single, string alternative)
        => single ? alternative : ProcLibReportFormatNames.Newline;

    private static Term Join(string separator, IEnumerable<Term> values)
    {
        var output = new List<Term>();
        foreach (Term value in values)
        {
            if (output.Count > 0)
                output.Add(Term.String(separator));
            output.Add(value);
        }

        return Cons.From(output);
    }
}
