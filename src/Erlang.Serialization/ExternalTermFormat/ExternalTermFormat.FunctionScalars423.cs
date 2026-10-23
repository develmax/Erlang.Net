namespace Erlang;

public static partial class ExternalTermFormat
{
    private static void WriteFunctionScalar423(
        Stream target,
        Integer value,
        int depth,
        EtfEncodingOptions options
    )
    {
        if (!options.Local)
        {
            Write(
                target,
                value,
                depth,
                options
            );

            return;
        }
        if (depth > EtfResourceLimits.MaximumNestingDepth)
            throw new ErlangException(ErlangErrorReasons.SystemLimit);

        byte[] encoded = Encode(
            value,
            options with { CompressionLevel = EtfEncodingParameters.Uncompressed }
        );
        target.Write(encoded.AsSpan(EtfHeaderLayout.PlainPayloadOffset));
    }
}
