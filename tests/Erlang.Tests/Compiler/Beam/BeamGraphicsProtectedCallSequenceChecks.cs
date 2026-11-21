using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamGraphicsProtectedCallSequenceChecks
{
    internal const string TestName = "compiler/beam/retains-protected-call-sequence-frames-and-schedules-float-results";
    private const string Fixture = "beam_graphics_protected_calls.erl";
    private const string WrongEntry = "The callback retained its state before validating the nested event.";
    private const string WrongTry = "The protected calls did not retain their live exception frame.";
    private const string WrongFloat = "The floating continuation did not box the product before moving the retained argument.";
    private const string WrongBinding = "The callback lost a repeated field match.";
    private const string Missing = "A compiled fixture function is missing.";

    private const string Source = """
        -module(beam_graphics_protected_calls).
        -export([paint/3,protected/4,scale/3]).
        -record(envelope,{target,event}).
        -record(refresh,{mode}).
        -record(render,{target,pen,brush,font}).

        paint(#envelope{event=#refresh{}},_,#render{target=Win,pen=Pen,brush=Brush,font=Font}) ->
            Dc = wxPaintDC:new(Win),
            protected(Dc,Pen,Brush,Font),
            wxPaintDC:destroy(Dc),
            ok.

        protected(Win,Pen,Brush,Font) ->
            try
                Canvas = wxGraphicsContext:create(Win),
                wxGraphicsContext:setPen(Canvas,Pen),
                wxGraphicsContext:setBrush(Canvas,Brush),
                wxGraphicsContext:setFont(Canvas,Font,{0,0,7}),
                Path = wxGraphicsContext:createPath(Canvas),
                Render = fun(N) -> scale(N,Canvas,Path) end,
                wx:foreach(Render,lists:seq(0,2)),
                wxGraphicsObject:destroy(Path),
                wxGraphicsObject:destroy(Canvas),
                ok
            catch _:{not_supported,_} ->
                Error = "render unavailable",
                io:format(Error,[])
            end.

        scale(N,Canvas,Path) ->
            wxGraphicsContext:scale(Canvas,1.25,1.5),
            wxGraphicsContext:translate(Canvas,3.5,-2.0*N),
            wxGraphicsContext:drawPath(Canvas,Path).
        """;

    internal static Task Run()
    {
        var get = ReadFunctions(Source);
        object[] paint = get("paint", 3).Body;
        int allocation = Array.FindIndex(paint, instruction => Opcode(instruction) == Allocate);
        if (allocation < 0 || Value(Operands(paint[allocation])[0]) != 1
            || paint.Take(allocation).Count(instruction => Opcode(instruction) == IsTaggedTuple) != 3)
        {
            throw new InvalidOperationException(WrongEntry);
        }

        object[] guarded = get("protected", 4).Body;
        if (guarded.Count(instruction => Opcode(instruction) == TryEnter) != 1
            || guarded.Count(instruction => Opcode(instruction) == TryCase) != 1
            || guarded.Any(instruction => Opcode(instruction) == TrimFrame)
            || !guarded.Any(instruction => Opcode(instruction) == InitYRegisters))
        {
            throw new InvalidOperationException(WrongTry);
        }

        object[] scale = get("scale", 3).Body;
        if (scale.Count(instruction => Opcode(instruction) == FloatMultiply) != 1
            || scale.Count(instruction => Opcode(instruction) == FloatConvert) != 1
            || scale.Count(instruction => Opcode(instruction) == TrimFrame) != 1)
        {
            throw new InvalidOperationException(WrongFloat);
        }

        string repeated = Source.Replace("target=Win,pen=Pen", "target=Win,pen=Win", StringComparison.Ordinal)
            .Replace("protected(Dc,Pen,Brush,Font)", "protected(Dc,Win,Brush,Font)", StringComparison.Ordinal);
        if (!ReadFunctions(repeated)("paint", 3).Body.Where(instruction => Opcode(instruction) == ExactEqualTest)
            .Select(Operands).Any(operands => operands.Skip(1).All(value => (string)Property(value, "Kind") is "x" or "y")))
        {
            throw new InvalidOperationException(WrongBinding);
        }

        return Task.CompletedTask;
    }

    private static Func<string, int, (object[] Body, int Failure)> ReadFunctions(string source)
    {
        object code = Code(CodeGeneration.CompileBeamModule(source, Fixture));
        var instructions = Items(code, "Instructions").ToArray();
        var atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();

        return (name, arity) =>
        {
            int entry = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                && atoms[Value(Operands(instruction)[1])] == name && Value(Operands(instruction)[2]) == arity
            );
            if (entry < 2)
            {
                throw new InvalidOperationException(Missing);
            }

            return (instructions.Skip(entry + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray(),
                Value(Operands(instructions[entry - 2])[0]));
        };
    }

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Operands(object instruction) => Items(instruction, "Operands").ToArray();

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static IEnumerable<object> Items(object owner, string name) =>
        ((IEnumerable)Property(owner, name)).Cast<object>();
}
