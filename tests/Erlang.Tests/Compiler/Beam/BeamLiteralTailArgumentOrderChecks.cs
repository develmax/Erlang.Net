using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamLiteralTailArgumentOrderChecks
{
    internal const string TestName = "compiler/beam/orders-appended-literal-tail-arguments";
    private const string ArgumentDependencyFailure = "Dependent argument reads or a preceding call were lost.";
    private const string LiteralArgumentOrderFailure = "Appended literals were emitted out of call setup order.";
    internal const string Source = """
        -module(beam_constant_tail_arguments_probe).
        -export([local/3, remote/2, mixed_dependencies/3, sequence/0, contextual/2, target/5]).
        local(W, P, F) -> target(W, P, F, "caption", 17).
        remote(W, P) -> ?MODULE:target(W, P, fixed, "caption", 17).
        mixed_dependencies(A, B, C) -> target(B, C, A, 1, 2).
        contextual(W, P) -> target(W, P, fixed, "caption", 17).
        sequence() -> erlang:put(stage, first), target(ok, done, step, 1, 2).
        target(A, B, C, D, E) -> {A,B,C,D,E}.
        """;

    internal static Task Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, "beam_constant_tail_arguments_probe.erl"));
        object[] instructions = Items(code, "Instructions").ToArray();
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        Check("local", [4, 3]);
        Check("remote", [3, 4, 2]);
        Check("contextual", [3, 4, 2]);
        if (!Body("mixed_dependencies").Any(instruction => Opcode(instruction) == Swap)
            || !Body("sequence").Any(instruction => Opcode(instruction) == ExternalCall))
        {
            throw new InvalidOperationException(ArgumentDependencyFailure);
        }

        return Task.CompletedTask;

        void Check(string name, int[] expected)
        {
            int[] actual = Body(name).Where(item => Opcode(item) == Move)
                .Select(item => (int)(BigInteger)Property(Items(item, "Operands").Last(), "Value")).ToArray();
            if (!actual.SequenceEqual(expected))
            {
                throw new InvalidOperationException(LiteralArgumentOrderFailure);
            }
        }

        IEnumerable<object> Body(string name)
        {
            int first = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                    && atoms[(int)(BigInteger)Property(Items(instruction, "Operands").ElementAt(1), "Value")] == name
            );

            return instructions.Skip(first + 1).TakeWhile(item => Opcode(item) != FunctionInfo);
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static IEnumerable<object> Items(object item, string name) => ((IEnumerable)Property(item, name)).Cast<object>();
}
