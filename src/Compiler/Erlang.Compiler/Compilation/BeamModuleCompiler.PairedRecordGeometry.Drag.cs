using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private static GeometryDrag? ReadGeometryDrag(Expr body, GeometryEvent source)
        {
            if (body is not Expr.Sequence
                {
                    Items:
                    [
                        Expr.Match
                    {
                        Pattern: Pattern.Tuple { Items: [Pattern.Variable width, Pattern.Variable height] },
                        Value: Expr.Call size
                    },
                        Expr.Match
                    {
                        Pattern: Pattern.Variable difference,
                        Value: Expr.Binary
                        {
                            Operator: ErlangOperators.Minus, Left: Expr.Variable oldHeight, Right: Expr.Variable eventHeight
                        } subtraction
                    },
                        Expr.Match
                    {
                        Pattern: Pattern.Tuple { Items: [Pattern.Variable horizontal, Pattern.Variable { Name: VariableScopeNames.Wildcard }] },
                        Value: Expr.Call { Arguments: [Expr.Variable bottom] } position
                    },
                        Expr.Call minBottom,
                        Expr.Call minTop,
                        Expr.Call sizeBottom,
                        Expr.Call sizeTop,
                        Expr.Call refresh,
                        var reply
                    ]
                }
                || !DistinctBindings(
                    width,
                    height,
                    difference,
                    horizontal
                )
                || new[] { width, height, difference, horizontal }.Any(binding => source.Bound.Contains(binding.Name))
                || eventHeight.Name != source.Values[3] || oldHeight.Name != height.Name
                || !IsGeometryCall(position)
                || !source.Fields.TryGetValue(bottom.Name, out int bottomField)
                || sizeTop.Arguments is not [Expr.Variable top, _] || top.Name == bottom.Name
                || !source.Fields.TryGetValue(top.Name, out int topField)
                || ReadGeometryStateCall(size, source) is not int parent || ReadGeometryStateCall(refresh, source) != parent
                || !IsGeometryTupleCall(
                    minBottom,
                    bottom.Name,
                    width.Name,
                    difference.Name
                )
                || !IsGeometryTupleCall(
                    minTop,
                    top.Name,
                    width.Name,
                    source.Values[3]
                )
                || !IsGeometryTupleCall(
                    sizeBottom,
                    bottom.Name,
                    horizontal.Name,
                    source.Values[1],
                    width.Name,
                    difference.Name
                )
                || !IsGeometryTupleCall(
                    sizeTop,
                    top.Name,
                    width.Name,
                    source.Values[3]
                )
                || ReadGeometryReply(reply, source.State) is not { } tag)
            {
                return null;
            }

            return new(
                size,
                subtraction,
                position,
                minBottom,
                minTop,
                sizeBottom,
                sizeTop,
                refresh,
                parent,
                topField,
                bottomField,
                tag
            );
        }

        private static bool IsGeometryTupleCall(Expr.Call call, string receiver, params string[] fields) =>
            IsGeometryCall(call) && call.Arguments is [Expr.Variable target, Expr.Tuple tuple]
                && target.Name == receiver && tuple.Items.Count == fields.Length
                && tuple.Items.OfType<Expr.Variable>().Select(item => item.Name).SequenceEqual(fields, StringComparer.Ordinal);

        private void EmitGeometryDrag(GeometryDrag plan, int badPosition, int badSize)
        {
            const int frame = 8;
            const int offset = 5;
            const int rect = 6;
            const int state = 7;
            const int width = 4;
            const int extent = 3;
            const int bottom = 2;
            const int position = 1;
            const int top = 0;
            EmitFrameAllocation(4, frame);
            EmitFrameInitialization(0, offset);
            owner.MoveRegister(
                XRegisterTag,
                3,
                YRegisterTag,
                offset
            );
            owner.MoveRegister(
                XRegisterTag,
                2,
                YRegisterTag,
                rect
            );
            owner.MoveRegister(
                XRegisterTag,
                1,
                YRegisterTag,
                state
            );
            owner.EmitTuplePart(1, plan.Parent, 0);
            var previous = location;
            EmitWindowCall(plan.Size, ref previous);
            owner.EmitTupleCheck(0, 2, badSize);
            ProjectGeometry(
                YRegisterTag,
                rect,
                3,
                YRegisterTag,
                rect
            );
            ProjectGeometry(
                XRegisterTag,
                0,
                0,
                YRegisterTag,
                width
            );
            owner.EmitTuplePart(0, 1, 0);
            owner.EmitArithmeticLine(plan.Subtraction.SourceLine, ref previous);
            WriteGeometryBif(plan.Subtraction.Operator, 1);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(YRegisterTag, rect);
            owner.Operand(YRegisterTag, extent);

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
            EmitWindowCall(plan.Position, ref previous);
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                position
            );
            owner.EmitTupleCheck(0, 2, badPosition);
            EmitGeometryTuple(0, (YRegisterTag, width), (YRegisterTag, extent));
            owner.MoveRegister(
                YRegisterTag,
                bottom,
                XRegisterTag,
                0
            );
            EmitWindowCall(plan.MinBottom, ref previous);

            EmitGeometryTuple(0, (YRegisterTag, width), (YRegisterTag, rect));
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

            owner.Op(TestHeap, BeamWire.UnsignedTag, 5);
            owner.Operand(BeamWire.UnsignedTag, 0);
            owner.EmitTuplePart(
                position,
                0,
                0,
                YRegisterTag
            );
            owner.EmitTagMatchTuple(
                1,
                (XRegisterTag, 0),
                (YRegisterTag, offset),
                (YRegisterTag, width),
                (YRegisterTag, extent)
            );
            owner.MoveRegister(
                YRegisterTag,
                bottom,
                XRegisterTag,
                0
            );
            owner.MoveRegister(
                YRegisterTag,
                top,
                YRegisterTag,
                offset
            );
            TrimGeometry(4, 4);
            EmitWindowCall(plan.SizeBottom, ref previous);

            EmitGeometryTuple(0, (YRegisterTag, width - 4), (YRegisterTag, rect - 4));
            owner.MoveRegister(
                YRegisterTag,
                offset - 4,
                XRegisterTag,
                0
            );
            TrimGeometry(3, 1);
            EmitWindowCall(plan.SizeTop, ref previous);
            owner.EmitTuplePart(
                0,
                plan.Parent,
                0,
                YRegisterTag
            );
            EmitWindowCall(plan.Refresh, ref previous);
            EmitRetainedRecordReply([plan.Reply]);
        }

        private void ProjectGeometry(
            int sourceTag,
            int source,
            int index,
            int targetTag,
            int target
        )
        {
            owner.Op(GetTupleElement, sourceTag, source);
            owner.Operand(BeamWire.UnsignedTag, index);
            owner.Operand(targetTag, target);
        }

        private void EmitGeometryTuple(int live, params (int Tag, int Value)[] operands)
        {
            owner.Op(TestHeap, BeamWire.UnsignedTag, operands.Length + 1);
            owner.Operand(BeamWire.UnsignedTag, live);
            owner.EmitTagMatchTuple(1, operands);
        }

        private void TrimGeometry(int removed, int retained)
        {
            owner.Op(TrimFrame, BeamWire.UnsignedTag, removed);
            owner.Operand(BeamWire.UnsignedTag, retained);
        }

        private void WriteGeometryBif(string operation, int live)
        {
            owner.Op(GcBifTwo, LabelTag, 0);
            owner.Operand(BeamWire.UnsignedTag, live);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, operation, 2));
        }

        private sealed record GeometryDrag(
            Expr.Call Size,
            Expr.Binary Subtraction,
            Expr.Call Position,
            Expr.Call MinBottom,
            Expr.Call MinTop,
            Expr.Call SizeBottom,
            Expr.Call SizeTop,
            Expr.Call Refresh,
            int Parent,
            int Top,
            int Bottom,
            Atom Reply
        );
    }
}
