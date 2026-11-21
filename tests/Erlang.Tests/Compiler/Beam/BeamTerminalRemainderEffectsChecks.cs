using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamTerminalRemainderEffectsChecks
{
    internal const string TestName = "compiler/beam/trims-per-branch-frames-after-a-signed-remainder";
    private const string Fixture = "beam_terminal_remainder_effects.erl";
    private const string WrongSelector = "The case selector did not keep the signed remainder bound.";
    private const string WrongTrim = "Case-specific frame ownership was not reflected in the trim instructions.";
    private const string WrongBinding = "A repeated argument binding disappeared from the entry match.";

    private const string Source = """
        -module(beam_terminal_remainder_effects).
        -export([run/3]).

        run(Row, Paint, Canvas) ->
            paint_peer:draw(Canvas, Row, 0, white),
            paint_peer:draw(Canvas, Row, 1, paint_peer:decode(pencil)),
            case Row rem 4 of
                0 -> paint_peer:draw(Canvas, Row, 1, paint_peer:decode(red));
                1 ->
                    paint_peer:draw(Canvas, Row, 1, paint_peer:decode(green)),
                    paint_peer:finish(Canvas, Row, 1);
                2 -> paint_peer:draw(Canvas, Row, 1, paint_peer:decode(blue));
                _ ->
                    paint_peer:draw(Canvas, Row, 1, Paint),
                    paint_peer:finish(Canvas, Row, 1)
            end.
        """;

    internal static Task Run()
    {
        object[] body = Read(Source);
        if (!body.Where(instruction => Opcode(instruction) == SelectValue)
            .Any(instruction => Property(Operands(instruction)[0], "TypeIndex") is not null))
        {
            throw new InvalidOperationException(WrongSelector);
        }

        if (body.Where(instruction => Opcode(instruction) == TrimFrame)
            .Count(instruction => Value(Operands(instruction)[0]) == 1 && Value(Operands(instruction)[1]) == 2) != 4)
        {
            throw new InvalidOperationException(WrongTrim);
        }

        string bound = Source.Replace("run(Row, Paint, Canvas)", "run(Row, Row, Canvas)", StringComparison.Ordinal)
            .Replace("Row, 1, Paint", "Row, 1, Row", StringComparison.Ordinal);
        if (!Read(bound).Any(instruction => Opcode(instruction) == ExactEqualTest))
        {
            throw new InvalidOperationException(WrongBinding);
        }

        return Task.CompletedTask;
    }

    private static object[] Read(string source)
    {
        object code = Code(CodeGeneration.CompileBeamModule(source, Fixture));

        return ((IEnumerable)Property(code, "Instructions")).Cast<object>().ToArray();
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Operands(object instruction) =>
        ((IEnumerable)Property(instruction, "Operands")).Cast<object>().ToArray();
}
