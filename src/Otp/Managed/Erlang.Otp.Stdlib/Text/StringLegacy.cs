using System.Numerics;

namespace Erlang;

internal static class StringLegacy
{
    internal static bool Owns(string function) => function is
        StringFunctionNames.Len or StringFunctionNames.Concat or StringFunctionNames.Chr or StringFunctionNames.Rchr or StringFunctionNames.Str or StringFunctionNames.Rstr or StringFunctionNames.Span or StringFunctionNames.Cspan or
        StringFunctionNames.Substr or StringFunctionNames.Tokens or StringFunctionNames.Chars or StringFunctionNames.Copies or StringFunctionNames.Words or StringFunctionNames.Strip or StringFunctionNames.SubWord or
        StringFunctionNames.Left or StringFunctionNames.Right or StringFunctionNames.SubString or StringFunctionNames.Centre or StringFunctionNames.Join or StringFunctionNames.ToUpper or StringFunctionNames.ToLower or StringFunctionNames.ListToInteger;

    internal static ErlangException Clause() => new(ErlangErrorReasons.FunctionClause);

    internal static BigInteger IntegerValue(Term value) => value is Integer integer ? integer.Value : throw Clause();

    internal static int Count(Term value, int minimum = 0)
    {
        BigInteger number = IntegerValue(value);
        if (number < minimum)
        {
            throw Clause();
        }
        if (number > int.MaxValue)
        {
            throw new ErlangException(ErlangErrorReasons.SystemLimit);
        }

        return (int)number;
    }

    internal static Term Append(Term first, Term tail) => Cons.From(Cons.Items(first), tail);

    internal static Term LatinCase(Term value, bool upper)
    {
        if (value is not Integer integer)
        {
            return value;
        }
        BigInteger cp = integer.Value;
        bool changes = upper
            ? cp >= 'a' && cp <= 'z' || cp >= 0xe0 && cp <= 0xf6 || cp >= 0xf8 && cp <= 0xfe
            : cp >= 'A' && cp <= 'Z' || cp >= 0xc0 && cp <= 0xd6 || cp >= 0xd8 && cp <= 0xde;

        return changes ? new Integer(cp + (upper ? -32 : 32)) : value;
    }

    internal static Term ReadInteger(Term input)
    {
        if (input is not (Cons or Nil))
        {
            return Term.Tuple(Term.A(CommonAtomNames.Error), Term.A(StringTextAtoms.NotList));
        }
        Term rest = input;
        int sign = 1;
        if (rest is Cons { Head: Integer first } firstCell && (first.Value == '+' || first.Value == '-'))
        {
            sign = first.Value == '-' ? -1 : 1;
            rest = firstCell.Tail;
        }
        BigInteger number = 0;
        bool any = false;
        while (rest is Cons { Head: Integer digit } cell && digit.Value >= '0' && digit.Value <= '9')
        {
            any = true;
            number = number * 10 + digit.Value - '0';
            rest = cell.Tail;
        }

        return any ? Term.Tuple(new Integer(number * sign), rest) : Term.Tuple(Term.A(CommonAtomNames.Error), Term.A(StringTextAtoms.NoInteger));
    }
}
