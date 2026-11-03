using System.Numerics;

namespace Erlang;

internal static class StringLegacyWords
{
    internal static Term Invoke(string function, IReadOnlyList<Term> arguments)
    {
        Term input = arguments[0];
        if (function is StringFunctionNames.Words or StringFunctionNames.SubWord)
        {
            BigInteger number = function == StringFunctionNames.SubWord ? StringLegacy.IntegerValue(arguments[1]) : 0;
            Term separator = arguments.Count == (function == StringFunctionNames.Words ? 2 : 3) ? arguments[^1] : Term.I(' ');
            _ = StringLegacy.IntegerValue(separator);
            Term stripped = StringLegacy.Invoke(StringFunctionNames.Strip, [input, Term.A(StringTextAtoms.Both), separator]);
            var words = new List<Term>();
            var word = new List<Term>();
            foreach (Term value in Cons.Items(stripped))
            {
                if (value.Equals(separator))
                {
                    if (word.Count != 0)
                    {
                        words.Add(Cons.From(word));
                        word.Clear();
                    }
                }
                else
                {
                    word.Add(value);
                }
            }
            if (word.Count != 0)
            {
                words.Add(Cons.From(word));
            }
            if (function == StringFunctionNames.Words)
            {
                return Term.I(Math.Max(1, words.Count));
            }
            if (number > words.Count || words.Count == 0)
            {
                return Nil.Value;
            }
            if (number < 1)
            {
                throw StringLegacy.Clause();
            }

            return words[(int)number - 1];
        }
        BigInteger requested = StringLegacy.IntegerValue(arguments[1]);
        Term fill = arguments.Count == 3 ? arguments[2] : Term.I(' ');
        _ = StringLegacy.IntegerValue(fill);
        if (function == StringFunctionNames.Centre && requested == 0 && input is Cons or Nil)
        {
            return Nil.Value;
        }
        Term[] values = Cons.Items(input).ToArray();
        int length = StringLegacy.Count(new Integer(requested));
        if (values.Length >= length)
        {
            int start = function == StringFunctionNames.Left ? 0 : function == StringFunctionNames.Right ? values.Length - length : (values.Length - length) / 2;

            return Cons.From(values.Skip(start).Take(length));
        }
        int missing = length - values.Length;
        int leading = function == StringFunctionNames.Left ? 0 : function == StringFunctionNames.Right ? missing : missing / 2;

        return Cons.From(Enumerable.Repeat(fill, leading).Concat(values).Concat(Enumerable.Repeat(fill, missing - leading)));
    }
}
