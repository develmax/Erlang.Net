using System.Text;
using Erlang.Compiler;

namespace Erlang.Tests;

internal static class BeamReconstructedTupleChecks
{
    internal const string TestName = "compiler/beam/tagged-case-reuses-the-matched-tuple";
    private const string Module = "beam_reconstructed_tuple";

    internal static Task Run()
    {
        string file = Module + ".erl";
        string source = Encoding.UTF8.GetString(BeamModuleEmissionChecks.Read(file));
        byte[] reference = BeamModuleEmissionChecks.Read(Module + ".beam");
        byte[] reused = CodeGeneration.CompileBeamModule(source, file);
        byte[] changed = CodeGeneration.CompileBeamModule(
            source.Replace("-> {error, Reason}", "-> {diagnostic, Reason}", StringComparison.Ordinal),
            file
        );
        if (ConstructsTuple(reused))
        {
            throw new InvalidOperationException(BeamReconstructedTupleDiagnostics.Rebuilt);
        }

        if (!reference.AsSpan().SequenceEqual(reused))
        {
            throw new InvalidOperationException(BeamReconstructedTupleDiagnostics.Dispatch);
        }

        if (!ConstructsTuple(changed))
        {
            throw new InvalidOperationException(BeamReconstructedTupleDiagnostics.BadReuse);
        }

        _ = BeamModule.Prepare(reused);
        _ = BeamModule.Prepare(changed);

        return Task.CompletedTask;
    }

    private static bool ConstructsTuple(byte[] beam)
    {
        byte[] code = BeamDocumentationEmissionChecks.Chunks(beam).Single(chunk => chunk.Id == "Code").Data;
        ReadOnlySpan<byte> tuple = [BeamEmissionFormat.PutTuple, BeamEmissionFormat.XRegisterTag, BeamEmissionFormat.ExtendedListOperand, 0x20];

        return code.AsSpan().IndexOf(tuple) >= 0;
    }
}
