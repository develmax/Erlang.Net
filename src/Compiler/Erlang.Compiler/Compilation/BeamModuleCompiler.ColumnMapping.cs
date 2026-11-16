using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed record ColumnMapping(
        Term First,
        Term Second,
        Term Third,
        ComprehensionQualifier.Generator Generator
    ) : MappingPlan;

    private static ColumnMapping? ReadColumnMapping(IReadOnlyList<Clause> clauses)
    {
        if (clauses is not [
            {
                Guard: null, Patterns: [Pattern.Variable input], Body: Expr.List
                {
                    Items: [var first],
                    Tail: Expr.ListComprehension
                    {
                        Items: [Expr.Tuple { Items: [Expr.Variable projected, var second, var third] }],
                        Qualifiers: [ComprehensionQualifier.Generator
                        {
                            Strict: false, Pattern: Pattern.Variable element,
                            Source: Expr.Variable source
                        } generator]
                    }
                }
            }
        ] || !DistinctBindings(input, element) || source.Name != input.Name || projected.Name != element.Name
            || !AttributeLiteral.TryNormalizeConstant(first, out Term firstValue)
            || !AttributeLiteral.TryNormalizeConstant(second, out Term secondValue)
            || !AttributeLiteral.TryNormalizeConstant(third, out Term thirdValue))
        {
            return null;
        }

        return new(
            firstValue,
            secondValue,
            thirdValue,
            generator
        );
    }

    private static BeamSourceLocation ColumnLocation(ColumnMapping flow, BeamSourceLocation location) =>
        flow.Generator.GeneratorLine == 0 ? location : new(flow.Generator.GeneratorFile, flow.Generator.GeneratorLine);

    private void EmitColumnMapping(ColumnMapping flow, BeamSourceLocation location,
        IReadOnlyDictionary<(string Name, int Arity), int> entries)
    {
        Op(Allocate, BeamWire.UnsignedTag, 0);
        Operand(BeamWire.UnsignedTag, 1);
        var generator = ColumnLocation(flow, location);
        if (generator != location)
        {
            EmitLine(generator);
        }
        EmitFieldCall(1, entries[(MappingName(activeFunction, 1), 1)]);
        Op(TestHeap, BeamWire.UnsignedTag, ConsHeapWords);
        Operand(BeamWire.UnsignedTag, 1);
        instructions.WriteByte(PutList);
        LiteralOperand(flow.First);
        Operand(XRegisterTag, 0);
        Operand(XRegisterTag, 0);
        EmitFrameReturn();
    }

    private void EmitColumnMappingHelper(
        string name,
        ColumnMapping flow,
        BeamSourceLocation location,
        int entry
    )
    {
        StartFunction(
            name,
            1,
            ColumnLocation(flow, location),
            exported: false
        );
        int empty = nextLabel++;
        int invalid = nextLabel++;
        EmitNonemptyList(0, empty);
        Op(Allocate, BeamWire.UnsignedTag, 1);
        Operand(BeamWire.UnsignedTag, 1);
        EmitListParts(
            0,
            YRegisterTag,
            0,
            XRegisterTag,
            0
        );
        EmitFieldCall(1, entry);
        Op(TestHeap, BeamWire.UnsignedTag, 4 + ConsHeapWords);
        Operand(BeamWire.UnsignedTag, 1);
        EmitTupleHeader(XRegisterTag, 3, 1);
        Operand(YRegisterTag, 0);
        LiteralOperand(flow.Second);
        LiteralOperand(flow.Third);
        Op(PutList, XRegisterTag, 1);
        Operand(XRegisterTag, 0);
        Operand(XRegisterTag, 0);
        EmitFrameReturn(1);
        EmitMappingEnd(empty, invalid);
    }
}
