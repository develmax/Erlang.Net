using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamOwnedRecordUpdateChecks
{
    internal const string TestName = "compiler/beam/reallocates-owned-record-update-branches";
    private const string Fixture = "beam_owned_record_update_probe.erl";
    private const string WrongRegisters = "The record update used the wrong frame, operands, or heap roots.";
    private const string WrongResult = "An owned record update changed a value, cold return, or effect.";

    private const string Source = """
        -module(beam_owned_record_update_probe).
        -export([guarded/3,repeated/2,after_call/3,read_update/0]).
        -record(row,{first,second,third}).
        guarded(#row{}=Record, First, Third) ->
            Updated = Record#row{first=First,third=Third},
            {Updated,[First,Third],[Record]};
        guarded(_, _, _) -> fallback.
        repeated(#row{}=Record, Value) ->
            Updated = Record#row{first=Value,third=Value},
            {Updated,[Value],[Record]};
        repeated(_, _) -> fallback.
        after_call(#row{}=Record, First, Third) ->
            Updated = Record#row{first=First,third=Third},
            erlang:put(updated,Updated),
            {Updated,[First,Third],[Record]};
        after_call(_, _, _) -> fallback.
        read_update() -> erlang:get(updated).
        """;

    internal static async Task Run()
    {
        byte[] beam = CodeGeneration.CompileBeamModule(Source, Fixture);
        CheckInstructions(beam);
        var module = BeamModule.Prepare(beam);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term row = Term.A("row");
            Term first = Term.A("first");
            Term third = Term.A("third");
            Term none = Term.A("none");
            Term fallback = Term.A("fallback");
            Term original = Term.Tuple(
                row,
                none,
                none,
                none
            );
            Term updated = Term.Tuple(
                row,
                first,
                none,
                third
            );
            Term repeated = Term.Tuple(
                row,
                first,
                none,
                first
            );
            Term expected = Term.Tuple(updated, Term.List(first, third), Term.List(original));
            Term expectedRepeated = Term.Tuple(repeated, Term.List(first), Term.List(original));
            if (!expected.Equals(await module.Invoke(
                context,
                "guarded",
                original,
                first,
                third
            ))
                || !expectedRepeated.Equals(await module.Invoke(
                    context,
                    "repeated",
                    original,
                    first
                ))
                || !fallback.Equals(await module.Invoke(
                    context,
                    "guarded",
                    Term.Tuple(
                        Term.A("other"),
                        none,
                        none,
                        none
                    ),
                    first,
                    third
                ))
                || !fallback.Equals(await module.Invoke(
                    context,
                    "repeated",
                    none,
                    first
                ))
                || !expected.Equals(await module.Invoke(
                    context,
                    "after_call",
                    original,
                    first,
                    third
                ))
                || !updated.Equals(await module.Invoke(context, "read_update")))
            {
                throw new InvalidOperationException(WrongResult);
            }

            return Nil.Value;
        });
    }

    private static void CheckInstructions(byte[] beam)
    {
        object code = Code(beam);
        object[] instructions = Children(code, "Instructions").ToArray();
        string[] atoms = Children(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        CheckFrameless(
            "guarded",
            15,
            3,
            2
        );
        CheckFrameless(
            "repeated",
            13,
            2,
            1
        );

        object[] blocked = Body("after_call");
        if (!blocked.Any(instruction => Opcode(instruction) == AllocateHeap)
            || !blocked.Any(instruction => Opcode(instruction) == Deallocate)
            || !blocked.Any(instruction => Opcode(instruction) == ExternalCall))
        {
            throw new InvalidOperationException(WrongRegisters);
        }

        void CheckFrameless(
            string function,
            int words,
            int live,
            int lastReplacement
        )
        {
            object[] body = Body(function);
            object[] update = Children(body.Single(instruction => Opcode(instruction) == UpdateRecord), "Operands").ToArray();
            object[] heap = Children(body.Single(instruction => Opcode(instruction) == TestHeap), "Operands").ToArray();
            if (body.Any(instruction => Opcode(instruction) is Allocate or AllocateHeap or Deallocate)
                || Value(heap[0]) != words || Value(heap[1]) != live
                || Property(update[2], "TypeIndex") is not int
                || Property(update[2], "Kind") is not BeamBinaryHostNames.X
                || Property(update[3], "Kind") is not BeamBinaryHostNames.X
                || Property(update[6], "Kind") is not BeamBinaryHostNames.X || Value(update[6]) != 1
                || Property(update[8], "Kind") is not BeamBinaryHostNames.X || Value(update[8]) != lastReplacement)
            {
                throw new InvalidOperationException(WrongRegisters);
            }
        }

        object[] Body(string function)
        {
            int entry = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                && atoms[(int)Value(Children(instruction, "Operands").ElementAt(1))] == function
            );

            return instructions.Skip(entry + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static BigInteger Value(object operand) => (BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Children(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
