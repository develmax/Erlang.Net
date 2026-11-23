using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamMixedTailArgumentChainsChecks
{
    internal const string TestName = "compiler/beam/orders-one-register-and-constant-tail-arguments";
    internal const string Fixture = "beam_mixed_tail_argument_chains.erl";
    private const string IncorrectOrder = "The mixed tail arguments did not follow the OTP move chain.";

    internal const string Source = """
        -module(beam_mixed_tail_argument_chains).
        -export([forward/2, first/2, receiver/4]).

        forward(Socket, Data) -> ?MODULE:receiver(Socket, {local, <<>>}, [], Data).
        first(Socket, Data) -> ?MODULE:receiver(Data, {local, <<>>}, [], Socket).
        receiver(A, B, C, D) -> {A, B, C, D}.
        """;

    internal static byte[] CompileFixture() => CodeGeneration.CompileBeamModule(Source, Fixture);

    internal static Task Run()
    {
        object code = Code(CompileFixture());
        object[] instructions = Items(code, "Instructions").ToArray();
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        if (!OutgoingDestinations("forward").SequenceEqual(new[] { 2, 3, 1 }))
        {
            throw new InvalidOperationException(IncorrectOrder);
        }

        return Task.CompletedTask;

        IEnumerable<int> OutgoingDestinations(string name)
        {
            int start = Array.FindIndex(
                instructions,
                item => Opcode(item) == FunctionInfo && atoms[Value(Items(item, "Operands").ElementAt(1))] == name
            );

            return instructions.Skip(start + 1).TakeWhile(item => Opcode(item) != FunctionInfo)
                .Where(item => Opcode(item) == Move).Select(item => Value(Items(item, "Operands").Last()));
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
