using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamPartiallyOwnedTaggedStateChecks
{
    internal const string TestName = "compiler/beam/keeps-repeated-tagged-state-retries-in-source-order";
    private const string Fixture = "beam_partially_owned_tagged_state_probe.erl";
    private const string WrongLayout = "Distinct states may leave a bucket only when its repeated state still reaches both original rows.";

    private const string Source = """
        -module(beam_partially_owned_tagged_state_probe).
        -export([dispatch/4]).
        dispatch(internal, {failure, _, _}, Route={retry, client}, {nested, []}) -> {first_retry, Route};
        dispatch(internal, {failure, Value, _}, Route={retry, client}, Data) -> {second_retry, Route, Value, Data};
        dispatch(internal, {failure, _, _}, {state_b, client}, Data) -> {owned_b, Data};
        dispatch(internal, {failure, _, _}, {state_c, client}, Data) -> {owned_c, Data};
        dispatch(internal, {peer, Value, _}, {state_b, client}, Data) -> {peer_b, Value, Data};
        dispatch(internal, {peer, Value, _}, {state_c, client}, Data) -> {peer_c, Value, Data};
        dispatch(internal, {small}, {state_b, client}, Data) -> {small, Data};
        dispatch(Type, Event, State, Data) -> {fallback, Type, Event, State, Data}.
        """;

    internal static Task Run()
    {
        if (!HasSplit(Source, requireRetries: true) || HasSplit(Source.Replace(
            "{state_c, client}, Data) -> {owned_c",
            "{state_c, server}, Data) -> {owned_c",
            StringComparison.Ordinal
        )) || HasSplit(Source.Replace(
            "{state_c, client}, Data) -> {owned_c",
            "{state_c, client}, Data) when is_atom(Data) -> {owned_c",
            StringComparison.Ordinal
        )))
        {
            throw new InvalidOperationException(WrongLayout);
        }

        return Task.CompletedTask;
    }

    private static bool HasSplit(string source, bool requireRetries = false)
    {
        object code = Code(CodeGeneration.CompileBeamModule(source, Fixture));
        object[] instructions = Items(code, "Instructions").ToArray();
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        foreach (object instruction in instructions.Where(instruction => Opcode(instruction) == SelectValue))
        {
            object[] operands = Operands(instruction);
            if (operands.Length != 9 || Value(operands[0]) != 4)
            {
                continue;
            }

            var keys = operands.Skip(3).Where((_, index) => index % 2 == 0)
                .Where(operand => (string)Property(operand, "Kind") == "atom")
                .Select(operand => atoms[Value(operand)]);
            int[] targets = operands.Skip(4).Where((_, index) => index % 2 == 0).Select(Value).ToArray();
            if (!keys.SequenceEqual(["retry", "state_b", "state_c"]) || targets.Distinct().Count() != 3)
            {
                continue;
            }

            if (!requireRetries)
            {
                return true;
            }

            int retry = Array.FindIndex(instructions, candidate => Opcode(candidate) == Label && Value(Operands(candidate)[0]) == targets[0]);
            if (retry < 0)
            {
                return false;
            }

            var tags = instructions.Skip(retry + 1).TakeWhile(candidate => Opcode(candidate) != FunctionInfo)
                .Where(candidate => Opcode(candidate) == PutTuple)
                .SelectMany(Operands)
                .Where(operand => (string)Property(operand, "Kind") == "atom")
                .Select(operand => atoms[Value(operand)])
                .Where(atom => atom is "first_retry" or "second_retry");

            return tags.SequenceEqual(["first_retry", "second_retry"]);
        }

        return false;
    }

    private static object[] Operands(object instruction) => Items(instruction, "Operands").ToArray();

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
