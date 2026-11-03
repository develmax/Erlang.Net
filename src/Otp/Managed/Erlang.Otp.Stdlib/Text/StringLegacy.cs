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

    internal static Term Invoke(string function, IReadOnlyList<Term> a)
    {
        Term input = a[0];
        switch (function)
        {
            case StringFunctionNames.Len:
                return Term.I(Cons.Items(input).Count());
            case StringFunctionNames.Concat:
                return Append(input, a[1]);
            case StringFunctionNames.Chars:
                {
                    int count = Count(a[1]);
                    _ = IntegerValue(input);

                    return Cons.From(Enumerable.Repeat(input, count), a.Count == 3 ? a[2] : Nil.Value);
                }
            case StringFunctionNames.Copies:
                {
                    if (input is not (Cons or Nil))
                    {
                        throw Clause();
                    }
                    int count = Count(a[1]);
                    Term result = Nil.Value;
                    for (int index = 0; index < count; index++)
                    {
                        result = Append(input, result);
                    }

                    return result;
                }
            case StringFunctionNames.Substr:
            case StringFunctionNames.SubString:
                {
                    int start = Count(a[1], 1);
                    Term tail = Drop(input, start - 1);
                    if (a.Count == 2)
                    {
                        return tail;
                    }
                    int count = function == StringFunctionNames.Substr ? Count(a[2]) : Count(new Integer(IntegerValue(a[2]) - start + 1));

                    var selected = new List<Term>();
                    while (count > 0 && tail is Cons cell)
                    {
                        selected.Add(cell.Head);
                        tail = cell.Tail;
                        count--;
                    }
                    if (tail is not (Cons or Nil))
                    {
                        throw Clause();
                    }

                    return Cons.From(selected);
                }
            case StringFunctionNames.ToLower:
            case StringFunctionNames.ToUpper:
                {
                    bool upper = function == StringFunctionNames.ToUpper;
                    if (input is Integer)
                    {
                        return LatinCase(input, upper);
                    }
                    if (input is not (Cons or Nil))
                    {
                        throw Clause();
                    }

                    var mapped = new List<Term>();
                    while (input is Cons cell)
                    {
                        mapped.Add(LatinCase(cell.Head, upper));
                        input = cell.Tail;
                    }
                    if (input is not Nil)
                    {
                        throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.BadGenerator), input));
                    }

                    return Cons.From(mapped);
                }
            case StringFunctionNames.ListToInteger:
                return ReadInteger(input);
            case StringFunctionNames.Join:
                {
                    Term[] parts = Cons.Items(input).ToArray();
                    if (parts.Length == 0)
                    {
                        if (a[1] is not (Cons or Nil))
                        {
                            throw Clause();
                        }

                        return Nil.Value;
                    }
                    Term result = Nil.Value;
                    for (int index = parts.Length - 1; index >= 0; index--)
                    {
                        result = Append(parts[index], result);
                        if (index > 0)
                        {
                            result = Append(a[1], result);
                        }
                    }

                    return result;
                }
            case StringFunctionNames.Strip:
                {
                    string direction = a.Count > 1 && a[1] is Atom atom ? atom.Name : a.Count == 1 ? StringTextAtoms.Both : throw Clause();
                    Term separator = a.Count == 3 ? a[2] : Term.I(' ');
                    _ = IntegerValue(separator);
                    if (direction is not (StringTextAtoms.Left or StringTextAtoms.Right or StringTextAtoms.Both))
                    {
                        throw Clause();
                    }
                    Term result = input;
                    if (direction is StringTextAtoms.Left or StringTextAtoms.Both)
                    {
                        while (result is Cons cell && cell.Head.Equals(separator))
                        {
                            result = cell.Tail;
                        }
                    }
                    if (direction is StringTextAtoms.Right or StringTextAtoms.Both)
                    {
                        var retained = new List<Term>();
                        while (result is Cons cell)
                        {
                            retained.Add(cell.Head);
                            result = cell.Tail;
                        }
                        if (result is not Nil)
                        {
                            throw Clause();
                        }
                        Term[] items = retained.ToArray();
                        int end = items.Length;
                        while (end > 0 && items[end - 1].Equals(separator))
                        {
                            end--;
                        }
                        result = Cons.From(items.Take(end));
                    }
                    if (result is not (Cons or Nil))
                    {
                        throw Clause();
                    }

                    return result;
                }
        }

        if (function is StringFunctionNames.Chr or StringFunctionNames.Rchr or StringFunctionNames.Str or StringFunctionNames.Rstr
            or StringFunctionNames.Span or StringFunctionNames.Cspan or StringFunctionNames.Tokens)
        {
            return StringLegacySearch.Invoke(function, input, a[1]);
        }

        return StringLegacyWords.Invoke(function, a);
    }
    private static Term Drop(Term input, int count)
    {
        while (count > 0 && input is Cons cell)
        {
            input = cell.Tail;
            count--;
        }
        if (count != 0 || input is not (Cons or Nil))
        {
            throw Clause();
        }

        return input;
    }

    private static IEnumerable<Term> Tokens(Term[] values, HashSet<Term> separators)
    {
        int start = 0;
        for (int index = 0; index <= values.Length; index++)
        {
            if (index == values.Length || separators.Contains(values[index]))
            {
                if (index > start)
                {
                    yield return Cons.From(values.Skip(start).Take(index - start));
                }
                start = index + 1;
            }
        }
    }

    private static Term LatinCase(Term value, bool upper)
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

    private static Term ReadInteger(Term input)
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
