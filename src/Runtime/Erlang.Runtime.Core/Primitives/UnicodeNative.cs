namespace Erlang;

internal static class UnicodeNative
{
    internal static bool TryCapture(
        ModuleRegistry registry,
        string module,
        string function,
        int arity,
        out ErlangFunction body
    )
    {
        body = (module, function, arity) switch
        {
            (UnicodeNativeNames.Module, UnicodeNativeNames.IsSevenBit, 1)
                => (context, arguments) => SevenBit(context, arguments[0]),
            (UnicodeNativeNames.Module, UnicodeNativeNames.ToBinary, 2)
                => (context, arguments) => Convert(
                    registry,
                    context,
                    arguments[0],
                    arguments[1],
                    false
                ),
            (UnicodeNativeNames.Module, UnicodeNativeNames.ToList, 2)
                => (context, arguments) => Convert(
                    registry,
                    context,
                    arguments[0],
                    arguments[1],
                    true
                ),
            _ => null!
        };

        return body is not null;
    }

    private static async ValueTask<Term> SevenBit(ProcessContext context, Term input)
    {
        if (input is not BitString { IsBinary: true } binary)
            return Term.A(ErlangBooleanAtoms.False);
        for (int index = 0; index < binary.BitLength / UnicodeNativeNames.BitsPerByte; index++)
        {
            if (index % UnicodeNativeNames.WorkSlice == 0)
                await context.BumpReductionsAsync(UnicodeNativeNames.WorkSlice);
            if (binary.ByteSpan[index] >= UnicodeNativeNames.AsciiLimit)
                return Term.A(ErlangBooleanAtoms.False);
        }

        return Term.A(ErlangBooleanAtoms.True);
    }

    private static async ValueTask<Term> Convert(
        ModuleRegistry registry,
        ProcessContext context,
        Term data,
        Term encoding,
        bool list
    )
    {
        if (encoding is not Atom { Name: UnicodeNativeNames.Latin1 or UnicodeNativeNames.Unicode or UnicodeNativeNames.Utf8 })
            return await registry.Call(
                context,
                UnicodeNativeNames.Module,
                list ? UnicodeNativeNames.ToListTrap : UnicodeNativeNames.ToBinaryTrap,
                data,
                encoding
            );
        bool latin1 = encoding is Atom { Name: UnicodeNativeNames.Latin1 };
        Term subject = !list && data is Cons { Head: BitString single, Tail: Nil } ? single : data;
        var state = new UnicodeNativeTraversal(context, latin1, list);

        return await state.Run(subject);
    }
}
