using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamLexicalFunLocationChecks
{
    internal const string TestName = "compiler/beam/records-lifted-function-at-lexical-fun-location";
    private const string Fixture = "beam_lexical_fun_location_probe.erl";
    private const string WrongLocation = "Lifted function metadata must locate the fun token itself.";

    private const string Source = """
        -module(beam_lexical_fun_location_probe).
        -export([split/0, same/0]).
        split() ->
            fun
                (first) -> ok;
                (_) -> other
            end.
        same() ->
            fun (Value) -> {ok,Value} end.
        """;

    internal static Task Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, Fixture));
        object[] instructions = Items(code, "Instructions").ToArray();
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        var locations = (IReadOnlyList<Term>)Property(code, "SourceLocations");
        const int SplitDeclaration = 4;
        const int SameLineDeclaration = 9;

        Check("-split/0-fun-0-", SplitDeclaration);
        Check("-same/0-fun-0-", SameLineDeclaration);

        return Task.CompletedTask;

        void Check(string lambda, int expected)
        {
            int info = Array.FindIndex(
                instructions,
                instruction =>
                Opcode(instruction) == FunctionInfo && Items(instruction, "Operands").ToArray() is [_, var name, _]
                    && atoms[ReadValue(name)] == lambda
            );
            if (info < 1 || Opcode(instructions[info - 1]) != Line)
            {
                throw new InvalidOperationException(WrongLocation);
            }

            int index = ReadValue(Items(instructions[info - 1], "Operands").Single());
            if (locations[index] is not Cons
                {
                    Tail: Cons { Head: TupleTerm { Items: [Atom { Name: "line" }, Integer { Value: var line }] } }
                } || line != expected)
            {
                throw new InvalidOperationException(WrongLocation);
            }
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int ReadValue(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
