using System.Numerics;

namespace Erlang;

internal static class StringLegacyWordExecution
{
    internal static async ValueTask<Term> Strip(ProcessContext context, IReadOnlyList<Term> arguments)
    {
        Term input = arguments[0];
        if (arguments.Count == 1)
        {
            return await Strip(context, [input, Term.A(StringTextAtoms.Both)]);
        }
        if (arguments[1] is not Atom { Name: StringTextAtoms.Left or StringTextAtoms.Right or StringTextAtoms.Both } direction)
        {
            throw StringSourceTrace.Clause(
                context,
                StringFunctionNames.Strip,
                arguments,
                arguments.Count == 2 ? StringLegacySource.StripTwo : StringLegacySource.StripThree
            );
        }
        Term character = arguments.Count == 3 ? arguments[2] : Term.I(' ');
        if (direction.Name == StringTextAtoms.Left)
        {
            return StripLeft(context, input, character);
        }
        if (direction.Name == StringTextAtoms.Both)
        {
            using var call = StringSourceTrace.Scope(
                context,
                StringFunctionNames.Strip,
                arguments.Count,
                arguments.Count == 2 ? StringLegacySource.StripTwoLeftCall : StringLegacySource.StripThreeLeftCall
            );
            input = StripLeft(context, input, character);
        }

        return await StripRight(context, input, character);
    }

    private static Term StripLeft(ProcessContext context, Term input, Term character)
    {
        while (input is Cons cell && cell.Head.Equals(character))
        {
            input = cell.Tail;
        }
        if (input is not (Cons or Nil) || character is not Integer)
        {
            throw StringSourceTrace.Clause(
                context,
                StringSourceNames.StripLeft,
                [input, character],
                StringLegacySource.StripLeft
            );
        }

        return input;
    }

    private static async ValueTask<Term> StripRight(ProcessContext context, Term input, Term character)
    {
        var pending = new List<Term>();
        while (input is Cons cell)
        {
            pending.Add(cell.Head);
            input = cell.Tail;
            await context.ReduceAsync();
        }
        if (input is not Nil || character is not Integer)
        {
            var continuations = new List<Term>();
            int? previousLine = null;
            for (int index = pending.Count - 1; index >= 0; index--)
            {
                int line = pending[index].Equals(character) ? StringLegacySource.StripRightEqualCall : StringLegacySource.StripRightOtherCall;
                if (line != previousLine)
                {
                    continuations.Add(StringSourceTrace.Frame(StringSourceNames.StripRight, 2, line));
                }
                previousLine = line;
            }

            throw StringSourceTrace.Clause(
                context,
                StringSourceNames.StripRight,
                [input, character],
                StringLegacySource.StripRight,
                continuations.ToArray()
            );
        }
        int end = pending.Count;
        while (end > 0 && pending[end - 1].Equals(character))
        {
            end--;
        }

        return Cons.From(pending.Take(end));
    }

    internal static async ValueTask<Term> Invoke(ProcessContext context, string function, IReadOnlyList<Term> arguments)
    {
        if (function == StringFunctionNames.Words)
        {
            return await Words(context, arguments[0], arguments.Count == 2 ? arguments[1] : Term.I(' '));
        }
        if (function == StringFunctionNames.SubWord)
        {
            return await SubWord(
                context,
                arguments[0],
                arguments[1],
                arguments.Count == 3 ? arguments[2] : Term.I(' ')
            );
        }
        bool centre = function == StringFunctionNames.Centre;
        bool left = function == StringFunctionNames.Left;
        int headLine = arguments.Count == 3
            ? centre ? StringLegacySource.CentreThree : left ? StringLegacySource.LeftThree : StringLegacySource.RightThree
            : centre ? StringLegacySource.CentreTwo : left ? StringLegacySource.LeftTwo : StringLegacySource.RightTwo;
        if (arguments[1] is not Integer length)
        {
            throw StringSourceTrace.Clause(
                context,
                function,
                arguments,
                headLine
            );
        }
        Term character = arguments.Count == 3 ? arguments[2] : Term.I(' ');
        headLine = centre ? StringLegacySource.CentreThree : left ? StringLegacySource.LeftThree : StringLegacySource.RightThree;
        if (character is not Integer)
        {
            throw StringSourceTrace.Clause(
                context,
                function,
                [arguments[0], length, character],
                headLine
            );
        }
        Term input = arguments[0];
        if (centre && length.Value == 0 && input is Cons or Nil)
        {
            return Nil.Value;
        }
        Integer size;
        using (StringSourceTrace.Scope(
            context,
            function,
            3,
            centre ? StringLegacySource.CentreLengthCall : left ? StringLegacySource.LeftLengthCall : StringLegacySource.RightLengthCall
        ))
        {
            size = (Integer)await context.Runtime.Modules.Call(
                context,
                ErlangModuleNames.Module,
                StringFunctionNames.Length,
                input
            );
        }
        if (size.Value > length.Value)
        {
            if (!centre && !left)
            {
                return StringLegacySubstringExecution.Invoke(context, StringFunctionNames.Substr, [input, new Integer(size.Value - length.Value + 1)]);
            }
            BigInteger start = centre ? (size.Value - length.Value) / 2 + 1 : 1;

            return StringLegacySubstringExecution.Invoke(context, StringFunctionNames.Substr, [input, new Integer(start), length]);
        }
        if (size.Value == length.Value)
        {
            return input;
        }
        BigInteger missing = length.Value - size.Value;
        BigInteger leading = centre ? missing / 2 : left ? 0 : missing;
        Term tail = await StringLegacyExecution.Chars(
            context,
            character,
            new Integer(missing - leading),
            Nil.Value
        );
        Term padded = await StringLegacyExecution.Append(context, input, tail);

        return await StringLegacyExecution.Chars(
            context,
            character,
            new Integer(leading),
            padded
        );
    }

