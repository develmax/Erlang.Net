using System.Security.Cryptography;

namespace BeamFixtureExport;

internal static class FixtureExecution
{
    public static async Task<FixtureExecutionEvidence> Run(
        string oracle,
        string output,
        FixtureDefinition fixture,
        bool requested
    )
    {
        if (!requested || fixture.IndependentExpectedExpression is null)
        {
            return new(
                false,
                requested ? FixtureExecutionNames.OnLoadNotObserved : FixtureExecutionNames.CompilationOnly,
                fixture.IndependentExpectedExpression,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null
            );
        }
        string actualPath = Path.Combine(output, fixture.Module + FixtureExecutionFiles.ActualEtfSuffix);
        string expectedPath = Path.Combine(output, fixture.Module + FixtureExecutionFiles.ExpectedEtfSuffix);
        string prefix = Path.Combine(output, fixture.Module + FixtureExecutionFiles.LogSuffix);
        string expression = $"try {{module,{fixture.Module}}}=code:load_abs({String(Path.Combine(output, fixture.Module))}), "
            + $"Expected={fixture.IndependentExpectedExpression}, Actual={fixture.Module}:run(), "
            + $"ok=file:write_file({String(actualPath)},term_to_binary({{ok,Actual}})), "
            + $"ok=file:write_file({String(expectedPath)},term_to_binary({{ok,Expected}})), "
            + "case Actual =:= Expected of true -> halt(0); false -> io:format(standard_error,\"Actual=~tp Expected=~tp~n\",[Actual,Expected]),halt(2) end "
            + $"catch Class:Reason:Stack -> ok=file:write_file({String(actualPath)},term_to_binary({{Class,Reason}})), "
            + "io:format(standard_error,\"~tp~n\",[{Class,Reason,Stack}]),halt(3) end.";
        string evaluationPath = prefix + FixtureExecutionFiles.EvaluationSuffix;
        await File.WriteAllTextAsync(evaluationPath, expression + "\n");
        await OracleCommand.Run(oracle, [OracleCommandNames.NoShell, OracleCommandNames.Evaluate, expression], prefix);
        byte[] actual = await File.ReadAllBytesAsync(actualPath);
        byte[] expected = await File.ReadAllBytesAsync(expectedPath);
        if (!actual.AsSpan().SequenceEqual(expected))
        {
            throw new InvalidDataException(FixtureExecutionDiagnostics.EtfMismatch(fixture.Module));
        }

        return new(
            true,
            null,
            fixture.IndependentExpectedExpression,
            int.Parse(await File.ReadAllTextAsync(prefix + OracleCommandNames.ExitCodeSuffix), System.Globalization.CultureInfo.InvariantCulture),
            Path.GetFileName(actualPath),
            Hash(actual),
            Path.GetFileName(expectedPath),
            Hash(expected),
            Path.GetFileName(evaluationPath),
            Hash(await File.ReadAllBytesAsync(evaluationPath)),
            Hash(await File.ReadAllBytesAsync(prefix + OracleCommandNames.StandardOutputSuffix)),
            Hash(await File.ReadAllBytesAsync(prefix + OracleCommandNames.StandardErrorSuffix))
        );
    }

    private static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));

    private static string String(string value) => "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n") + "\"";
}
