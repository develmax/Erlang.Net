using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamClosedIntegerPatternMatrixChecks
{
    internal const string TestName = "compiler/beam/groups-closed-integer-pattern-columns";
    private const string WrongLayout = "Expected two ordered integer selector columns and one shared row.";
    private const string WrongResult = "The integer selector changed a value or the original failure arguments.";
    private const string Source = """
        -module(beam_closed_integer_matrix).
        -export([table/2, priority/2]).

        table(1, 8) -> high_one;
        table(1, 2) -> low_one;
        table(1, 4) -> mid_one;
        table(3, 8) -> high;
        table(3, 2) -> low;
        table(3, 4) -> mid;
        table(4, 4) -> mid;
        table(4, 2) -> low;
        table(4, 8) -> high;
        table(2, _) -> fallback.

        priority(1, _) -> first;
        priority(1, 2) -> second;
        priority(2, 1) -> other;
        priority(3, 1) -> another.
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, "beam_closed_integer_matrix.erl");
        CheckLayout(image);
        BeamModule module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term two = Term.I(2);
            if (!Term.A("low_one").Equals(await module.Invoke(
                context,
                "table",
                Term.I(1),
                two
            ))
                || !Term.A("fallback").Equals(await module.Invoke(
                    context,
                    "table",
                    two,
                    Term.A("anything")
                ))
                || !Term.A("mid").Equals(await module.Invoke(
                    context,
                    "table",
                    Term.I(4),
                    Term.I(4)
                ))
                || !Term.A("high").Equals(await module.Invoke(
                    context,
                    "table",
                    Term.I(3),
                    Term.I(8)
                ))
                || !Term.A("first").Equals(await module.Invoke(
                    context,
                    "priority",
                    Term.I(1),
                    two
                )))
            {
                throw new InvalidOperationException(WrongResult);
            }

            await RequireFailure(Term.I(9), two);
            await RequireFailure(Term.I(3), Term.I(7));

            return Nil.Value;

            async Task RequireFailure(Term first, Term second)
            {
                try
                {
                    _ = await module.Invoke(
                        context,
                        "table",
                        first,
                        second
                    );
                }
                catch (ErlangException error) when (error.Reason.Equals(Term.A(ErlangErrorReasons.FunctionClause))
                    && error.StackTraceTerm is Cons { Head: TupleTerm { Items: [_, Atom { Name: "table" }, var arguments, _] } }
                    && Cons.Items(arguments).SequenceEqual(new[] { first, second }))
                {
                    return;
                }

                throw new InvalidOperationException(WrongResult);
            }
        });
    }

    private static void CheckLayout(byte[] image)
    {
        object code = Code(image);
        object[] instructions = Items(code, "Instructions");
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        int info = Array.FindIndex(instructions, instruction => Opcode(instruction) == FunctionInfo
            && atoms[Value(Operands(instruction)[1])] == "table");
        if (info < 0)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        object[][] selects = instructions.Skip(info + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo)
            .Where(instruction => Opcode(instruction) == SelectValue).Select(Operands).ToArray();
        if (selects.Length != 3 || !HasTable(selects[0], 0, [1, 2, 3, 4])
            || !HasTable(selects[1], 1, [2, 4, 8]) || !HasTable(selects[2], 1, [2, 4, 8])
            || Value(selects[0][8]) != Value(selects[0][10])
            || selects.Skip(1).Any(select => Value(select[1]) != Value(selects[0][1])))
        {
            throw new InvalidOperationException(WrongLayout);
        }
    }

    private static bool HasTable(object[] operands, int register, int[] keys) =>
        operands.Length == 3 + 2 * keys.Length && Value(operands[0]) == register
        && Enumerable.Range(0, keys.Length).All(index => Value(operands[3 + 2 * index]) == keys[index]);

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static object[] Operands(object instruction) => Items(instruction, "Operands");

    private static object[] Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();
}
