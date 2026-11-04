namespace Erlang;

/// <summary>Explicit C# implementation of the pinned OTP string API.</summary>
/// <remarks>Unicode tables and streaming grapheme rules are supplied by the selected unicode_util module.</remarks>
public static class StringModule
{
    public const string Name = "string";

    public static IReadOnlyList<(string Name, int Arity)> Exports { get; } = Array.AsReadOnly(new (string, int)[]
    {
        (StringFunctionNames.IsEmpty, 1), (StringFunctionNames.Length, 1), (StringFunctionNames.ToGraphemes, 1), (StringFunctionNames.Reverse, 1),
        (StringFunctionNames.Equal, 2), (StringFunctionNames.Equal, 3), (StringFunctionNames.Equal, 4), (StringFunctionNames.Slice, 2), (StringFunctionNames.Slice, 3),
        (StringFunctionNames.Pad, 2), (StringFunctionNames.Pad, 3), (StringFunctionNames.Pad, 4), (StringFunctionNames.Trim, 1), (StringFunctionNames.Trim, 2), (StringFunctionNames.Trim, 3),
        (StringFunctionNames.Chomp, 1), (StringFunctionNames.Take, 2), (StringFunctionNames.Take, 3), (StringFunctionNames.Take, 4), (StringFunctionNames.Lexemes, 2), (StringFunctionNames.NthLexeme, 3),
        (StringFunctionNames.Uppercase, 1), (StringFunctionNames.Lowercase, 1), (StringFunctionNames.Titlecase, 1), (StringFunctionNames.Casefold, 1), (StringFunctionNames.Prefix, 2),
        (StringFunctionNames.Split, 2), (StringFunctionNames.Split, 3), (StringFunctionNames.Replace, 3), (StringFunctionNames.Replace, 4), (StringFunctionNames.Find, 2), (StringFunctionNames.Find, 3),
        (StringFunctionNames.JaroSimilarity, 2), (StringFunctionNames.NextCodepoint, 1), (StringFunctionNames.NextGrapheme, 1), (StringFunctionNames.ToFloat, 1), (StringFunctionNames.ToInteger, 1),
        (StringFunctionNames.Len, 1), (StringFunctionNames.Concat, 2), (StringFunctionNames.Chr, 2), (StringFunctionNames.Rchr, 2), (StringFunctionNames.Str, 2), (StringFunctionNames.Rstr, 2),
        (StringFunctionNames.Span, 2), (StringFunctionNames.Cspan, 2), (StringFunctionNames.Substr, 2), (StringFunctionNames.Substr, 3), (StringFunctionNames.Tokens, 2), (StringFunctionNames.Chars, 2), (StringFunctionNames.Chars, 3),
        (StringFunctionNames.Copies, 2), (StringFunctionNames.Words, 1), (StringFunctionNames.Words, 2), (StringFunctionNames.Strip, 1), (StringFunctionNames.Strip, 2), (StringFunctionNames.Strip, 3),
        (StringFunctionNames.SubWord, 2), (StringFunctionNames.SubWord, 3), (StringFunctionNames.Left, 2), (StringFunctionNames.Left, 3), (StringFunctionNames.Right, 2), (StringFunctionNames.Right, 3),
        (StringFunctionNames.SubString, 2), (StringFunctionNames.SubString, 3), (StringFunctionNames.Centre, 2), (StringFunctionNames.Centre, 3), (StringFunctionNames.Join, 2),
        (StringFunctionNames.ToUpper, 1), (StringFunctionNames.ToLower, 1), (StringFunctionNames.ListToFloat, 1), (StringFunctionNames.ListToInteger, 1)
    });

    /// <summary>Opt in before loading an original string image; no automatic backend fallback is performed.</summary>
    public static void Register(ModuleRegistry registry)
        => registry.InstallSourceWrappers(RegisterExports);

    private static void RegisterExports(ModuleRegistry registry)
    {
        foreach (var export in Exports)
        {
            // This entry is a runtime primitive, shared by both library backends.
            if (export is (StringFunctionNames.ListToFloat, 1))
            {
                continue;
            }

            string function = export.Name;
            registry.Register(
                Name,
                function,
                export.Arity,
                (context, arguments) => Invoke(context, function, arguments)
            );
        }
    }

    public static async ValueTask<Term> Invoke(ProcessContext context, string function, IReadOnlyList<Term> arguments)
    {
        if (!Exports.Contains((function, arguments.Count)))
        {
            throw new ErlangException(ErlangErrorReasons.UndefinedFunction);
        }

        using var tail = context.EnterSourceTailCall(Name, function);
        if (StringLegacy.Owns(function))
        {
            return await StringLegacyExecution.Invoke(context, function, arguments);
        }

        return await StringUnicode.Invoke(context, function, arguments);
    }
}
