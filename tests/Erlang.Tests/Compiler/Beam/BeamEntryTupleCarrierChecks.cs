using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamEntryTupleCarrierChecks
{
    internal const string TestName = "compiler/beam/reuses-entry-tuple-carriers-in-guards-and-calls";
    private const string Fixture = "entry_tuple_carrier_probe.erl";

    internal const string Source = """
        -module(entry_tuple_carrier_probe).
        -export([pick/3, observe/1, swapped/1, consume/2]).

        pick(Wanted, [{K,V}|Rest], {Previous,Payload}) ->
            if
                Wanted == K -> {K,V};
                Wanted > K -> ?MODULE:consume(Rest, {K,V});
                true -> {Previous,Payload}
            end;
        pick(_, _, _) -> absent.

        observe({A,B,C}) ->
            erlang:put(tuple_carrier_seen, {A,B,C}),
            ?MODULE:consume(C, {A,B,C}).

        swapped({A,B}) -> ?MODULE:consume(swapped, {B,A}).
        consume(_, Value) -> Value.
        """;

    internal static Task Run()
    {
        byte[] optimized = CodeGeneration.CompileBeamModule(Source, Fixture);
        if (TupleConstructors(optimized, "pick") != 0 || TupleConstructors(optimized, "observe") != 0
            || TupleConstructors(optimized, "swapped") != 1)
        {
            throw new InvalidOperationException(BeamEntryTupleCarrierDiagnostics.WrongConstructor);
        }

        var directory = Directory.CreateTempSubdirectory("erlang-beam-entry-tuple-carriers-");
        try
        {
            string path = Path.Combine(directory.FullName, Fixture);
            File.WriteAllText(path, Source);
            var profile = new BeamReleaseBuildProfile(
                "10.0.6",
                path,
                directory.FullName,
                [],
                [],
                EmitDocumentation: false
            );
            foreach (string flag in new[] { "no_alias", "no_copt", "beam_debug_info" })
            {
                byte[] unoptimized = CodeGeneration.CompileBeamFile(
                    path,
                    buildProfile: profile with { RecordedCompileOptions = [Term.A(flag)] }
                );
                if (TupleConstructors(unoptimized, "observe") == 0)
                {
                    throw new InvalidOperationException(BeamEntryTupleCarrierDiagnostics.DisabledOption(flag));
                }
            }
        }
        finally
        {
            directory.Delete(recursive: true);
        }

        return Task.CompletedTask;
    }

    private static int TupleConstructors(byte[] beam, string name)
    {
        object code = Code(beam);
        object[] instructions = Items(code, "Instructions").ToArray();
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        int start = Array.FindIndex(
            instructions,
            instruction => Opcode(instruction) == FunctionInfo
                && atoms[(int)(BigInteger)Property(Items(instruction, "Operands").ElementAt(1), "Value")] == name
        );
        if (start < 0)
        {
            throw new InvalidOperationException(BeamEntryTupleCarrierDiagnostics.MissingFunction(name));
        }

        return instructions.Skip(start + 1)
            .TakeWhile(instruction => Opcode(instruction) != FunctionInfo)
            .Count(instruction => Opcode(instruction) == PutTuple);
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
