using Erlang.Compiler;
using static Erlang.Tests.BeamDocumentationEmissionChecks;

namespace Erlang.Tests;

internal static class BeamLineFilenameChecks
{
    internal const string TestName = "compiler/beam/module-filename-uses-implicit-line-index";
    private const string Fixture = "beam_implicit_line_file";
    private const string LineChunk = "Line";
    private const string Failure = "The module filename did not use the implicit BEAM line table index.";

    // Line section from OTP-29.1.1 erlc without deterministic filename rewriting.
    private const string ExpectedLine =
        "0000000000000000000000050000000200000001092B12092B" +
        "00216F746865722F6265616D5F696D706C696369745F6C696E655F66696C652E65726C";

    internal static Task Run()
    {
        byte[] expected = Convert.FromHexString(ExpectedLine);
        var directory = Directory.CreateTempSubdirectory("erlang-beam-line-filename-");
        try
        {
            string sourcePath = Path.Combine(directory.FullName, Fixture + ".erl");
            File.WriteAllBytes(sourcePath, BeamModuleEmissionChecks.Read(Fixture + ".erl"));

            // A non-deterministic build preserves the explicitly supplied relative filename.
            var profile = new BeamReleaseBuildProfile(
                "10.0.6",
                sourcePath,
                directory.FullName,
                [],
                [],
                RecordedCompileOptions: [],
                EmitAbstractCode: false,
                EmitDocumentation: false
            );
            byte[] image = CodeGeneration.CompileBeamFile(sourcePath, buildProfile: profile);
            byte[] actual = Chunks(image).Single(chunk => chunk.Id == LineChunk).Data;
            if (!actual.AsSpan().SequenceEqual(expected))
            {
                throw new InvalidOperationException(Failure);
            }
        }
        finally
        {
            directory.Delete(recursive: true);
        }

        return Task.CompletedTask;
    }
}
