using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed record NestedListCursorLayout(string Outer, string Inner, Expr Source);

    private readonly Dictionary<string, NestedListCursorLayout> nestedListCursorLayouts = new(StringComparer.Ordinal);

    private void RecordNestedListCursorLayout(
        string outerName,
        string innerName,
        Pattern.Variable outer,
        Pattern.Variable inner,
        Expr outerSource,
        Expr innerSource,
        Expr mapped
    )
    {
        bool identity = mapped is Expr.Variable value && value.Name == inner.Name
            && innerSource is Expr.Call
            {
                Module: not ErlangModuleNames.Module,
                Arguments: [Expr.Variable input]
            } && input.Name == outer.Name;
        bool closedPair = IsClosedList(outerSource) && IsClosedList(innerSource)
            && mapped is Expr.List
            {
                Items: [Expr.Variable first, Expr.Variable second],
                Tail: null
            }
            && first.Name == outer.Name && second.Name == inner.Name;
        if (!identity && !closedPair)
        {
            return;
        }

        var plan = new NestedListCursorLayout(outerName, innerName, innerSource);
        nestedListCursorLayouts.Add(outerName, plan);
        nestedListCursorLayouts.Add(innerName, plan);

        static bool IsClosedList(Expr expression)
        {
            if (expression is not Expr.Literal { Value: Cons first })
            {
                return false;
            }

            Term tail = first.Tail;
            while (tail is Cons cell)
            {
                tail = cell.Tail;
            }

            return tail is Nil;
        }
    }

    private sealed partial class GeneralEmitter
    {
        private bool TryEmitNestedListCursorLayout(FunctionDefinition function)
        {
            if (!owner.nestedListCursorLayouts.TryGetValue(function.Name, out var plan))
            {
                return false;
            }

            bool closedPair = plan.Source is Expr.Literal;
            int width = closedPair ? 3 : 2;
            if (!entries.TryGetValue((plan.Outer, 1), out int outer)
                || !entries.TryGetValue((plan.Inner, width), out int inner)
                || plan.Source is Expr.Call { Module: null } local && !entries.ContainsKey((local.Function, 1)))
            {
                return false;
            }

            int exhausted = Label();
            owner.EmitNonemptyList(0, exhausted);
            if (function.Name == plan.Outer)
            {
                if (plan.Source is Expr.Literal source)
                {
                    owner.EmitListParts(
                        0,
                        XRegisterTag,
                        2,
                        XRegisterTag,
                        1
                    );
                    owner.EmitConstant(source.Value, 0);
                    EmitNestedTail(width, inner);
                    Mark(exhausted);
                    owner.instructions.WriteByte(Return);
                }
                else
                {
                    var producer = (Expr.Call)plan.Source;
                    EmitFrameAllocation(1, 1);
                    owner.EmitListParts(
                        0,
                        XRegisterTag,
                        0,
                        YRegisterTag,
                        0
                    );
                    var callLocation = EmitListMapperCalls([new ListMapperCall(producer, [null])], location);
                    owner.MoveRegister(
                        YRegisterTag,
                        0,
                        XRegisterTag,
                        1
                    );
                    owner.Op(TailLocalCallWithFrame, BeamWire.UnsignedTag, width);
                    owner.Operand(LabelTag, inner);
                    owner.Operand(BeamWire.UnsignedTag, 1);
                    owner.EmitMappingEnd(exhausted, Label(), callLocation == location ? null : location);
                }

                return true;
            }

            // The current inner head is live across recursion. A pure two-cell value
            // also retains the outer head, then constructs its cells after the call.
            EmitFrameAllocation(width, closedPair ? 2 : 1);
            if (closedPair)
            {
                owner.MoveRegister(
                    XRegisterTag,
                    2,
                    YRegisterTag,
                    1
                );
            }

            owner.EmitListParts(
                0,
                YRegisterTag,
                0,
                XRegisterTag,
                0
            );
            if (closedPair)
            {
                EmitClosedNestedListPair(inner, width);
            }
            else
            {
                owner.EmitRecursiveCons(inner, width);
            }

            Mark(exhausted);
            int invalid = closedPair ? 0 : Label();
            if (!closedPair)
            {
                owner.EmitNilTest(XRegisterTag, 0, invalid);
            }

            owner.MoveToX0(XRegisterTag, 1);
            EmitNestedTail(1, outer);
            if (!closedPair)
            {
                Mark(invalid);
                owner.EmitListMappingError(1, 0, ErlangErrorReasons.BadGenerator);
            }

            return true;
        }

        private void EmitClosedNestedListPair(int inner, int width)
        {
            owner.EmitFieldCall(width, inner);
            owner.Op(TestHeap, BeamWire.UnsignedTag, ConsHeapWords * 3);
            owner.Operand(BeamWire.UnsignedTag, 1);

            owner.Op(PutList, YRegisterTag, 0);
            owner.LiteralOperand(Nil.Value);
            owner.Operand(XRegisterTag, 1);
            owner.Op(PutList, YRegisterTag, 1);
            owner.Operand(XRegisterTag, 1);
            owner.Operand(XRegisterTag, 1);
            owner.Op(PutList, XRegisterTag, 1);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(XRegisterTag, 0);
            owner.EmitFrameReturn(2);
        }

        private void EmitNestedTail(int arity, int entry)
        {
            owner.Op(TailLocalCall, BeamWire.UnsignedTag, arity);
            owner.Operand(LabelTag, entry);
        }
    }
}
