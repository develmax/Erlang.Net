using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitReportedStateEvent(FunctionDefinition function)
        {
            if (function.Clauses is not
                [
                    var prepend,
                    var update,
                    {
                        Patterns: [Pattern.Variable ignored, Pattern.Variable state], Guard: null,
                        Body: Expr.Tuple { Items: [Expr.Literal { Value: Atom tag }, Expr.Variable unchanged] }
                    }
                ] || ignored.Name == state.Name || state.Name == VariableScopeNames.Wildcard
                || unchanged.Name != state.Name
                || ReadBranch(prepend) is not { } add || ReadBranch(update) is not { } modify
                || add.Event.Equals(modify.Event)
                || add.Result is not Expr.List { Items: [Expr.Variable head], Tail: Expr.Variable tail }
                || head.Name != add.Item || tail.Name != add.State
                || modify.Result is not Expr.Call
                {
                    Module: not null, DynamicRemote: false,
                    Arguments: [Expr.Variable key, var rawPosition, Expr.Variable collection]
                } change || key.Name != modify.Item || collection.Name != modify.State
                || !AttributeLiteral.TryNormalizeConstant(rawPosition, out Term position))
            {
                return false;
            }

            int appendLabel = Label();
            int updateLabel = Label();
            int fallback = Label();
            EmitTupleShapeTest(XRegisterTag, 0, 2, fallback);
            owner.EmitTuplePart(0, 0, 2);
            owner.EmitTuplePart(0, 1, 0);
            owner.Op(SelectValue, XRegisterTag, 2);
            owner.Operand(LabelTag, fallback);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, 4);
            foreach (var (selector, target) in new[] { (add.Event, appendLabel), (modify.Event, updateLabel) }
                .OrderBy(branch => branch.Event))
            {
                owner.LiteralOperand(selector);
                owner.Operand(LabelTag, target);
            }

            Mark(appendLabel);
            EmitReport(add.Report, add.Category, add.Action);
            Heap(ConsHeapWords + 3, 0);
            owner.Op(PutList, YRegisterTag, 0);
            owner.Operand(YRegisterTag, 1);
            owner.Operand(XRegisterTag, 0);
            EmitResult(0);
            owner.EmitFrameReturn(2);

            Mark(updateLabel);
            EmitReport(modify.Report, modify.Category, modify.Action);
            owner.EmitConstant(position, 1);
            owner.MoveRegister(YRegisterTag, 1, XRegisterTag, 2);
            owner.MoveRegister(YRegisterTag, 0, XRegisterTag, 0);
            TrimDetailFrame(2, 0);
            owner.EmitCallLine(change, CallLocation(modify.Report, location));
            owner.EmitStaticRemoteCall(change);
            Heap(3, 1);
            EmitResult(0);
            owner.EmitFrameReturn(0);

            Mark(fallback);
            Heap(3, 2);
            EmitResult(1);
            owner.instructions.WriteByte(Return);

            return true;

            (Atom Event, string Item, string State, Expr.Call Report, Atom Category, Atom Action, Expr Result)? ReadBranch(Clause clause)
            {
                if (clause is not
                    {
                        Guard: null,
                        Patterns: [Pattern.Tuple { Items: [Pattern.Literal { Value: Atom selector }, Pattern.Variable item] },
                            Pattern.Variable saved],
                        Body: Expr.Sequence { Items:
                            [Expr.Call { Module: not null, DynamicRemote: false, Arguments:
                                [Expr.List { Items:
                                    [Expr.Tuple { Items: [Expr.Literal { Value: Atom category },
                                        Expr.Tuple { Items: [Expr.Literal { Value: Atom action }, Expr.Variable reported] }] }],
                                    Tail: null }] } report,
                                Expr.Tuple { Items: [Expr.Literal { Value: Atom resultTag }, var result] }] }
                    } || !DistinctBindings(item, saved) || reported.Name != item.Name || !resultTag.Equals(tag))
                {
                    return null;
                }

                return (selector, item.Name, saved.Name, report, category, action, result);
            }

            void EmitReport(Expr.Call call, Atom category, Atom action)
            {
                owner.Op(AllocateHeap, BeamWire.UnsignedTag, 2);
                owner.Operand(BeamWire.UnsignedTag, 6 + ConsHeapWords);
                owner.Operand(BeamWire.UnsignedTag, 2);
                owner.MoveRegister(XRegisterTag, 0, YRegisterTag, 0);
                owner.MoveRegister(XRegisterTag, 1, YRegisterTag, 1);
                owner.EmitTagMatchTuple(0, (AtomTag, owner.Intern(action.Name)), (XRegisterTag, 0));
                owner.EmitTagMatchTuple(0, (AtomTag, owner.Intern(category.Name)), (XRegisterTag, 0));
                owner.Op(PutList, XRegisterTag, 0);
                owner.LiteralOperand(Nil.Value);
                owner.Operand(XRegisterTag, 0);
                owner.EmitCallLine(call, location);
                owner.EmitStaticRemoteCall(call);
            }

            void Heap(int words, int live)
            {
                owner.Op(TestHeap, BeamWire.UnsignedTag, words);
                owner.Operand(BeamWire.UnsignedTag, live);
            }

            void EmitResult(int register) => owner.EmitTagMatchTuple(0, (AtomTag, owner.Intern(tag.Name)), (XRegisterTag, register));
        }
    }
}
