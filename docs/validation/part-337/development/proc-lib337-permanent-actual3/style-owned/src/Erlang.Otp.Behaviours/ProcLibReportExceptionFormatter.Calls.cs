using System.Numerics;
using System.Text;

namespace Erlang.Otp;

internal sealed partial class ProcLibReportExceptionFormatter
{
    private Term Mfa(
        Term module,
        Term function,
        BigInteger arity,
        Term encoding
    )
        => TextFormat(ProcLibReportExceptionFormatNames.MfaControl, ModuleFunction(
            module,
            function,
            arity,
            encoding
        ), new Integer(arity));

    private Term ModuleFunction(
        Term module,
        Term function,
        BigInteger arity,
        Term encoding
    )
    {
        if (module is Atom { Name: ProcLibNames.Erlang } && function is Atom functionAtom && arity >= int.MinValue && arity <= int.MaxValue &&
            ProcLibReportExceptionCatalog.Bifs.Contains((functionAtom.Name, (int)arity)))
            return TextFormat(ProcLibReportExceptionFormatNames.WriteControl, function);
        if (IsOperator(module, function, arity))
            return function is Atom { Name: ProcLibReportExceptionFormatNames.Slash }
                ? TextFormat(ProcLibReportExceptionFormatNames.WriteControl, function)
                : S(((Atom)function).Name);

        return TextFormat(ProcLibReportExceptionFormatNames.QualifiedFunctionControl, module, ProcLibReportAtomWriter.Write(function, encoding));
    }

