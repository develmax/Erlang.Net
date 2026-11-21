using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamImmutableClosureOperandChecks
{
    internal const string TestName = "compiler/beam/replaces-only-proven-constant-closure-captures";
    private const string Fixture = "closure_constant_operands_probe.erl";
    private const string WrongCapture = "The lifted closure did not retain exactly its live lexical operands.";

    private const string Source = """
        -module(closure_constant_operands_probe).
        -export([constant/2, dynamic/2]).

        constant(List, Suffix) ->
            Flags = #{flag => 8192},
            F = fun({keep, Value}) -> {Value, Flags, Suffix} end,
            [F(Item) || Item <- List].

        dynamic(List, Suffix) ->
            Flags = erlang:get(flags),
            F = fun({keep, Value}) -> {Value, Flags, Suffix} end,
            [F(Item) || Item <- List].
        """;

    internal static Task Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, Fixture));
        string[] atoms = ((IEnumerable)Property(Property(code, "Structure"), "Atoms")).Cast<string>().ToArray();
        var functions = new Dictionary<string, int>();
        foreach (var instruction in ((IEnumerable)Property(code, "Instructions")).Cast<object>())
        {
            if ((byte)Property(Property(instruction, "Definition"), "Opcode") != FunctionInfo)
            {
                continue;
            }

            object[] operands = ((IEnumerable)Property(instruction, "Operands")).Cast<object>().ToArray();
            int index = (int)(BigInteger)Property(operands[1], "Value");
            int arity = (int)(BigInteger)Property(operands[2], "Value");
            if (atoms[index] is "-constant/2-fun-0-" or "-dynamic/2-fun-0-")
            {
                functions.Add(atoms[index], arity);
            }
        }

        if (functions.GetValueOrDefault("-constant/2-fun-0-") != 2
            || functions.GetValueOrDefault("-dynamic/2-fun-0-") != 3)
        {
            throw new InvalidOperationException(WrongCapture);
        }

        return Task.CompletedTask;
    }
}
