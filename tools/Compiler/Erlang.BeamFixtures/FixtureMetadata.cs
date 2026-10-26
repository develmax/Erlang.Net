using System.Globalization;
using System.Security.Cryptography;

namespace BeamFixtureExport;

internal static class FixtureMetadata
{
    public static async Task<FixtureMetadataEvidence> Run(
        string oracle,
        string output,
        FixtureDefinition fixture,
        string beamPath
    )
    {
        string prefix = Path.Combine(output, fixture.Module + FixtureMetadataNames.LogSuffix);
        string etf = Path.Combine(output, fixture.Module + FixtureMetadataNames.EtfSuffix);
        string text = Path.Combine(output, fixture.Module + FixtureMetadataNames.TextSuffix);
        string evaluation = prefix + FixtureMetadataNames.EvaluationSuffix;
        string expression = $"try {{ok,{{{fixture.Module},Md5}}}}=beam_lib:md5({String(beamPath)}), "
            + $"{{ok,{{{fixture.Module},Chunks}}}}=beam_lib:chunks({String(beamPath)},[attributes,compile_info,exports,locals,\"FunT\"], [allow_missing_chunks]), "
            + $"{{beam_file,{fixture.Module},_,_,_,Functions}}=beam_disasm:file({String(beamPath)}), "
            + "FunctionKeys=[{Name,Arity} || {function,Name,Arity,_,_} <- Functions], "
            + $"Data={{beam_metadata,{fixture.Module},Md5,Chunks,FunctionKeys}}, "
            + $"ok=file:write_file({String(etf)},term_to_binary(Data)), "
            + $"ok=file:write_file({String(text)},unicode:characters_to_binary(io_lib:format(\"~tp.~n\",[Data]))),halt(0) "
            + "catch Class:Reason:Stack -> io:format(standard_error,\"~tp~n\",[{Class,Reason,Stack}]),halt(4) end.";
        await File.WriteAllTextAsync(evaluation, expression + "\n");
        await OracleCommand.Run(oracle, [OracleCommandNames.NoShell, OracleCommandNames.Evaluate, expression], prefix);

        return new(
            int.Parse(await File.ReadAllTextAsync(prefix + OracleCommandNames.ExitCodeSuffix), CultureInfo.InvariantCulture),
            true,
            false,
            Path.GetFileName(etf),
            Hash(await File.ReadAllBytesAsync(etf)),
            Path.GetFileName(text),
            Hash(await File.ReadAllBytesAsync(text)),
            Path.GetFileName(evaluation),
            Hash(await File.ReadAllBytesAsync(evaluation)),
            Hash(await File.ReadAllBytesAsync(prefix + OracleCommandNames.StandardOutputSuffix)),
            Hash(await File.ReadAllBytesAsync(prefix + OracleCommandNames.StandardErrorSuffix))
        );
    }

    private static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));

    private static string String(string value) => "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n") + "\"";
}
