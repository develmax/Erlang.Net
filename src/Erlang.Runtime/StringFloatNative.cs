namespace Erlang;

internal static class StringFloatNative
{
    internal static void Register(ModuleRegistry registry)
        => registry.Register(
            StringFloatNativeNames.Module,
            StringFloatNativeNames.Function,
            1,
            (_, arguments) => ValueTask.FromResult(Read(arguments[0]))
        );

    internal static Term Read(Term input)
    {
        if (input is not Cons)
            return Error(input is Nil ? StringFloatNativeNames.NoFloat : StringFloatNativeNames.NotList);
        Term rest = input;
        var characters = new List<int>();
        if (Peek() is StringFloatNativeNames.Positive or StringFloatNativeNames.Negative)
            Take();
        if (!Digits() || Peek() is not (StringFloatNativeNames.DecimalPoint or StringFloatNativeNames.DecimalComma))
            return Error(StringFloatNativeNames.NoFloat);
        Take();
        if (!Digits())
            return Error(StringFloatNativeNames.NoFloat);
        if (Peek() is StringFloatNativeNames.ExponentLower or StringFloatNativeNames.ExponentUpper)
        {
            Term beforeExponent = rest;
            int beforeCharacters = characters.Count;
            Take();
            if (Peek() is StringFloatNativeNames.Positive or StringFloatNativeNames.Negative)
                Take();
            if (!Digits())
            {
                rest = beforeExponent;
                characters.RemoveRange(beforeCharacters, characters.Count - beforeCharacters);
            }
        }
        try
        {
            return Term.Tuple(ErlangFloatText.Parse(characters), rest);
        }
        catch (ErlangException error) when (error.Reason is Atom { Name: ErlangErrorReasons.BadArgument })
        {
            return Error(StringFloatNativeNames.NoFloat);
        }

        int? Peek() => rest is Cons { Head: Integer value } && value.Value >= char.MinValue && value.Value <= char.MaxValue
            ? (int)value.Value : null;

        void Take()
        {
            var cell = (Cons)rest;
            characters.Add((int)((Integer)cell.Head).Value);
            rest = cell.Tail;
        }

        bool Digits()
        {
            int before = characters.Count;
            while (Peek() is >= StringFloatNativeNames.FirstDigit and <= StringFloatNativeNames.LastDigit)
                Take();

            return before != characters.Count;
        }
    }

    private static Term Error(string reason) => Term.Tuple(Term.A(CommonAtomNames.Error), Term.A(reason));
}