    private static async ValueTask<Term> Words(ProcessContext context, Term input, Term character)
    {
        if (character is not Integer)
        {
            throw StringSourceTrace.Clause(
                context,
                StringFunctionNames.Words,
                [input, character],
                StringLegacySource.Words
            );
        }
        using (StringSourceTrace.Scope(
            context,
            StringFunctionNames.Words,
            2,
            StringLegacySource.WordsStripCall
        ))
        {
            input = await Strip(context, [input, Term.A(StringTextAtoms.Both), character]);
        }
        int count = 0;
        while (input is Cons cell)
        {
            if (cell.Head.Equals(character))
            {
                using var call = StringSourceTrace.Scope(
                    context,
                    StringSourceNames.WordCount,
                    3,
                    StringLegacySource.WordCountStripCall
                );
                input = StripLeft(context, cell.Tail, character);
                count++;
            }
            else
            {
                input = cell.Tail;
            }
            await context.ReduceAsync();
        }
        if (input is not Nil)
        {
            throw StringSourceTrace.Clause(
                context,
                StringSourceNames.WordCount,
                [input, character, Term.I(count)],
                StringLegacySource.WordCount
            );
        }

        return Term.I(count + 1);
    }

    private static async ValueTask<Term> SubWord(
        ProcessContext context,
        Term input,
        Term number,
        Term character
    )
    {
        if (number is not Integer stop || character is not Integer)
        {
            throw StringSourceTrace.Clause(
                context,
                StringFunctionNames.SubWord,
                [input, number, character],
                StringLegacySource.SubWord
            );
        }
        Integer count;
        using (StringSourceTrace.Scope(
            context,
            StringFunctionNames.SubWord,
            3,
            StringLegacySource.SubWordCountCall
        ))
        {
            count = (Integer)await Words(context, input, character);
        }
        if (count.Value < stop.Value)
        {
            return Nil.Value;
        }
        using (StringSourceTrace.Scope(
            context,
            StringFunctionNames.SubWord,
            3,
            StringLegacySource.SubWordStripCall
        ))
        {
            input = StripLeft(context, input, character);
        }
        BigInteger index = 1;
        Term reverse = Nil.Value;
        while (true)
        {
            if (input is Nil || input is Cons first && first.Head.Equals(character) && index == stop.Value)
            {
                return Cons.From(Cons.Items(reverse).Reverse());
            }
            if (input is Cons cell)
            {
                if (index == stop.Value)
                {
                    reverse = new Cons(cell.Head, reverse);
                    input = cell.Tail;
                    continue;
                }
                if (index < stop.Value)
                {
                    if (cell.Head.Equals(character))
                    {
                        using var call = StringSourceTrace.Scope(
                            context,
                            StringSourceNames.WordSelect,
                            5,
                            StringLegacySource.WordSelectStripCall
                        );
                        input = StripLeft(context, cell.Tail, character);
                        index++;
                    }
                    else
                    {
                        input = cell.Tail;
                    }
                    continue;
                }
            }

            throw StringSourceTrace.Clause(
                context,
                StringSourceNames.WordSelect,
                [input, number, character, new Integer(index), reverse],
                StringLegacySource.WordSelect
            );
        }
    }
}
