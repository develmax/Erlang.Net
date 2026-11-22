using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamNamedCallCaptureOrderChecks
{
    internal const string TestName = "compiler/beam/orders-named-call-captures-in-lifted-callback";
    private const string WrongCapture = "The lifted callback must receive the named captures in Core free-variable order.";
    private const string WrongCopies = "A live callback capture must be copied before its X register is reused.";
    private const string Source = """
        -module(beam_named_call_capture_order).
        -export([run/2]).
        run(X, Y) ->
            Zed = capture_sink:make(one, two, three, four, []),
            Alpha = capture_sink:make(X, [{size, {4, 5}}]),
            capture_sink:touch(Alpha),
            capture_sink:use(Alpha, old, Zed),
            capture_sink:touch(Alpha, next),
            capture_sink:touch(Alpha, next, 1),
            P = capture_sink:measure(Alpha, next, "wide"),
            capture_sink:touch(Alpha, next, P),
            capture_sink:touch(Alpha, true),
            Items = [{1,2},{3,4}],
            F = fun({A, B}) ->
                capture_sink:use(Alpha, A, Zed),
                capture_sink:save(Alpha, A, B)
            end,
            [F(Item) || Item <- Items],
            capture_sink:finish(Alpha, capture_sink:stop()), capture_sink:done(Y, Alpha), Alpha.
        """;

    internal static Task Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, "beam_named_call_capture_order.erl"));
        object callback = Items(code, "Functions").Single(function =>
            (string)Property(function, "Name") == "-run/2-fun-0-");
        int first = (int)Property(callback, "FirstInstructionIndex");
        int last = (int)Property(callback, "EndInstructionIndex");
        object saved = Items(code, "Instructions")[first..last].First(instruction =>
            (byte)Property(Property(instruction, "Definition"), "Opcode") == Move);
        object[] operands = Items(saved, "Operands");
        if ((string)Property(operands[0], "Kind") != BeamOperandParameters.X
            || (BigInteger)Property(operands[0], "Value") != 1
            || (string)Property(operands[1], "Kind") != BeamOperandParameters.Y)
        {
            throw new InvalidOperationException(WrongCapture);
        }

        object[] instructions = Items(code, "Instructions")[first..last];
        int call = Array.FindIndex(instructions, instruction => Opcode(instruction) == ExternalCall);
        int tail = Array.FindIndex(instructions, instruction => Opcode(instruction) == TailExternalCallWithFrame);
        if (call < 0 || tail < 0
            || MovesBefore(instructions, call, 2) is not
                [("x", 1, "x", 0), ("y", 1, "x", 1)]
            || MovesBefore(instructions, tail, 3) is not
                [("y", 0, "x", 2), ("y", 1, "x", 1), ("y", 2, "x", 0)])
        {
            throw new InvalidOperationException(WrongCopies);
        }

        return Task.CompletedTask;
    }

    private static (string Source, int From, string Target, int To)[] MovesBefore(
        object[] instructions,
        int call,
        int count
    )
    {
        while (call != 0 && Opcode(instructions[call - 1]) == Line)
        {
            call--;
        }

        return instructions[Math.Max(0, call - count)..call].Where(instruction => Opcode(instruction) == Move)
            .Select(instruction =>
            {
                object[] operands = Items(instruction, "Operands");

                return (
                    (string)Property(operands[0], "Kind"),
                    (int)(BigInteger)Property(operands[0], "Value"),
                    (string)Property(operands[1], "Kind"),
                    (int)(BigInteger)Property(operands[1], "Value")
                );
            }).ToArray();
    }

    private static byte Opcode(object instruction) =>
        (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static object[] Items(object owner, string property) =>
        ((IEnumerable)Property(owner, property)).Cast<object>().ToArray();
}
