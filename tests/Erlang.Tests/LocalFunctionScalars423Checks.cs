namespace Erlang.Tests;

internal static class LocalFunctionScalars423Checks
{
    private static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(LocalFunctionScalars423Names.Failure);
    }

    private static Term Field(byte[] wire, ref int offset)
    {
        byte[] packet = [LocalFunctionScalars423Names.Version, .. wire.AsSpan(offset).ToArray()];
        EtfDecodedTerm result = ExternalTermFormat.DecodeOne(packet, new EtfDecodeOptions { LocalSeed = LocalFunctionScalars423Names.Seed });
        offset += result.Used - LocalFunctionScalars423Names.PlainEnvelope;

        return result.Term;
    }

    private static void Scalar(
        byte[] wire,
        ref int offset,
        Integer value,
        bool local
    )
    {
        int start = offset;
        Require((wire[offset] == LocalFunctionScalars423Names.Local) == local);
        Require(Field(wire, ref offset).Equals(value));
        if (!local) return;
        byte[] scalar = [LocalFunctionScalars423Names.Version, .. wire.AsSpan(start, offset - start).ToArray()];
        scalar[LocalFunctionScalars423Names.LocalEnvelope - 1] ^= 1;
        bool refused = false;
        try
        {
            ExternalTermFormat.DecodeOne(scalar, new EtfDecodeOptions { LocalSeed = LocalFunctionScalars423Names.Seed });
        }
        catch (ErlangException error) when (error.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument)))
        {
            refused = true;
        }
        Require(refused);
    }

    private static void SizeAndIoVec(Term function, EtfEncodingOptions options, byte[] encoded)
    {
        Require(ExternalTermFormat.EncodedSize(function, options) == encoded.Length);
        byte[] flat = ExternalTermFormat.EncodeIoVec(function, options).SelectMany(part => part.ToArray()).ToArray();
        Require(flat.SequenceEqual(encoded));
    }

    internal static async Task Local()
    {
        await using var runtime = new ProcessRuntime();
        FunctionTerm function = (FunctionTerm)runtime.ImportExternalTerm(DistDecode423Bytes.StaticNormalFun).Term;
        foreach (int minor in new[] { 0, 1, 2 })
            foreach (bool local in new[] { false, true })
            {
                var options = new EtfEncodingOptions
                {
                    MinorVersion = minor,
                    Local = local,
                    LocalSeed = LocalFunctionScalars423Names.Seed,
                    LocalNode = LocalFunctionScalars423Names.Node,
                    FunctionOwner = runtime.InitializationPid
                };
                byte[] wire = ExternalTermFormat.Encode(function, options);
                SizeAndIoVec(function, options, wire);
                int start = local ? LocalFunctionScalars423Names.LocalEnvelope : LocalFunctionScalars423Names.PlainEnvelope;
                Require(wire[start] == LocalFunctionScalars423Names.NewFun);
                int offset = start + LocalFunctionScalars423Names.NewFunHeader;
                Require(Field(wire, ref offset).Equals(function.LocalIdentity!.Module));
                Scalar(
                    wire,
                    ref offset,
                    Term.I(function.LocalIdentity.OldIndex),
                    local
                );
                Scalar(
                    wire,
                    ref offset,
                    Term.I(function.LocalIdentity.OldUnique),
                    local
                );
            }
    }

    internal static async Task Export()
    {
        await using var runtime = new ProcessRuntime();
        FunctionTerm function = (FunctionTerm)runtime.ImportExternalTerm(LocalFunctionScalars423Names.ExportWire).Term;
        foreach (int minor in new[] { 0, 1, 2 })
            foreach (bool local in new[] { false, true })
            {
                var options = new EtfEncodingOptions
                {
                    MinorVersion = minor,
                    Local = local,
                    LocalSeed = LocalFunctionScalars423Names.Seed,
                    LocalNode = LocalFunctionScalars423Names.Node
                };
                byte[] wire = ExternalTermFormat.Encode(function, options);
                SizeAndIoVec(function, options, wire);
                int start = local ? LocalFunctionScalars423Names.LocalEnvelope : LocalFunctionScalars423Names.PlainEnvelope;
                Require(wire[start] == LocalFunctionScalars423Names.Export);
                int offset = start + LocalFunctionScalars423Names.ExportHeader;
                Require(Field(wire, ref offset).Equals(function.ExternalIdentity!.Module));
                Require(Field(wire, ref offset).Equals(function.ExternalIdentity!.Function));
                Scalar(
                    wire,
                    ref offset,
                    Term.I(function.Arity),
                    local
                );
                Require(offset == wire.Length);
            }
    }
}
