using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamCheckedTupleGuardProjectionChecks
{
    internal const string TestName = "compiler/beam/sinks-checked-tuple-fields-through-guards";
    private const string Fixture = "beam_guarded_projection_probe.erl";
    private const string WrongLayout = "The projected field crossed a guard with the wrong successor requirements.";
    private const string WrongResult = "Moving a projection changed the case result.";
    private const string Source = """
        -module(beam_guarded_projection_probe).
        -export([lookup/3, compared/3, second_tuple/2]).
        -record(entry, {key, value}).

        lookup(Entry, Wanted, Default) ->
            case Entry of
                {Wanted, Value} -> Value;
                _ -> Default
            end.

        compared(Entry, Minimum, Default) ->
            case Entry of
                #entry{key = Key, value = Value} when Key >= Minimum -> Value;
                _ -> Default
            end.

        second_tuple({event, Pid, _}, {Witness, _, _}) when Pid =:= Witness -> accepted;
        second_tuple(_, State) -> {ok, State}.
        """;

    internal static async Task Run()
    {
        byte[] beam = CodeGeneration.CompileBeamModule(Source, Fixture);
        CheckLayouts(beam);
        var module = BeamModule.Prepare(beam);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term first = Term.A("first");
            Term second = Term.A("second");
            Term value = Term.A("value");
            Term fallback = Term.A("fallback");
            Term receiver = Term.Tuple(Term.A("event"), first, value);
            Term witness = Term.Tuple(first, second, value);
            Term rejected = Term.Tuple(second, first, value);
            Term pair = Term.Tuple(first, value);
            Term entry = Term.Tuple(Term.A("entry"), second, value);
            if (!Term.A("accepted").Equals(await module.Invoke(
                context,
                "second_tuple",
                receiver,
                witness
            ))
                || !Term.Tuple(Term.A("ok"), rejected).Equals(await module.Invoke(
                    context,
                    "second_tuple",
                    receiver,
                    rejected
                ))
                || !Term.Tuple(Term.A("ok"), fallback).Equals(await module.Invoke(
                    context,
                    "second_tuple",
                    receiver,
                    fallback
                ))
                || !value.Equals(await module.Invoke(
                    context,
                    "lookup",
                    pair,
                    first,
                    fallback
                ))
                || !fallback.Equals(await module.Invoke(
                    context,
                    "lookup",
                    pair,
                    second,
                    fallback
                ))
                || !value.Equals(await module.Invoke(
                    context,
                    "compared",
                    entry,
                    first,
                    fallback
                ))
                || !fallback.Equals(await module.Invoke(
                    context,
                    "compared",
                    entry,
                    value,
                    fallback
                )))
            {
                throw new InvalidOperationException(WrongResult);
            }

            return Nil.Value;
        });
    }

    private static void CheckLayouts(byte[] beam)
    {
        object code = Code(beam);
        object[] instructions = Children(code, "Instructions").ToArray();
        string[] atoms = Children(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] positive = Body("second_tuple");
        int guard = Array.FindIndex(positive, instruction => Opcode(instruction) == TestArity);
        int field = Array.FindIndex(positive, instruction => Opcode(instruction) == GetTupleElement);
        byte[] negative = Body("compared").Select(Opcode).ToArray();
        int compare = Array.IndexOf(negative, IsLess);
        int projectedValue = Array.LastIndexOf(negative, GetTupleElement);
        if (guard < 0 || field <= guard || projectedValue < 0 || compare <= projectedValue)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        object[] Body(string name)
        {
            int entry = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                && atoms[(int)Value(Children(instruction, "Operands").ElementAt(1))] == name
            );

            return instructions.Skip(entry + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static BigInteger Value(object operand) => (BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Children(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
