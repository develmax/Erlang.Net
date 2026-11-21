using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamUnreachableRecordFailureChecks
{
    internal const string TestName = "compiler/beam/prunes-unreachable-record-failure-between-live-blocks";
    private const string Fixture = "dead_record_exit_probe.erl";
    private const string WrongFailures = "The proven input record must have no failure exit. The live exits must remain.";
    private const string Source = """
        -module(dead_record_exit_probe).
        -export([run/1, field/1]).
        -record(context, {data, mode}).
        -record(result, {size}).

        run(Info) ->
            case Info#context.mode of
                use -> worker(Info);
                _ -> other
            end.

        worker(Info) ->
            Data = Info#context.data,
            case erlang:get(Data) of
                {ok, Found} -> {found, Found#result.size};
                missing -> absent
            end.

        field(Info) -> Info#context.data.
        """;

    internal static Task Run()
    {
        byte[] binary = CodeGeneration.CompileBeamModule(Source, Fixture);
        object code = Code(binary);
        object[] instructions = Items(code, "Instructions").ToArray();
        var atoms = Items(Property(code, "Structure"), "Atoms").Cast<string?>().ToArray();
        object[] worker = Body("worker");
        if (worker.Count(instruction => Opcode(instruction) == BadRecord) != 1
            || worker.Count(instruction => Opcode(instruction) == CaseEnd) != 1
            || Body("run").Count(instruction => Opcode(instruction) == BadRecord) != 1
            || Body("field").Count(instruction => Opcode(instruction) == BadRecord) != 1)
        {
            throw new InvalidOperationException(WrongFailures);
        }

        return Task.CompletedTask;

        object[] Body(string name)
        {
            int info = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                && atoms[(int)(BigInteger)Property(Items(instruction, "Operands").ElementAt(1), "Value")] == name
            );

            return instructions.Skip(info + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        }
    }

    private static byte Opcode(object instruction) =>
        (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static IEnumerable<object> Items(object container, string name) =>
        ((IEnumerable)Property(container, name)).Cast<object>();
}
