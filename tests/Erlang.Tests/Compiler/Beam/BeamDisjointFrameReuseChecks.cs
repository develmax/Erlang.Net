using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamBinaryHostNames;

namespace Erlang.Tests;

internal static class BeamDisjointFrameReuseChecks
{
    internal static void Run()
    {
        object compiler = typeof(CodeGeneration).Assembly.GetType(BeamDisjointFrameReuseDiagnostics.Compiler, throwOnError: true)!;
        const int entry = 2;
        const int allocation = 4;
        Array reusable = Instructions(keepOriginal: false);
        object planned = Call(
            compiler,
            BeamDisjointFrameReuseDiagnostics.Plan,
            reusable,
            entry,
            reusable.Length
        );
        if (planned is not Array layouts
            || layouts.GetValue(allocation - entry) is not ValueTuple<int, int[]> layout
            || layout.Item1 != 1 || !layout.Item2.SequenceEqual(new[] { 0, 0 }))
        {
            throw new InvalidOperationException(BeamDisjointFrameReuseDiagnostics.ReusableFrame);
        }

        Array overlapping = Instructions(keepOriginal: true);
        if (Call(
            compiler,
            BeamDisjointFrameReuseDiagnostics.Plan,
            overlapping,
            entry,
            overlapping.Length
        ) is not null)
        {
            throw new InvalidOperationException(BeamDisjointFrameReuseDiagnostics.OverlappingValues);
        }
    }

    private static Array Instructions(bool keepOriginal)
    {
        object x0 = Op(X, 0);
        object y0 = Op(Y, 0);
        object y1 = Op(Y, 1);
        object one = Op(Unsigned, 1);
        object two = Op(Unsigned, 2);
        var instructions = new List<object>();
        Add(BeamEmissionFormat.Label, one);
        Add(
            FunctionInfo,
            Op(AtomKind, 1),
            Op(AtomKind, 2),
            one
        );
        Add(BeamEmissionFormat.Label, two);
        Add(IsAtom, Op(BeamBinaryHostNames.Label, 3), x0);
        Add(BeamEmissionFormat.Allocate, two, one);
        Add(Move, x0, y0);
        Add(
            GcBifTwo,
            Op(BeamBinaryHostNames.Label, 0),
            one,
            Op(Unsigned, 0),
            y0,
            Op(IntegerKind, 1),
            x0
        );
        Add(Move, x0, y1);
        Add(
            IsLess,
            Op(BeamBinaryHostNames.Label, 4),
            y1,
            keepOriginal ? y0 : Op(IntegerKind, 10)
        );
        Add(Move, y1, x0);
        Add(Deallocate, two);
        Add(Return);
        Add(BeamEmissionFormat.Label, Op(Unsigned, 4));
        Add(Move, y1, x0);
        Add(Deallocate, two);
        Add(Return);
        Add(BeamEmissionFormat.Label, Op(Unsigned, 3));
        Add(BeamEmissionFormat.Allocate, one, one);
        Add(Move, x0, y0);
        Add(Move, y0, x0);
        Add(Deallocate, one);
        Add(Return);

        Array result = Array.CreateInstance(Type(Instruction), instructions.Count);
        for (int index = 0; index < instructions.Count; index++)
        {
            result.SetValue(instructions[index], index);
        }

        return result;

        void Add(byte opcode, params object[] operands)
        {
            object definition = New(
                Definition,
                opcode,
                string.Empty,
                operands.Length,
                false
            );
            instructions.Add(New(
                Instruction,
                instructions.Count,
                definition,
                Ops(operands)
            ));
        }
    }
}
