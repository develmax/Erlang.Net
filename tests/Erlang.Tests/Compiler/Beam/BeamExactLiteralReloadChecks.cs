using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamExactLiteralReloadChecks
{
    internal const string TestName = "compiler/beam/forwards-an-exactly-matched-literal-without-reloading-it";
    private const string Fixture = "beam_exact_literal_routes.erl";
    private const string RepeatedMove = "An exactly matched literal was loaded again before the tail call.";
    private const string IntegerOperand = "integer";
    private const string XOperand = "x";
    private const string Source = """
        -module(beam_exact_literal_routes).
        -export([forward/3, other/3, numeric/3, probe/3]).
        forward(A, Address = {tag,_}, 0) ->
            beam_exact_literal_routes:probe(A, Address, 0).
        other(A, Address = {tag,_}, 0) ->
            beam_exact_literal_routes:probe(0, Address, A).
        numeric(A, Address = {tag,_}, Port) when Port == 0 ->
            beam_exact_literal_routes:probe(A, Address, 0).
        probe(A, Address, Port) ->
            {A, Address, Port}.
        """;

    internal static Task Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, Fixture));
        object[] instructions = Items(code, "Instructions").ToArray();
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        int start = Array.FindIndex(
            instructions,
            item => Opcode(item) == FunctionInfo && atoms[Value(Items(item, "Operands").ElementAt(1))] == "forward"
        );
        var body = instructions.Skip(start + 1).TakeWhile(item => Opcode(item) != FunctionInfo).ToArray();
        bool reloaded = body.Any(item => Opcode(item) == Move
            && Items(item, "Operands").ToArray() is [var source, var destination]
            && (string)Property(source, "Kind") == IntegerOperand
            && (string)Property(destination, "Kind") == XOperand
            && Value(source) == 0 && Value(destination) == 2);
        if (reloaded)
        {
            throw new InvalidOperationException(RepeatedMove);
        }

        return Task.CompletedTask;
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string name) =>
        ((IEnumerable)Property(owner, name)).Cast<object>();
}
