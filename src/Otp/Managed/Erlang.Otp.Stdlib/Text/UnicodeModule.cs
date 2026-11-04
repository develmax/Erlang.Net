// Derived from lib/stdlib/src/unicode.erl, Erlang/OTP 29.1.1, Apache-2.0.
// Copyright Ericsson AB 2008-2026. Upstream notices are retained in the Erlang source.
namespace Erlang;

/// <summary>Explicit managed alternative for the complete pinned unicode module.</summary>
/// <remarks>Native conversion BIFs remain shared primitives; Unicode data comes from unicode_util.</remarks>
public static class UnicodeModule
{
    public const string Name = "unicode";

    public static IReadOnlyList<(string Name, int Arity)> Exports { get; } = Array.AsReadOnly(new (string, int)[]
    {
        (UnicodeNames.Binary, 1), (UnicodeNames.Binary, 2), (UnicodeNames.Binary, 3),
        (UnicodeNames.List, 1), (UnicodeNames.List, 2),
        (UnicodeNames.BinaryTrap, 2), (UnicodeNames.ListTrap, 2),
        (UnicodeNames.SevenBit, 1), (UnicodeNames.Bom, 1), (UnicodeNames.EncodingBom, 1),
        ("characters_to_nfd_list", 1), ("characters_to_nfd_binary", 1),
        ("characters_to_nfc_list", 1), ("characters_to_nfc_binary", 1),
        ("characters_to_nfkd_list", 1), ("characters_to_nfkd_binary", 1),
        ("characters_to_nfkc_list", 1), ("characters_to_nfkc_binary", 1),
        (UnicodeNames.Whitespace, 1), (UnicodeNames.Start, 1), (UnicodeNames.Continue, 1), (UnicodeNames.Category, 1)
    });

    public static void Register(ModuleRegistry registry)
        => registry.InstallSourceWrappers(target =>
        {
            foreach (var export in Exports)
            {
                if (export is (UnicodeNames.Binary, 2) or (UnicodeNames.List, 2) or (UnicodeNames.SevenBit, 1))
                {
                    continue;
                }
                string function = export.Name;
                target.RegisterReviewedManagedLibrary789(
                    Name,
                    function,
                    export.Arity,
                    (context, arguments) => Invoke(context, function, arguments)
                );
            }
        });

