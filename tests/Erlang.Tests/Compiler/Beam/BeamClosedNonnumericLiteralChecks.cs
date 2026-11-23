using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamClosedNonnumericLiteralChecks
{
    internal const string TestName = "compiler/beam/uses-exact-tests-for-closed-nonnumeric-literals";
    private const string Fixture = "closed_nonnumeric_literal_probe.erl";
    private const string WrongEquality = "Closed exact tests or their numeric-coercion controls were lost.";
    internal const string Source = """
        -module(closed_nonnumeric_literal_probe).
        -export([tuple/1, atoms/1, empty_map/1, numeric/1, wide/1, numeric_map/1]).
        tuple(V) when V == {tcp_closed,[plain,<<>>]} -> match;
        tuple(_) -> miss.
        atoms(V) when V /= [active,passive] -> other;
        atoms(_) -> same.
        empty_map(V) when V == #{} -> empty;
        empty_map(_) -> other.
        numeric(V) when V == {count,[1.0]} -> same;
        numeric(_) -> other.
        wide(V) when V == {a,b,c,d,e,f,g,h,i,j,k,l,m} -> same;
        wide(_) -> other.
        numeric_map(V) when V == #{item => 0.0} -> same;
        numeric_map(_) -> other.
        """;

    internal static Task Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, Fixture));
        object[] instructions = Items(code, "Instructions").ToArray();
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        if (!Uses("tuple", ExactEqualTest) || !Uses("atoms", ExactEqualTest)
            || !Uses("empty_map", ExactEqualTest) || !Uses("numeric", NumericEqualTest)
            || !Uses("wide", NumericEqualTest) || !Uses("numeric_map", NumericEqualTest))
        {
            throw new InvalidOperationException(WrongEquality);
        }

        return Task.CompletedTask;

        bool Uses(string name, byte opcode)
        {
            int start = Array.FindIndex(instructions, item => Opcode(item) == FunctionInfo
                && atoms[Value(Items(item, "Operands").ElementAt(1))] == name);

            return instructions.Skip(start + 1).TakeWhile(item => Opcode(item) != FunctionInfo)
                .Any(item => Opcode(item) == opcode);
        }
    }

    private static byte Opcode(object item) => (byte)Property(Property(item, "Definition"), "Opcode");

    private static int Value(object item) => (int)(BigInteger)Property(item, "Value");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
