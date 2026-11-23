using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamCallResultLiteralMoveChecks
{
    internal const string TestName = "compiler/beam/orders-literal-and-preceding-result-arguments";
    private const string Fixture = "outgoing_call_result_literals.erl";
    private const string MoveOrderMismatch = "The literal arguments did not preserve the preceding call result and pinned move order.";

    internal static Task Compare()
    {
        const string source = """
            -module(outgoing_call_result_literals).
            -export([nested/0, middle/1, last/1, retained/2, produce/1, consume/3]).
            middle(Input) ->
                Result = ?MODULE:produce(Input),
                Value = ?MODULE:consume(tag, Result, [tail]),
                {done, Value}.
            last(Input) ->
                Result = ?MODULE:produce(Input),
                Value = ?MODULE:consume(tag, 12, Result),
                {done, Value}.
            retained(Input, Other) ->
                Result = ?MODULE:produce(Input),
                Value = ?MODULE:consume(Other, Result, tail),
                {done, Value}.
            nested() ->
                Flags = {flag, 0, timer:hours(1)},
                Workers = [consume(first, timer:seconds(2), [worker]),
                           consume(second, timer:seconds(3), [worker])],
                {ok, {Flags, Workers}}.
            produce(Value) -> Value.
            consume(First, Second, Third) -> {First, Second, Third}.
            """;

        byte[] image = CodeGeneration.CompileBeamModule(source, Fixture);
        object code = Code(image);
        object[] instructions = Items(code, "Instructions");
        object[] functions = Items(code, "Functions");

        CompareDestinations("middle", [1, 2, 0]);
        CompareDestinations("last", [1, 2, 0]);
        CompareDestinations("nested", [1, 2, 0], expectedWindows: 2);
        _ = BeamModule.Prepare(image);

        return Task.CompletedTask;

        void CompareDestinations(string name, int[] expected, int expectedWindows = 1)
        {
            object function = functions.Single(item => (string)Property(item, "Name") == name);
            int start = (int)Property(function, "FirstInstructionIndex");
            int end = (int)Property(function, "EndInstructionIndex");
            object[] body = instructions[start..end];
            var actual = new List<int[]>();
            for (int index = 0; index < body.Length; index++)
            {
                if (Opcode(body[index]) != BeamEmissionFormat.ExternalCall)
                {
                    continue;
                }

                int[] destinations = body.Skip(index + 1).TakeWhile(instruction => Opcode(instruction) == BeamEmissionFormat.Move)
                    .Select(instruction => (int)(BigInteger)Property(Items(instruction, "Operands")[1], "Value")).ToArray();
                if (destinations.Length != 0)
                {
                    actual.Add(destinations);
                }
            }

            if (actual.Count != expectedWindows || actual.Any(order => !order.SequenceEqual(expected)))
            {
                throw new InvalidOperationException(MoveOrderMismatch);
            }
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static object[] Items(object value, string property) =>
        ((IEnumerable)Property(value, property)).Cast<object>().ToArray();
}
