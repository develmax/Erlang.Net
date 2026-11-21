using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamDeferredTupleListArgumentChecks
{
    internal const string TestName = "compiler/beam/plans-deferred-tuple-list-arguments";
    private const string Construction = "Keep the allocating result in a register and reserve one heap window for the list.";
    private const string Evaluation = "Preserve the operand order and keep shadowed BIFs on the ordinary call path.";

    private const string Source = """
        -module(beam_deferred_tuple_list_argument).
        -compile({no_auto_import, [make_ref/0, self/0]}).
        -export([ordered/1, leading_field/1, reads_first/1, shadowed/1, fallible/1]).

        ordered(Value) ->
            {seen, fixture_sink:take([{ref, erlang:make_ref()},
                                      {field, Value},
                                      {fixed, false},
                                      {reader, erlang:self()}])}.

        leading_field(Value) ->
            {seen, fixture_sink:take([{field, Value},
                                      {ref, erlang:make_ref()},
                                      {fixed, false},
                                      {mode, read_only},
                                      {reader, erlang:node()}])}.

        reads_first(Value) ->
            {seen, fixture_sink:take([{reader, erlang:self()},
                                      {ref, erlang:make_ref()},
                                      {field, Value}])}.

        shadowed(Value) ->
            {seen, fixture_sink:take([{ref, make_ref()},
                                      {field, Value},
                                      {reader, self()}])}.

        fallible(Value) ->
            {seen, fixture_sink:take([{mark, fixture_sink:mark(before)},
                                      {ref, erlang:make_ref()},
                                      {data, fixture_sink:maybe_fail(Value)},
                                      {reader, erlang:self()}])}.

        make_ref() -> fixture_sink:mark(local_ref).
        self() -> fixture_sink:mark(local_self).
        """;

    internal static void Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, "beam_deferred_tuple_list_argument.erl"));
        object[] instructions = Values(code, "Instructions");
        object[] functions = Values(code, "Functions");
        Check("ordered", 17);
        Check("leading_field", 19);

        object[] reversed = Body("reads_first");
        int intrinsic = Array.FindIndex(reversed, item => Opcode(item) == BifZero);
        int allocator = Array.FindIndex(reversed, IsZeroArityCall);
        if (intrinsic < 0 || allocator <= intrinsic
            || Body("shadowed").Count(item => Opcode(item) == LocalCall) != 2)
        {
            throw new InvalidOperationException(Evaluation);
        }

        void Check(string name, int heapWords)
        {
            object[] body = Body(name);
            int start = Array.FindIndex(body, IsZeroArityCall);
            int end = Array.FindLastIndex(body, item => Opcode(item) == ExternalCall);
            if (start < 0 || end <= start + 2 || Opcode(body[start + 1]) != TestHeap
                || ReadOperand(body[start + 1], 0) != heapWords || ReadOperand(body[start + 1], 1) != 1
                || Opcode(body[start + 2]) != BifZero || ReadOperand(body[start + 2], 1) != 1
                || body[(start + 2)..end].Count(item => Opcode(item) == TestHeap) != 0
                || body[(start + 2)..end].Any(item => Opcode(item) == Move))
            {
                throw new InvalidOperationException(Construction);
            }
        }

        object[] Body(string name)
        {
            object function = functions.Single(candidate => (string)Property(candidate, "Name") == name);
            int start = (int)Property(function, "FirstInstructionIndex");
            int end = (int)Property(function, "EndInstructionIndex");

            return instructions[start..end];
        }
    }

    private static bool IsZeroArityCall(object instruction) =>
        Opcode(instruction) == ExternalCall && ReadOperand(instruction, 0) == BigInteger.Zero;

    private static BigInteger ReadOperand(object instruction, int index) =>
        (BigInteger)Property(Values(instruction, "Operands")[index], "Value");

    private static object[] Values(object owner, string property) =>
        ((IEnumerable)Property(owner, property)).Cast<object>().ToArray();

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");
}
