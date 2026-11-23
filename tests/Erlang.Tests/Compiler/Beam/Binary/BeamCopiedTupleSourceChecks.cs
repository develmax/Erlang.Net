using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamCopiedTupleSourceChecks
{
    internal const string TestName = "compiler/beam/prefers-live-x-tuple-source-after-retaining-y";
    private const string Fixture = "beam_preferred_tuple_source_probe.erl";
    private const string WrongSource = "The tuple read must use the live copy and retain Y after the X register is overwritten.";
    private const string Source = """
        -module(beam_preferred_tuple_source_probe).
        -export([run/1, post_call/1]).
        -record(payload,{first,second,third}).

        run(Record=#payload{}) ->
            Pair = store(Record#payload.first, Record#payload.second),
            {Pair, Record#payload.third}.

        post_call(Record=#payload{}) ->
            Applied = store(tag, Record),
            {Applied, Record#payload.first}.

        store(Left, Right) -> {Left,Right}.
        """;

    internal static Task Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, Fixture));
        object[] instructions = Items(code, "Instructions").ToArray();
        var atoms = Items(Property(code, "Structure"), "Atoms").Cast<string?>().ToArray();
        object[] run = FindBody("run");
        object[] postCall = FindBody("post_call");
        int first = Array.FindIndex(run, instruction => Opcode(instruction) == GetTupleElement);
        int call = Array.FindIndex(postCall, instruction => Opcode(instruction) == LocalCall);
        if (first < 0 || first + 1 >= run.Length || call < 0
            || !ReadsFrom(run[first], "x") || !ReadsFrom(run[first + 1], "y")
            || !postCall.Skip(call + 1).Any(instruction => ReadsFrom(instruction, "y")))
        {
            throw new InvalidOperationException(WrongSource);
        }

        return Task.CompletedTask;

        object[] FindBody(string name)
        {
            int entry = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                && atoms[(int)(BigInteger)Property(Items(instruction, "Operands").ElementAt(1), "Value")] == name
            );

            return instructions.Skip(entry + 2).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        }
    }

    private static bool ReadsFrom(object instruction, string register) =>
        Opcode(instruction) == GetTupleElement
        && (string)Property(Items(instruction, "Operands").First(), "Kind") == register;

    private static byte Opcode(object instruction) =>
        (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static IEnumerable<object> Items(object container, string name) =>
        ((IEnumerable)Property(container, name)).Cast<object>();
}
