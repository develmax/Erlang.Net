using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamFloatCallArithmeticChecks
{
    internal const string TestName = "compiler/beam/converts-call-results-for-floating-multiplication";
    private const string Fixture = "float_call_arithmetic_probe.erl";
    private const string WrongArithmetic = "A floating operand did not select the float register path or changed an integer control.";
    internal const string Source = """
        -module(float_call_arithmetic_probe).
        -export([scale/1, reverse/1, integer/1]).
        scale(T) -> round(erlang:element(1, T) * 1.5).
        reverse(T) -> round(1.5 * erlang:element(1, T)).
        integer(T) -> round(erlang:element(1, T) * 2).
        """;

    internal static Task Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, Fixture));
        object[] instructions = Items(code, "Instructions").ToArray();
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        if (!Body("scale").Any(instruction => Opcode(instruction) == FloatConvert)
            || !Body("reverse").Any(instruction => Opcode(instruction) == FloatConvert)
            || Body("integer").Any(instruction => Opcode(instruction) == FloatConvert))
        {
            throw new InvalidOperationException(WrongArithmetic);
        }

        return Task.CompletedTask;

        IEnumerable<object> Body(string name)
        {
            int start = Array.FindIndex(instructions, item => Opcode(item) == FunctionInfo
                && atoms[Value(Items(item, "Operands").ElementAt(1))] == name);

            return instructions.Skip(start + 1).TakeWhile(item => Opcode(item) != FunctionInfo);
        }
    }

    private static byte Opcode(object item) => (byte)Property(Property(item, "Definition"), "Opcode");

    private static int Value(object item) => (int)(BigInteger)Property(item, "Value");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
