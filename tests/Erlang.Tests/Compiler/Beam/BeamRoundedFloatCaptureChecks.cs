using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamRoundedFloatCaptureChecks
{
    internal const string TestName = "compiler/beam/keeps-typed-float-rounding-operands-and-captures";
    private const int FloatType = 1 << 3;
    private const string Fixture = "rounded_capture_probe.erl";
    private const string WrongCapture = "A floating rounding operand or its saved result lost the proven register type.";
    internal const string Source = """
        -module(rounded_capture_probe).
        -compile({no_auto_import,[round/1]}).
        -export([boxed/1, direct/1, open/2, shadow/1, produce/1, next/0, mark/2]).
        boxed(N) -> ?MODULE:mark(erlang:round(?MODULE:produce(N) * 1.5), ?MODULE:next()).
        direct(N) -> erlang:floor(?MODULE:produce(N) * 1.5).
        open(N, Scale) -> erlang:round(?MODULE:produce(N) * Scale).
        shadow(N) -> ?MODULE:mark(round(?MODULE:produce(N) * 1.5), ?MODULE:next()).
        round(N) -> {shadow, N}.
        produce(N) -> erlang:put(seen, N), N.
        next() -> next.
        mark(A, B) -> {A, B}.
        """;

    internal static Task Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, Fixture));
        object[] instructions = Items(code, "Instructions").ToArray();
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] types = Items(Property(code, "Types"), "Entries").ToArray();
        object[] captured = Unary(Body("boxed")).Single();
        object[] direct = Unary(Body("direct")).Single();
        object[] open = Unary(Body("open")).Single();
        if (!IsTypedFloat(captured[3], types) || Kind(captured[4]) != "y" || !IsTypedFloat(direct[3], types) || Kind(direct[4]) != "x"
            || IsTypedFloat(open[3], types) || Unary(Body("shadow")).Any())
        {
            throw new InvalidOperationException(WrongCapture);
        }

        return Task.CompletedTask;

        IEnumerable<object> Body(string name)
        {
            int start = Array.FindIndex(instructions, item => Opcode(item) == FunctionInfo
                && atoms[Value(Items(item, "Operands").ElementAt(1))] == name);

            return instructions.Skip(start + 1).TakeWhile(item => Opcode(item) != FunctionInfo);
        }
    }

    private static IEnumerable<object[]> Unary(IEnumerable<object> body) =>
        body.Where(item => Opcode(item) == GcBifOne).Select(item => Items(item, "Operands").ToArray());

    private static bool IsTypedFloat(object operand, object[] types) => Kind(operand) == "x"
        && Property(operand, "TypeIndex") is int type && (int)Property(types[type], "TypeUnion") == FloatType;

    private static string Kind(object item) => (string)Property(item, "Kind");

    private static byte Opcode(object item) => (byte)Property(Property(item, "Definition"), "Opcode");

    private static int Value(object item) => (int)(BigInteger)Property(item, "Value");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
