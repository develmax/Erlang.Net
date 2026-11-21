using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamRetainedScalarClosureChecks
{
    internal const string TestName = "compiler/beam/retains-scalar-results-concurrently-with-a-closure-capture";
    private const string Fixture = "beam_retained_scalar_closure.erl";
    private const string WrongFrame = "The immediately preceding return value did not remain in its capture register.";
    private const string WrongOrder = "The closure operands were not ordered by the Core free variables.";
    private const string WrongCapture = "An unrelated source binding entered the retained-result capture plan.";

    private const string Source = """
        -module(beam_retained_scalar_closure).
        -export([run/1]).

        run(Seed) ->
            ZOwner = closure_peer:create(Seed),
            closure_peer:prepare(ZOwner),
            ACapture = closure_peer:compute(Seed),
            Work = fun(Item) -> closure_peer:observe(ZOwner,ACapture,Item) end,
            lists:foreach(Work, closure_peer:inputs(Seed)),
            closure_peer:finish(ZOwner),
            ZOwner.
        """;

    internal static Task Run()
    {
        object[] body = Read(Source);
        int make = Array.FindIndex(body, instruction => Opcode(instruction) == MakeFun);
        if (make < 1 || Opcode(body[make - 1]) != TestHeap || Value(Operands(body[make - 1])[1]) != 1)
        {
            throw new InvalidOperationException(WrongFrame);
        }

        var captures = Operands(body[make]);
        if (Kind(captures[1]) != "y" || Value(captures[2]) != 2
            || Kind(captures[3]) != "x" || Value(captures[3]) != 0 || Kind(captures[4]) != "y")
        {
            throw new InvalidOperationException(WrongOrder);
        }

        string control = Source.Replace("observe(ZOwner,ACapture,Item)", "observe(Seed,ACapture,Item)", StringComparison.Ordinal);
        var unrelated = Operands(Read(control).First(instruction => Opcode(instruction) == MakeFun));
        if (unrelated.Skip(3).Any(operand => Kind(operand) == "x"))
        {
            throw new InvalidOperationException(WrongCapture);
        }

        return Task.CompletedTask;
    }

    private static object[] Read(string source)
    {
        object code = Code(CodeGeneration.CompileBeamModule(source, Fixture));

        return ((IEnumerable)Property(code, "Instructions")).Cast<object>().ToArray();
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static string Kind(object operand) => (string)Property(operand, "Kind");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Operands(object instruction) =>
        ((IEnumerable)Property(instruction, "Operands")).Cast<object>().ToArray();
}