    private async ValueTask<Term> FunctionName(FunctionTerm function, Term encoding)
    {
        async ValueTask<Term> Information(string name)
        {
            Term information = await context.Runtime.Modules.Call(
                context,
                ProcLibNames.Erlang,
                ProcLibReportExceptionFormatNames.FunctionInformation,
                function,
                Term.A(name)
            );
            if (information is not TupleTerm { Items: [_, var value] })
                throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.BadMatch), information));

            return value;
        }
        Term module = await Information(ProcLibReportExceptionFormatNames.Module);
        Term name = await Information(ProcLibReportExceptionFormatNames.Name);
        Term arity = await Information(ProcLibReportExceptionFormatNames.Arity);
        Term type = await Information(ProcLibReportExceptionFormatNames.Type);
        bool local = type is Atom { Name: ProcLibReportExceptionFormatNames.Local };
        if (local && name is Nil)
            return TextFormat(ProcLibReportExceptionFormatNames.WriteControl, function);
        if (local && module is Atom { Name: ProcLibReportExceptionNames.Evaluator })
            return TextFormat(ProcLibReportExceptionFormatNames.InterpretedFunction + ProcLibReportExceptionFormatNames.WriteControl, arity);

        return Mfa(
            module,
            name,
            ((Integer)arity).Value,
            encoding
        );
    }

    private async ValueTask<Term> FormatCall(
        Term error,
        Term prefix,
        Term function,
        Term arguments,
        Term encoding,
        Term depth,
        BigInteger limit
    )
    {
        BigInteger arity = Arity(arguments);
        if (function is TupleTerm { Items: [var operatorModule, var operatorName] } && IsOperator(operatorModule, operatorName, arity))
            return new Cons(error, FormatOperator(
                error,
                prefix,
                (Atom)operatorName,
                Cons.Items(arguments).ToArray(),
                encoding,
                depth,
                limit
            ));
        Term functionText = function switch
        {
            TupleTerm { Items: [var module, var name] } => ModuleFunction(
                module,
                name,
                arity,
                encoding
            ),
            FunctionTerm callback => await FunctionName(callback, encoding),
            _ => TextFormat(ProcLibReportFormatNames.UnicodeTextControl, ProcLibReportAtomWriter.Write(function, encoding))
        };
        int column = checked((int)Measure(Term.List(prefix, error, functionText), Term.A(ProcLibReportControlNames.Unicode)));
        Term first = await Arguments(
            arguments,
            column,
            encoding,
            depth,
            limit
        );
        Term second = await Arguments(
            arguments,
            checked((int)Measure(Term.List(prefix, functionText), Term.A(ProcLibReportControlNames.Unicode))),
            encoding,
            depth,
            limit
        );
        Term probe = await Arguments(
            Term.List(Term.A(ProcLibReportExceptionFormatNames.ProbeFirst), Term.A(ProcLibReportExceptionFormatNames.ProbeSecond)),
            column,
            encoding,
            depth,
            limit
        );
        Term body = CountNewlines(probe) > 0 || CountNewlines(second) < CountNewlines(first)
            ? Term.List(
                Term.I('\n'),
                prefix,
                functionText,
                second
            )
            : Term.List(functionText, first);

        return new Cons(error, body);
    }

    private Term FormatOperator(
        Term error,
        Term prefix,
        Atom operation,
        IReadOnlyList<Term> arguments,
        Term encoding,
        Term depth,
        BigInteger limit
    )
    {
        int start = checked((int)Measure(Term.List(error, prefix), ProcLibReportFormatAtoms.Latin1));
        if (arguments.Count == 1)
        {
            Term operatorText = TextFormat(ProcLibReportExceptionFormatNames.UnaryOperatorControl, operation);
            int column = checked((int)Measure(Term.List(error, prefix, operatorText), ProcLibReportFormatAtoms.Latin1)) + 1;
            Term text = Pretty(
                arguments[0],
                column,
                encoding,
                depth,
                limit
            ).Text;

            return new Cons(operatorText, text);
        }
        var first = Pretty(
            arguments[0],
            start + 1,
            encoding,
            depth,
            limit
        );
        Term second = Pretty(
            arguments[1],
            start + 1,
            encoding,
            depth,
            first.Remaining
        ).Text;
        Term newPrefix = new Cons(Term.I('\n'), Spaces(start));
        Term operatorName = S(operation.Name);
        if (CountNewlines(first.Text) > 0)
            return Append(Term.List(
                first.Text,
                newPrefix,
                operatorName,
                newPrefix
            ), second);
        Term spacedName = S(ProcLibReportExceptionFormatNames.Space + operation.Name + ProcLibReportExceptionFormatNames.Space);
        BigInteger size = Measure(Term.List(error, prefix, spacedName), ProcLibReportFormatAtoms.Latin1) + Measure(first.Text, encoding);
        Term combinedSecond = Pretty(
            arguments[1],
            checked((int)size + 1),
            encoding,
            depth,
            first.Remaining
        ).Text;

        return CountNewlines(second) < CountNewlines(combinedSecond)
            ? Append(Term.List(
                first.Text,
                newPrefix,
                operatorName,
                newPrefix
            ), second)
            : Append(Term.List(first.Text, spacedName), combinedSecond);
    }

    private async ValueTask<Term> Arguments(
        Term arguments,
        int column,
        Term encoding,
        Term depth,
        BigInteger limit
    )
    {
        Term text;
        if (arguments is Cons { Head: Integer first, Tail: var tail } && PrintableArguments(arguments))
        {
            string digits = Digits(first.Value);
            Term atom = await context.Runtime.Modules.Call(
                context,
                ProcLibNames.Erlang,
                ErlangAtomBifNames.ListToAtom,
                S(new string(ProcLibReportExceptionFormatNames.ProbeAtomCharacter, digits.Length))
            );
            Term pretty = Pretty(
                new Cons(atom, tail),
                column + 1,
                encoding,
                depth,
                limit
            ).Text;
            string flattened = ProcLibReportCharacters.FlattenUnicode(pretty);
            text = Term.List(Term.I('['), S(digits), S(flattened[(1 + digits.Length)..]));
        }
        else
        {
            text = Pretty(
                arguments,
                column + 1,
                encoding,
                depth,
                limit
            ).Text;
        }
        byte[] encoded = Encoding.UTF8.GetBytes(ProcLibReportCharacters.FlattenUnicode(text));
        if (encoded.Length < 2 || encoded[0] != '[' || encoded[^1] != ']')
            throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.BadMatch), new BitString(encoded)));

        return Term.List(Term.I('('), new BitString(encoded.AsSpan(1, encoded.Length - 2)), Term.I(')'));
    }

    private static bool PrintableArguments(Term arguments)
    {
        while (arguments is Cons item)
        {
            if (item.Head is not Integer integer || integer.Value < 0 || integer.Value > ProcLibReportCharacterParameters.Latin1Maximum)
                return false;
            int value = (int)integer.Value;
            if (value is not (>= ProcLibReportCharacterParameters.PrintableAsciiFirst and <= ProcLibReportCharacterParameters.PrintableAsciiLast or
                >= ProcLibReportCharacterParameters.PrintableLatin1First and <= ProcLibReportCharacterParameters.Latin1Maximum or
                '\n' or '\r' or '\t' or '\v' or '\b' or '\f' or '\u001b'))
                return false;
            arguments = item.Tail;
        }

        return arguments is Nil;
    }

    private static Term Append(Term prefix, Term suffix)
    {
        Term[] items = Cons.Items(prefix).ToArray();
        for (int index = items.Length - 1; index >= 0; index--)
            suffix = new Cons(items[index], suffix);

        return suffix;
    }

    private static int CountNewlines(Term value)
    {
        int count = 0;
        var pending = new Stack<Term>();
        pending.Push(value);
        while (pending.TryPop(out var item))
        {
            if (item is Cons list)
            {
                pending.Push(list.Tail);
                pending.Push(list.Head);
            }
            else if (item is Integer integer && integer.Value == '\n')
                count++;
            else if (item is BitString { IsBinary: true } binary)
                count += binary.ToArray().Count(character => character == '\n');
        }

        return count;
    }
}
