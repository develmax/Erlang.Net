using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamEntryLiteralDispatchChecks
{
    internal const string TestName = "compiler/beam/groups-exclusive-entry-literal-tests";
    private const string Fixture = "entry_literal_dispatch_probe.erl";
    private const string WrongDispatch = "Disjoint literal branches lost their shared switch or a guarded failure lost its original path.";
    internal const string Source = """
        -module(entry_literal_dispatch_probe).
        -export([atom_branch/1, integer_branch/1, guarded/2]).
        atom_branch(second) -> fun second/0;
        atom_branch(first) -> fun first/0.
        integer_branch(20) -> fun second/0;
        integer_branch(10) -> fun first/0.
        guarded(first, N) when is_integer(N) -> fun first/0;
        guarded(first, _) -> fun second/0;
        guarded(second, _) -> fun second/0.
        first() -> first.
        second() -> second.
        """;

    internal static Task Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, Fixture));
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions").ToArray();
        object[] atom = Body("atom_branch");
        object[] integer = Body("integer_branch");
        object[] guarded = Body("guarded");
        if (atom.Count(instruction => Opcode(instruction) == SelectValue) != 1
            || integer.Count(instruction => Opcode(instruction) == SelectValue) != 1
            || !guarded.Any(instruction => Opcode(instruction) == IsInteger))
        {
            throw new InvalidOperationException(WrongDispatch);
        }

        return Task.CompletedTask;

        object[] Body(string function)
        {
            int start = Array.FindIndex(instructions, instruction => Opcode(instruction) == FunctionInfo
                && atoms[Value(Operands(instruction)[1])] == function);

            return instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        }
    }

    private static byte Opcode(object item) => (byte)Property(Property(item, "Definition"), "Opcode");

    private static object[] Operands(object item) => Items(item, "Operands").ToArray();

    private static int Value(object item) => (int)(BigInteger)Property(item, "Value");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
