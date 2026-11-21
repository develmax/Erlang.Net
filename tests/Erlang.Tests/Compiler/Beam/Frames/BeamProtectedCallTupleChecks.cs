using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamProtectedCallTupleChecks
{
    internal const string TestName = "compiler/beam/keeps-nonfailing-tuple-construction-outside-protected-calls";
    private const string Fixture = "beam_protected_call_tuple.erl";
    private const string WrongSuccess = "The protected success path must release the try tag before building the result.";
    private const string WrongArgument = "The precomputed call argument must be built before entering catch.";
    private const string WrongHandler = "The exception class, reason and original stack must remain available for reraising.";
    private const string WrongArithmetic = "The fallible arithmetic must run before releasing its exception tag.";
    private const string MissingFunction = "A protected tuple fixture function is missing.";

    internal const string Source = """
        -module(beam_protected_call_tuple).
        -export([look/2,bump/1,capture/2,adjust/2]).

        look(Key, Field) ->
            try begin {present, ets:lookup_element(beam_probe_table, Key, Field)} end
            catch error:badarg -> {missing, absent}
            end.

        bump(Key) ->
            Counter = {Key, count},
            catch ets:update_counter(beam_probe_table, Counter, 1).

        capture(Key, Field) ->
            try {present, ets:lookup_element(beam_probe_table, Key, Field)}
            catch error:badarg -> {missing, Key}
            end.

        adjust(Key, Field) ->
            try {present, ets:lookup_element(beam_probe_table, Key, Field) + 1}
            catch error:badarith -> bad_type
            end.
        """;

    internal static Task Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, Fixture));
        object[] instructions = Items(code, "Instructions");
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();

        var lookup = Function("look", 2);
        int allocation = Array.FindIndex(lookup, instruction => Opcode(instruction) == Allocate);
        int release = Array.FindIndex(lookup, instruction => Opcode(instruction) == TryEnd);
        int result = Array.FindIndex(lookup, instruction => Opcode(instruction) == PutTuple);
        if (allocation < 0 || Value(Operands(lookup[allocation])[0]) != 1 || release < 0 || result <= release)
        {
            throw new InvalidOperationException(WrongSuccess);
        }

        object[] raised = Operands(lookup.Single(instruction => Opcode(instruction) == Raise));
        if (!raised.Select(Value).SequenceEqual([2, 1]))
        {
            throw new InvalidOperationException(WrongHandler);
        }

        var counter = Function("bump", 1);
        int catchEnter = Array.FindIndex(counter, instruction => Opcode(instruction) == CatchEnter);
        int argument = Array.FindIndex(counter, instruction => Opcode(instruction) == PutTuple);
        if (argument < 0 || catchEnter <= argument || !counter.Any(instruction => Opcode(instruction) == AllocateHeap
            && Value(Operands(instruction)[0]) == 1))
        {
            throw new InvalidOperationException(WrongArgument);
        }

        var adjustment = Function("adjust", 2);
        int arithmetic = Array.FindIndex(adjustment, instruction => Opcode(instruction) == GcBifTwo);
        int adjustmentRelease = Array.FindIndex(adjustment, instruction => Opcode(instruction) == TryEnd);
        if (arithmetic < 0 || arithmetic >= adjustmentRelease)
        {
            throw new InvalidOperationException(WrongArithmetic);
        }

        return Task.CompletedTask;

        object[] Function(string name, int arity)
        {
            int index = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                && atoms[Value(Operands(instruction)[1])] == name && Value(Operands(instruction)[2]) == arity
            );
            if (index < 0)
            {
                throw new InvalidOperationException(MissingFunction);
            }

            return instructions.Skip(index + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        }
    }

    private static object[] Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();

    private static object[] Operands(object instruction) => Items(instruction, "Operands");

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");
}
