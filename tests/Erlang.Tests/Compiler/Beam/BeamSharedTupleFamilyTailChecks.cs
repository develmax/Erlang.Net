using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamSharedTupleFamilyTailChecks
{
    internal const string TestName = "compiler/beam/groups-tuple-family-aliases-before-local-tail";
    private const string Fixture = "beam_shared_tuple_family_tail_probe.erl";
    private const string UnexpectedLayout = "The shared tuple family dispatch must preserve its two distinct fallback edges.";

    private const string Source = """
        -module(beam_shared_tuple_family_tail_probe).
        -export([route/3]).
        route({work, local, beta=Key, Payload}, From, State) -> finish(Key, Payload, From, State);
        route({work, local, alpha=Key, Payload}, From, State) -> finish(Key, Payload, From, State);
        route({work, Family, Member, Payload}, From, State) ->
            Worker = prepare(Family, Member, Payload, From),
            _Pid = erlang:spawn_link(Worker),
            {deferred, State};
        route(Msg, _From, State) ->
            lists:prefix([], [probe, Msg]),
            {reply, rejected, State}.
        finish(Key, Payload, From, State) -> {Key, Payload, From, State}.
        prepare(Family, Member, Payload, From) -> fun() -> {Family, Member, Payload, From} end.
        """;

    internal static Task Run()
    {
        byte[] prefix = [IsTaggedTuple, GetTupleElement, GetTupleElement, GetTupleElement, ExactEqualTest, SelectValue];
        object[] body = Body(Source);
        object[] guard = Body(Source.Replace(
            "beta=Key, Payload}, From, State) ->",
            "beta=Key, Payload}, From, State) when is_atom(From) ->",
            StringComparison.Ordinal
        ));
        if (!body.Take(prefix.Length).Select(Opcode).SequenceEqual(prefix)
            || guard.Take(prefix.Length).Select(Opcode).SequenceEqual(prefix)
            || body.Count(instruction => Opcode(instruction) == TailLocalCall) != 1
            || body.Count(instruction => Opcode(instruction) is Allocate or AllocateHeap) != 2)
        {
            throw new InvalidOperationException(UnexpectedLayout);
        }

        int report = Value(Operands(body[0])[0]);
        int general = Value(Operands(body[4])[0]);
        object[] selection = Operands(body[5]);
        if (report == general || Value(selection[1]) != general
            || Value(selection[4]) != Value(selection[6]) || Value(selection[4]) == general
            || Value(selection[4]) == report)
        {
            throw new InvalidOperationException(UnexpectedLayout);
        }

        return Task.CompletedTask;
    }

    private static object[] Body(string source)
    {
        object code = Code(CodeGeneration.CompileBeamModule(source, Fixture));
        object[] instructions = Items(code, "Instructions").ToArray();
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        int entry = Array.FindIndex(
            instructions,
            instruction => Opcode(instruction) == FunctionInfo
                && Operands(instruction) is [_, var name, var arity] && atoms[Value(name)] == "route" && Value(arity) == 3
        );
        if (entry < 0)
        {
            throw new InvalidOperationException(UnexpectedLayout);
        }

        return instructions.Skip(entry + 2).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
    }

    private static object[] Operands(object instruction) => Items(instruction, "Operands").ToArray();

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
