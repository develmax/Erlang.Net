using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamCommonTupleEntryPrefixChecks
{
    internal const string TestName = "compiler/beam/checks-a-common-leading-atom-before-tuple-entry-selection";
    internal const string Fixture = "beam_common_tuple_entry_prefix_probe.erl";
    private const string IncorrectPrefix = "Tuple entry selection did not preserve its leading atom partition.";

    internal const string Source = """
        -module(beam_common_tuple_entry_prefix_probe).
        -export([route/3, mixed/3]).

        route(accept, {first, Value}, Context) -> {small,Value,Context};
        route(accept, {second, Key, Value}, Context) -> {large,Key,Value,Context};
        route(Kind, Event, Context) -> {fallback,Kind,Event,Context}.

        mixed(accept, {first, Value}, Context) -> {small,Value,Context};
        mixed(reject, {second, Key, Value}, Context) -> {large,Key,Value,Context};
        mixed(Kind, Event, Context) -> {fallback,Kind,Event,Context}.
        """;

    internal static Task Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, Fixture));
        object[] instructions = ((IEnumerable)Property(code, "Instructions")).Cast<object>().ToArray();
        byte[] opcodes = instructions.Select(instruction => (byte)Property(Property(instruction, "Definition"), "Opcode")).ToArray();
        int[] entries = Enumerable.Range(0, opcodes.Length).Where(index => opcodes[index] == FunctionInfo).ToArray();
        int exact = Array.IndexOf(
            opcodes,
            ExactEqualTest,
            entries[0],
            entries[1] - entries[0]
        );
        int tuple = Array.IndexOf(
            opcodes,
            IsTuple,
            entries[0],
            entries[1] - entries[0]
        );
        if (exact < entries[0] || tuple != exact + 1)
        {
            throw new InvalidOperationException(IncorrectPrefix);
        }

        object[] prefix = ((IEnumerable)Property(instructions[exact], "Operands")).Cast<object>().ToArray();
        object[] arity = ((IEnumerable)Property(instructions[tuple], "Operands")).Cast<object>().ToArray();
        if ((BigInteger)Property(prefix[1], "Value") != BigInteger.Zero
            || (BigInteger)Property(arity[1], "Value") != BigInteger.One
            || (BigInteger)Property(prefix[0], "Value") != (BigInteger)Property(arity[0], "Value"))
        {
            throw new InvalidOperationException(IncorrectPrefix);
        }

        return Task.CompletedTask;
    }
}