    public static async ValueTask<Term> Invoke(ProcessContext context, string function, IReadOnlyList<Term> arguments)
    {
        if (!Exports.Contains((function, arguments.Count)))
        {
            throw new ErlangException(ErlangErrorReasons.UndefinedFunction);
        }
        using var tail = context.EnterSourceTailCall(Name, function);
        Term data = arguments[0];
        if (function is UnicodeNames.SevenBit || function is UnicodeNames.Binary or UnicodeNames.List && arguments.Count == 2)
        {
            return await context.Runtime.Modules.Call(
                context,
                Name,
                function,
                arguments.ToArray()
            );
        }
        if (function == UnicodeNames.Bom)
        {
            return ReadBom(context, data);
        }
        if (function == UnicodeNames.EncodingBom)
        {
            return WriteBom(context, data);
        }
        if (function is UnicodeNames.Category or UnicodeNames.Whitespace or UnicodeNames.Start or UnicodeNames.Continue)
        {
            return await Property(context, function, data);
        }
        if (function.StartsWith("characters_to_nf", StringComparison.Ordinal))
        {
            try
            {
                return await Normalize(context, function, data);
            }
            catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Error)
            {
                throw UnicodeSource.Error(
                    context,
                    function,
                    arguments,
                    Term.A(ErlangErrorReasons.BadArgument),
                    badarg: true
                );
            }
        }
        try
        {
            if (arguments.Count == 1)
            {
                return await context.Runtime.Modules.Call(
                    context,
                    Name,
                    function,
                    data,
                    Term.A(UnicodeNames.Unicode)
                );
            }
            Term input = arguments[1];
            Term output = arguments.Count == 3 ? arguments[2] : Term.A(UnicodeNames.Unicode);
            Term converted = await ToBinary(
                context,
                data,
                input,
                output
            );
            if (function == UnicodeNames.ListTrap)
            {
                if (converted is TupleTerm tuple)
                {
                    return Term.Tuple(tuple.Items[0], await NativeList(context, tuple.Items[1]), tuple.Items[2]);
                }

                return await NativeList(context, converted);
            }

            return converted;
        }
        catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Error)
        {
            string owner = function == UnicodeNames.BinaryTrap ? UnicodeNames.Binary : function == UnicodeNames.ListTrap ? UnicodeNames.List : function;
            Term reason = arguments.Count == 1 || error.Reason.Equals(Term.A(ErlangErrorReasons.SystemLimit))
                ? error.Reason : Term.A(ErlangErrorReasons.BadArgument);
            throw UnicodeSource.Error(
                context,
                owner,
                arguments,
                reason,
                fake: function is UnicodeNames.BinaryTrap or UnicodeNames.ListTrap
            );
        }
    }

    private static ValueTask<Term> NativeList(ProcessContext context, Term data)
        => context.Runtime.Modules.Call(
            context,
            Name,
            UnicodeNames.List,
            data,
            Term.A(UnicodeNames.Utf8)
        );

    private static async ValueTask<Term> ToBinary(
        ProcessContext context,
        Term data,
        Term from,
        Term to
    )
    {
        UnicodeEncoding input = UnicodeEncoding.Parse(from);
        UnicodeEncoding output = UnicodeEncoding.Parse(to);
        if (data is BitString { IsBinary: true } binary && input.Width == 1 && output.Width == 1)
        {
            return binary;
        }
        if (input.Width is 0 or 1 && output.Width == 0)
        {
            if (input.Width == 1 && data is BitString { IsBinary: true } asciiInput
                && (await context.Runtime.Modules.Call(
                    context,
                    Name,
                    UnicodeNames.SevenBit,
                    asciiInput
                )).Equals(Term.A(ErlangBooleanAtoms.True)))
            {
                return asciiInput;
            }

            return await context.Runtime.Modules.Call(
                context,
                Name,
                UnicodeNames.Binary,
                data,
                from
            );
        }
        if (input.Width == 0 && output.Width == 1 && data is BitString { IsBinary: true } ascii
            && (await context.Runtime.Modules.Call(
                context,
                Name,
                UnicodeNames.SevenBit,
                ascii
            )).Equals(Term.A(ErlangBooleanAtoms.True)))
        {
            return ascii;
        }

        return await new UnicodeConversion(context, input, output).Run(data);
    }

    private static async ValueTask<Term> Normalize(ProcessContext context, string function, Term data)
    {
        string form = function["characters_to_".Length..function.LastIndexOf('_')];
        bool binary = function.EndsWith("_binary", StringComparison.Ordinal);
        var result = new List<Term>();
        var row = new List<Term>();
        var chunks = new List<byte>();
        while (true)
        {
            await context.BumpReductionsAsync(1);
            Term next = await Utility(context, form, data);
            if (next is Cons cell)
            {
                if (binary)
                {
                    row.Add(cell.Head);
                    if (row.Count == UnicodeNames.NormalizationRow)
                    {
                        await Flush();
                    }
                }
                else if (cell.Head is Cons or Nil)
                {
                    result.AddRange(Cons.Items(cell.Head));
                }
                else
                {
                    result.Add(cell.Head);
                }
                data = cell.Tail;
                continue;
            }
            if (binary)
            {
                await Flush();
            }
            Term converted = binary ? new BitString(chunks.ToArray()) : Cons.From(result);
            if (next is TupleTerm { Items.Count: 2 } error)
            {
                return Term.Tuple(Term.A(CommonAtomNames.Error), converted, error.Items[1]);
            }

            return converted;
        }

        async ValueTask Flush()
        {
            Term encoded = await context.Runtime.Modules.Call(
                context,
                Name,
                UnicodeNames.Binary,
                Cons.From(row)
            );
            if (encoded is not BitString binaryRow)
            {
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            }
            chunks.AddRange(binaryRow.ByteSpan.ToArray());
            row.Clear();
        }
    }

    private static async ValueTask<Term> Property(ProcessContext context, string function, Term data)
    {
        if (data is not Integer integer || integer.Value < 0 || integer.Value > UnicodeNames.Maximum)
        {
            throw UnicodeSource.Error(
                context,
                function,
                [data],
                Term.A(ErlangErrorReasons.BadArgument),
                badarg: true
            );
        }
        int scalar = (int)integer.Value;
        if (function == UnicodeNames.Category)
        {
            return await Utility(context, function, data);
        }
        if (function == UnicodeNames.Whitespace)
        {
            return scalar is 9 or 10 or 11 or 12 or 13 or 32 or 133 or 160 ? Term.A(ErlangBooleanAtoms.True)
                : scalar <= 5000 ? Term.A(ErlangBooleanAtoms.False) : await Utility(context, function, data);
        }
        bool continuation = function == UnicodeNames.Continue;
        if (scalar <= 127)
        {
            bool valid = scalar is >= 65 and <= 90 or >= 97 and <= 122 || continuation && scalar is >= 48 and <= 57 or 95;

            return Term.A(valid ? ErlangBooleanAtoms.True : ErlangBooleanAtoms.False);
        }
        var category = (TupleTerm)await Utility(context, UnicodeNames.Category, data);
        string major = ((Atom)category.Items[0]).Name;
        string minor = ((Atom)category.Items[1]).Name;
        if (major == "letter")
        {
            return minor == "modifier" ? await Utility(context, "is_letter_not_pattern_syntax", data) : Term.A(ErlangBooleanAtoms.True);
        }
        if (major == "number" && minor == "letter" || continuation && (major == "number" && minor != "other"
            || major == "punctuation" && minor == "connector" || major == "mark" && minor is "non_spacing" or "spacing_combining"))
        {
            return Term.A(ErlangBooleanAtoms.True);
        }
        if (continuation && major == "number" && minor == "other")
        {
            return await Utility(context, "is_other_id_continue", data);
        }
        Term start = await Utility(context, "is_other_id_start", data);

        return !continuation || start.Equals(Term.A(ErlangBooleanAtoms.True)) ? start : await Utility(context, "is_other_id_continue", data);
    }

    private static ValueTask<Term> Utility(ProcessContext context, string function, Term data)
        => context.Runtime.Modules.Call(
            context,
            UnicodeNames.Utility,
            function,
            data
        );

    private static Term ReadBom(ProcessContext context, Term data)
    {
        if (data is not BitString { IsBinary: true } binary)
        {
            throw UnicodeSource.Clause(
                context,
                UnicodeNames.Bom,
                data,
                386
            );
        }
        ReadOnlySpan<byte> bytes = binary.ByteSpan;
        foreach ((Term encoding, byte[] prefix) in Boms)
        {
            if (bytes.StartsWith(prefix))
            {
                return Term.Tuple(encoding, Term.I(prefix.Length));
            }
        }

        return Term.Tuple(Term.A(UnicodeNames.Latin1), Term.I(0));
    }

    private static Term WriteBom(ProcessContext context, Term data)
    {
        Term normalized = data is Atom { Name: UnicodeNames.Unicode } ? Term.A(UnicodeNames.Utf8)
            : data is Atom { Name: UnicodeNames.Utf16 or UnicodeNames.Utf32 } atom ? Term.Tuple(atom, Term.A(UnicodeNames.Big)) : data;
        foreach ((Term encoding, byte[] prefix) in Boms)
        {
            if (normalized.Equals(encoding))
            {
                return new BitString(prefix);
            }
        }
        if (normalized.Equals(Term.A(UnicodeNames.Latin1)))
        {
            return new BitString(Array.Empty<byte>());
        }

        throw UnicodeSource.Clause(
            context,
            UnicodeNames.EncodingBom,
            data,
            416
        );
    }

    private static readonly (Term Encoding, byte[] Prefix)[] Boms =
    [
        (Term.A(UnicodeNames.Utf8), [239, 187, 191]),
        (Term.Tuple(Term.A(UnicodeNames.Utf32), Term.A(UnicodeNames.Big)), [0, 0, 254, 255]),
        (Term.Tuple(Term.A(UnicodeNames.Utf32), Term.A(UnicodeNames.Little)), [255, 254, 0, 0]),
        (Term.Tuple(Term.A(UnicodeNames.Utf16), Term.A(UnicodeNames.Big)), [254, 255]),
        (Term.Tuple(Term.A(UnicodeNames.Utf16), Term.A(UnicodeNames.Little)), [255, 254])
    ];
}
