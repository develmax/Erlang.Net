using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamNonnumericComparisonChecks
{
    internal const string TestName = "compiler/beam/uses-exact-comparisons-for-empty-list-and-bitstring-literals";
    private const string Fixture = "beam_nonnumeric_comparison_probe.erl";
    private const string LooseNonnumeric = "Comparisons against empty lists and bitstring literals must use exact tests.";
    private const string ExactNumericList = "Loose list equality must still compare nested numbers numerically.";

    private const string Source = """
        -module(beam_nonnumeric_comparison_probe).
        -export([nil_left/1,nil_right/1,binary_eq/1,binary_ne/1,number_list/1,forward/3]).

        nil_left(V) when [] /= V -> occupied;
        nil_left(_) -> zero.

        nil_right(V) when V /= [] -> occupied;
        nil_right(_) -> zero.

        binary_eq(V) when V == <<1,2>> -> match;
        binary_eq(_) -> no_match.

        binary_ne(V) when V /= <<1,2>> -> no_match;
        binary_ne(_) -> match.

        forward(_, _, Data) when Data == []; Data == <<>> -> [];
        forward(Module, Mode, Data) -> Module:forward(Mode, Data).

        number_list(V) when V == [0.0] -> numeric_match;
        number_list(_) -> no_match.
        """;

    internal static Task Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, Fixture));
        object[] instructions = Items(code, "Instructions").ToArray();
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        if (!Uses("nil_left", IsNil)
            || !Uses("nil_right", IsNil)
            || !Uses("binary_eq", ExactEqualTest)
            || !Uses("binary_ne", ExactEqualTest)
            || !Uses("forward", ExactNotEqualTest))
        {
            throw new InvalidOperationException(LooseNonnumeric);
        }

        if (!Uses("number_list", NumericEqualTest))
        {
            throw new InvalidOperationException(ExactNumericList);
        }

        return Task.CompletedTask;

        bool Uses(string name, byte opcode)
        {
            int start = Array.FindIndex(
                instructions,
                i => Opcode(i) == FunctionInfo
                && atoms[(int)(BigInteger)Property(Items(i, "Operands").ElementAt(1), "Value")] == name
            );

            return instructions.Skip(start + 1).TakeWhile(i => Opcode(i) != FunctionInfo).Any(i => Opcode(i) == opcode);
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
