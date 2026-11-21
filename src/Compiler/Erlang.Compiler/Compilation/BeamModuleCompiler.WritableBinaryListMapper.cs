using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private const int NibbleSegmentWidth = 4;

    private sealed record WritableBinaryListMapper(
        string Name,
        int Width,
        int ResultType,
        long? Lower = null,
        long? Upper = null
    );

    private WritableBinaryListMapper? ReadWritableBinaryListMapper(
        Pattern pattern,
        IReadOnlyList<BitSegment> segments,
        int line,
        IReadOnlyDictionary<(string Name, int Arity), FunctionDefinition> localFunctions
    )
    {
        if (pattern is not Pattern.Variable { Name: not VariableScopeNames.Wildcard } item
            || segments is not
            [
            {
                Value: Expr.Call
                {
                    Module: null, DynamicRemote: false, Arguments: [Expr.Variable input]
                } call
            } segment
            ] || input.Name != item.Name || (call.SourceLine != 0 && call.SourceLine != line)
            || !IsWritableIntegerSegment(segment)
            || !localFunctions.TryGetValue((call.Function, 1), out FunctionDefinition? target)
            || IsDeclaredNative((target.Name, target.Arity)) || target.IsInlineFailure)
        {
            return null;
        }

        int width = segment.Size switch
        {
            null => BitSyntaxDefaults.IntegerWidth,
            Expr.Literal { Value: Integer size } when size.Value == NibbleSegmentWidth => NibbleSegmentWidth,
            Expr.Literal { Value: Integer size } when size.Value == BitSyntaxDefaults.IntegerWidth => BitSyntaxDefaults.IntegerWidth,
            _ => 0
        };
        if (width == 0)
        {
            return null;
        }

        if (ReadLiteralIntegerResult(target) is { Lower: >= 0 } range && range.Upper < (1 << width))
        {
            return new WritableBinaryListMapper(
                call.Function,
                width,
                IntegerTypeBits,
                range.Lower,
                range.Upper
            );
        }

        // Every normal exit of a parameter-minus-integer clause is numeric.
        // In particular a number guard does not rule out a float.
        return target.Clauses.Count > 0 && target.Clauses.All(ReturnsNumericDifference)
            ? new WritableBinaryListMapper(call.Function, width, NumberTypeBits)
            : null;

        static bool ReturnsNumericDifference(Clause clause) =>
            clause is
            {
                Patterns: [Pattern.Variable { Name: not VariableScopeNames.Wildcard } formal],
                Body: Expr.Binary
                {
                    Operator: ErlangOperators.Minus, Left: Expr.Variable source, Right: Expr.Literal { Value: Integer }
                }
            } && formal.Name == source.Name;
    }

    private void EmitWritableMappedBinaryIteration(
        int entry,
        int conversion,
        WritableBinaryListMapper mapper
    )
    {
        // The call clobbers X, so retain both owned roots in the matched branch only.
        Op(Allocate, BeamWire.UnsignedTag, 2);
        Operand(BeamWire.UnsignedTag, 2);
        MoveRegister(
            XRegisterTag,
            1,
            YRegisterTag,
            1
        );
        Op(GetList, XRegisterTag, 0);
        Operand(XRegisterTag, 0);
        Operand(YRegisterTag, 0);
        Op(LocalCall, BeamWire.UnsignedTag, 1);
        Operand(LabelTag, conversion);
        BeginBinaryConstruction(
            failure: 0,
            live: 1,
            unit: mapper.Width,
            destination: 1,
            segmentCount: 2
        );
        EmitWritableBinaryAccumulator(YRegisterTag);
        EmitBinarySegmentPrefix(
            BitSegmentTypes.Integer,
            2,
            BitSyntaxDefaults.NumericUnit,
            BitByteOrders.Big
        );
        EmitTypedRegister(
            0,
            mapper.ResultType,
            mapper.Lower,
            mapper.Upper
        );
        LiteralOperand(Term.I(mapper.Width));
        MoveRegister(
            YRegisterTag,
            0,
            XRegisterTag,
            0
        );
        Op(TailLocalCallWithFrame, BeamWire.UnsignedTag, WritableBinaryListHelperArity);
        Operand(LabelTag, entry);
        Operand(BeamWire.UnsignedTag, 2);
    }
}
