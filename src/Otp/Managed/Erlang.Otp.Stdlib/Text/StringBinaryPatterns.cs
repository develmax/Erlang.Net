namespace Erlang;

internal static class StringBinaryPatterns
{
    internal static Term Encode(ProcessContext context, Term codepoints)
    {
        var patterns = new List<Term>();
        using var pending = new StringSourceContinuations(context, StringSeparatorSource.BinaryPattern, 1);
        foreach (Term cp in Cons.Items(codepoints))
        {
            try
            {
                patterns.Add(BeamBinaryConstructionValues.Utf8(cp));
            }
            catch (ErlangException error)
            {
                using var origin = context.EnterGeneratedFunction(
                    StringModule.Name,
                    StringSeparatorSource.BinaryPattern,
                    1,
                    StringSourceTrace.Location(StringSeparatorSource.BinaryPatternEncode),
                    new object()
                );
                throw BeamBinaryConstructionValues.CaptureFailure(
                    context,
                    error,
                    1,
                    BeamBinaryNames.Utf8Segment,
                    cp
                );
            }
            pending.Push(StringSeparatorSource.BinaryPatternEncode);
        }

        return Cons.From(patterns);
    }
}
