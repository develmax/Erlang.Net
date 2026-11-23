using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamMatchedTupleArgumentEffectChecks
{
    internal const string TestName = "compiler/beam/reuses-a-checked-nested-tuple-for-an-effectful-call";
    private const string Fixture = "beam_matched_tuple_argument_effects.erl";
    private const string WrongStructure = "A checked tuple argument was rebuilt or a different argument lost its constructor.";

    private const string Source = """
        -module(beam_matched_tuple_argument_effects).
        -export([forward/4,different/2,repeated/2,effect_fails/2]).
        forward(Key, _, {outer, {inner, Value}}, _) ->
            erlang:put(Key, {inner, Value}),
            done;
        forward(_, _, _, _) -> done.
        different(Key, {outer, {inner, Value}}) ->
            erlang:put(Key, {changed, Value}),
            done;
        different(_, _) -> done.
        repeated(Key, {outer, {inner, Key}}) ->
            erlang:put(Key, {inner, Key}),
            done;
        repeated(_, _) -> done.
        effect_fails(Target, {outer, {inner, Value}}) ->
            erlang:exit(Target,{inner, Value}),
            done;
        effect_fails(_, _) -> done.
        """;

    internal static Task Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, Fixture));
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions").ToArray();
        if (Operations("forward").Contains(PutTuple) || Operations("effect_fails").Contains(PutTuple)
            || !Operations("different").Contains(PutTuple))
        {
            throw new InvalidOperationException(WrongStructure);
        }

        return Task.CompletedTask;

        byte[] Operations(string name)
        {
            int entry = Array.FindIndex(
                instructions,
                instruction =>
                Opcode(instruction) == FunctionInfo
                && atoms[(int)(BigInteger)Property(Items(instruction, "Operands").ElementAt(1), "Value")] == name
            );
            if (entry < 0)
            {
                throw new InvalidOperationException(WrongStructure);
            }

            return instructions.Skip(entry + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo)
                .Select(Opcode).ToArray();
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static IEnumerable<object> Items(object owner, string name) =>
        ((IEnumerable)Property(owner, name)).Cast<object>();
}
