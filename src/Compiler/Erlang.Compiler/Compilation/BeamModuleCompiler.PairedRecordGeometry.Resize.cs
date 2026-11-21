using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private static GeometryResize? ReadGeometryResize(Expr body, GeometryEvent source)
        {
            if (body is not Expr.Sequence
                {
                    Items: [Expr.Call minTop, Expr.Call minBottom, Expr.Call sizeTop, Expr.Call sizeBottom, Expr.Call refresh, var reply]
                }
                || minTop.Arguments is not [Expr.Variable firstReceiver, var firstSize]
                || minBottom.Arguments is not [Expr.Variable secondReceiver, var secondSize]
                || sizeTop.Arguments is not [Expr.Variable firstChanged, var firstRectangle]
                || sizeBottom.Arguments is not
                [
                    Expr.Variable secondChanged,
                    Expr.Tuple
                {
                    Items: [Expr.Literal { Value: Integer horizontal }, Expr.Binary vertical, Expr.Variable width, Expr.Binary height]
                }
                ]
                || !new[] { minTop, minBottom, sizeTop, sizeBottom }.All(IsGeometryCall)
                || firstReceiver.Name == secondReceiver.Name
                || firstChanged.Name != firstReceiver.Name || secondChanged.Name != secondReceiver.Name
                || !source.Fields.TryGetValue(firstReceiver.Name, out int top)
                || !source.Fields.TryGetValue(secondReceiver.Name, out int bottom)
                || ReadGeometryStateCall(refresh, source) is not int parent
                || ReadGeometryReply(reply, source.State) is not { } tag
                || width.Name != source.Values[0]
                || ReadPair(firstSize) is not { } first || ReadPair(secondSize) is not { } second || ReadPair(firstRectangle) is not { } third
                || FirstDivisor(first, source.Values[1]) is not { } divisor
                || !divisor.Equals(FirstDivisor(second, source.Values[1])) || !divisor.Equals(FirstDivisor(third, source.Values[1]))
                || !divisor.Equals(FirstDivisor(vertical, source.Values[1])) || !divisor.Equals(FirstDivisor(height, source.Values[1])))
            {
                return null;
            }

            return new(
                minTop,
                minBottom,
                sizeTop,
                sizeBottom,
                refresh,
                first,
                second,
                third,
                vertical,
                divisor,
                horizontal,
                parent,
                top,
                bottom,
                tag
            );

            Expr.Binary? ReadPair(Expr expression) => expression is Expr.Tuple
            {
                Items: [Expr.Variable w, Expr.Binary division]
            } && w.Name == source.Values[0] ? division : null;
        }

        private static Integer? FirstDivisor(Expr.Binary expression, string from) => expression is
        {
            Operator: ErlangOperators.IntegerDivide, Left: Expr.Variable input,
            Right: Expr.Literal { Value: Integer constant }
        } && input.Name == from && !constant.Value.IsZero ? constant : null;

        private void EmitGeometryResize(GeometryResize plan)
        {
            const int frame = 5;
            const int bottom = 0;
            const int top = 1;
            const int width = 2;
            const int height = 3;
            const int state = 4;
            owner.EmitTuplePart(2, 1, 0);
            var previous = location;
            owner.EmitArithmeticLine(plan.First.SourceLine, ref previous);
            WriteGeometryBif(plan.First.Operator, 3);
            owner.Operand(XRegisterTag, 0);
            owner.LiteralOperand(plan.Divisor);
            owner.Operand(XRegisterTag, 3);
            EmitFrameAllocation(4, frame, heapWords: 3);
            EmitFrameInitialization(0, 1);
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                height
            );
            owner.MoveRegister(
                XRegisterTag,
                1,
                YRegisterTag,
                state
            );
            ProjectGeometry(
                XRegisterTag,
                2,
                0,
                YRegisterTag,
                width
            );
            owner.EmitTagMatchTuple(1, (YRegisterTag, width), (XRegisterTag, 3));
            ProjectGeometry(
                YRegisterTag,
                state,
                plan.Top,
                YRegisterTag,
                top
            );
            owner.MoveRegister(
                YRegisterTag,
                top,
                XRegisterTag,
                0
            );
            EmitWindowCall(plan.MinTop, ref previous);

            Divide(plan.Second, height);
            EmitGeometryTuple(1, (YRegisterTag, width), (XRegisterTag, 0));
            ProjectGeometry(
                YRegisterTag,
                state,
                plan.Bottom,
                YRegisterTag,
                bottom
            );
            owner.MoveRegister(
                YRegisterTag,
                bottom,
                XRegisterTag,
                0
            );
            EmitWindowCall(plan.MinBottom, ref previous);

            Divide(plan.Third, height);
            EmitGeometryTuple(1, (YRegisterTag, width), (XRegisterTag, 0));
            owner.MoveRegister(
                YRegisterTag,
                top,
                XRegisterTag,
                0
            );
            owner.MoveRegister(
                YRegisterTag,
                bottom,
                YRegisterTag,
                top
            );
            TrimGeometry(1, 4);
            EmitWindowCall(plan.SizeTop, ref previous);

            Divide(plan.Fourth, height - 1);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 5);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.EmitTupleHeader(XRegisterTag, 4, 1);
            owner.LiteralOperand(plan.Horizontal);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(YRegisterTag, width - 1);
            owner.Operand(XRegisterTag, 0);
            owner.MoveRegister(
                YRegisterTag,
                top - 1,
                XRegisterTag,
                0
            );
            TrimGeometry(3, 1);
            EmitWindowCall(plan.SizeBottom, ref previous);
            owner.EmitTuplePart(
                0,
                plan.Parent,
                0,
                YRegisterTag
            );
            EmitWindowCall(plan.Refresh, ref previous);
            EmitRetainedRecordReply([plan.Reply]);

            void Divide(Expr.Binary expression, int slot)
            {
                owner.EmitArithmeticLine(expression.SourceLine, ref previous);
                WriteGeometryBif(expression.Operator, 0);
                owner.EmitTypedRegister(slot, IntegerTypeBits, registerTag: YRegisterTag);
                owner.LiteralOperand(plan.Divisor);
                owner.Operand(XRegisterTag, 0);
            }
        }

        private sealed record GeometryResize(
            Expr.Call MinTop,
            Expr.Call MinBottom,
            Expr.Call SizeTop,
            Expr.Call SizeBottom,
            Expr.Call Refresh,
            Expr.Binary First,
            Expr.Binary Second,
            Expr.Binary Third,
            Expr.Binary Fourth,
            Integer Divisor,
            Integer Horizontal,
            int Parent,
            int Top,
            int Bottom,
            Atom Reply
        );
    }
}
