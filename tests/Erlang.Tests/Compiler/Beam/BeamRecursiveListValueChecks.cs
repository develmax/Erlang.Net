using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamRecursiveListValueChecks
{
    internal const string TestName = "compiler/beam/lifts-list-generators-with-immediate-and-captured-values";
    private const string HelperIdentityFailure = "Recursive list mapper identities or captured arities differ.";

    private const string Source = """
        -module(beam_recursive_list_values_probe).
        -export([identity/1, constant/1, capture/2, tagged/1, live_case/2, improper/1, alias/2]).
        identity(Items) -> [Item || Item <- Items].
        constant(Items) -> [constant || _Item <- Items].
        capture(Items, Value) -> [Value || _Item <- Items].
        tagged({ok, Items}) -> {ok, [Item || Item <- Items]};
        tagged(_) -> unknown.
        live_case(Input, Value) ->
            case Input of
                {ok, Items} -> {ok, [Value || _Item <- Items]};
                _ -> unknown
            end.
        improper(Items) -> {generated, [Item || Item <- Items]}.
        alias(Items, Input) ->
            Outer = {cont, Input},
            {Outer, [Outer || _Item <- Items]}.
        """;

    internal static Task Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, "beam_recursive_list_values_probe.erl"));
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        var helpers = new List<(string Name, int Arity)>();
        object[] instructions = Items(code, "Instructions").ToArray();
        foreach (object instruction in instructions)
        {
            if ((byte)Property(Property(instruction, "Definition"), "Opcode") != FunctionInfo)
            {
                continue;
            }

            object[] operands = Items(instruction, "Operands").ToArray();
            string name = atoms[(int)(BigInteger)Property(operands[1], "Value")];
            if (name.Contains("-lc$^", StringComparison.Ordinal))
            {
                helpers.Add((name, (int)(BigInteger)Property(operands[2], "Value")));
                if (name.StartsWith("-constant/", StringComparison.Ordinal)
                    || name.StartsWith("-capture/", StringComparison.Ordinal))
                {
                    int bodyStart = Array.IndexOf(instructions, instruction) + 1;
                    byte[] opcodes = instructions.Skip(bodyStart).Select(Opcode)
                        .TakeWhile(opcode => opcode != FunctionInfo).ToArray();
                    if (!opcodes.Contains(GetTail) || opcodes.Contains(GetList))
                    {
                        throw new InvalidOperationException(HelperIdentityFailure);
                    }
                }
            }
        }

        (string Name, int Arity)[] expected =
        [
            ("-alias/2-lc$^0/1-0-", 2),
            ("-improper/1-lc$^0/1-0-", 1),
            ("-live_case/2-lc$^0/1-0-", 2),
            ("-tagged/1-lc$^0/1-0-", 1),
            ("-capture/2-lc$^0/1-0-", 2),
            ("-constant/1-lc$^0/1-0-", 1),
            ("-identity/1-lc$^0/1-0-", 1)
        ];
        if (!helpers.SequenceEqual(expected))
        {
            throw new InvalidOperationException(HelperIdentityFailure);
        }

        return Task.CompletedTask;
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static IEnumerable<object> Items(object item, string name) => ((IEnumerable)Property(item, name)).Cast<object>();
}
