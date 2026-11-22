using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamOperatorShortCircuitChecks
{
    internal const string TestName = "compiler/beam/short-circuits-proven-operator-results";
    private const string Fixture = "beam_operator_short_circuit.erl";
    private const string IncorrectCheck = "An operator with a proven boolean result requires only its short-circuit test.";
    private const string IncorrectValue = "Short-circuiting an operator changed its value, error or right-side evaluation.";

    private const string Source = """
        -module(beam_operator_short_circuit).
        %% options
        -export([compare/3, negate/2, opaque/2, strict/3, nested/2]).
        compare(A, B, Tail) -> {value, (A =:= B) orelse hd(Tail)}.
        negate(A, Tail) -> {value, (not A) andalso hd(Tail)}.
        strict(A, B, Tail) -> {value, (A xor B) orelse hd(Tail)}.
        opaque(A, Tail) -> {value, A orelse hd(Tail)}.
        nested(A, Tail) -> {value, (A orelse arbitrary) andalso hd(Tail)}.
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, Fixture);
        byte[] disabled = CodeGeneration.CompileBeamModule(
            Source.Replace("%% options", "-compile(no_ssa_opt).", StringComparison.Ordinal),
            Fixture
        );
        if (BooleanChecks(image, "compare") != 1 || BooleanChecks(image, "negate") != 1
            || BooleanChecks(image, "strict") != 1 || BooleanChecks(image, "opaque") < 2
            || BooleanChecks(image, "nested") < 2 || BooleanChecks(disabled, "negate") < 2)
        {
            throw new InvalidOperationException(IncorrectCheck);
        }

        BeamModule module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term yes = Term.A("true");
            Term no = Term.A("false");
            Term item = Term.A("item");
            Term tail = Term.List(item);
            await Check(
                "compare",
                yes,
                item,
                item,
                Nil.Value
            );
            await Check(
                "compare",
                item,
                item,
                yes,
                tail
            );
            await Check(
                "negate",
                no,
                yes,
                Nil.Value
            );
            await Check(
                "negate",
                item,
                no,
                tail
            );
            await Check(
                "strict",
                yes,
                yes,
                no,
                Nil.Value
            );
            await Check(
                "strict",
                item,
                yes,
                yes,
                tail
            );
            await Check(
                "nested",
                item,
                yes,
                tail
            );
            try
            {
                await module.Invoke(
                    context,
                    "negate",
                    item,
                    Nil.Value
                );
            }
            catch (ErlangException error) when (error.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument)))
            {
                return Nil.Value;
            }

            throw new InvalidOperationException(IncorrectValue);

            async Task Check(string function, Term expected, params Term[] args)
            {
                Term observed = await module.Invoke(context, function, args);
                if (!observed.Equals(Term.Tuple(Term.A("value"), expected)))
                {
                    throw new InvalidOperationException(IncorrectValue);
                }
            }
        });
    }

    private static int BooleanChecks(byte[] image, string name)
    {
        object code = Code(image);
        object[] instructions = Items(code, "Instructions");
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        int entry = Array.FindIndex(
            instructions,
            instruction => Opcode(instruction) == FunctionInfo
                && Items(instruction, "Operands") is [_, var function, _]
                && atoms[Value(function)] == name
        );
        if (entry < 0)
        {
            throw new InvalidOperationException(IncorrectCheck);
        }

        return instructions.Skip(entry + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo)
            .Count(instruction => Opcode(instruction) is ExactEqualTest or ExactNotEqualTest
                && Items(instruction, "Operands") is [_, _, var operand]
                && Property(operand, "Kind").ToString() == "Atom" && atoms[Value(operand)] is "true" or "false");
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();
}
