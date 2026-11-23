using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamCheckedFieldCallFrameChecks
{
    internal const string TestName = "compiler/beam/allocates-before-a-checked-call-field";
    private const string Module = "beam_checked_field_call_frame";
    private const string ShapeError = "The checked field call frame retained a dead discriminator.";
    private const string ValueError = "The checked field call changed the selected result.";

    private const string Source = """
        -module(beam_checked_field_call_frame).
        -export([scan/3, third_arg/3]).

        scan(_, _, []) -> no;
        scan(Path, Key, [{Key,Head}|_]) -> {yes, Head ++ Path};
        scan(Path, Key, [_|Rest]) -> scan(Path, Key, Rest).

        third_arg(_, _, []) -> no;
        third_arg(Path, Key, [{Key,Head}|_]) -> {yes, lists:sublist(Head, Path, Key)};
        third_arg(Path, Key, [_|Rest]) -> third_arg(Path, Key, Rest).
        """;

    internal static async Task Run()
    {
        byte[] beam = CodeGeneration.CompileBeamModule(Source, Module + ".erl");
        object code = Code(beam);
        object[] instructions = Children(code, "Instructions");
        string[] atoms = Children(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        if (!HasMovedField(Body("scan")) || HasMovedField(Body("third_arg")))
        {
            throw new InvalidOperationException(ShapeError);
        }

        var module = BeamModule.Prepare(beam);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term key = Term.A("key");
            Term other = Term.A("other");
            Term tail = Term.List(Term.I(2));
            Term first = Term.List(Term.I(1));
            Term items = Term.List(Term.Tuple(other, first), Term.Tuple(key, first));
            Term expected = Term.Tuple(Term.A("yes"), Term.List(Term.I(1), Term.I(2)));
            if (!expected.Equals(await module.Invoke(
                context,
                "scan",
                tail,
                key,
                items
            )) || !Term.A("no").Equals(await module.Invoke(
                context,
                "scan",
                tail,
                key,
                Term.List(Term.Tuple(other, first))
            )))
            {
                throw new InvalidOperationException(ValueError);
            }

            return Nil.Value;
        });

        object[] Body(string name)
        {
            int start = Array.FindIndex(instructions, instruction => Opcode(instruction) == FunctionInfo
                && atoms[Value(Children(instruction, "Operands")[1])] == name);

            return start < 0 ? [] : instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        }
    }

    private static bool HasMovedField(object[] instructions)
    {
        for (int index = 0; index + 2 < instructions.Length; index++)
        {
            if (Opcode(instructions[index]) == Allocate
                && Children(instructions[index], "Operands").Select(Value).SequenceEqual([0, 4])
                && Opcode(instructions[index + 1]) == GetTupleElement
                && Opcode(instructions[index + 2]) == Swap)
            {
                return true;
            }
        }

        return false;
    }

    private static object[] Children(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");
}
