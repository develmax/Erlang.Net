using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitCheckedDetailTab(FunctionDefinition function)
        {
            if (function.Clauses is not [
                {
                    Guard: null, Patterns: [Pattern.Variable notebook, Pattern.Variable title,
                    Pattern.Variable callback, Pattern.Variable arguments], Body: Expr.Sequence
                    {
                        Items: [
                        Expr.Match { Pattern: Pattern.Variable panel, Value: Expr.Call { Module: null } initialize },
                        Expr.Match
                        {
                            Pattern: Pattern.Literal { Value: Atom inserted },
                            Value: Expr.Call { Module: not null, DynamicRemote: false } insert
                        },
                        Expr.Variable result]
                    }
                }
            ] || !DistinctBindings(
                notebook,
                title,
                callback,
                arguments,
                panel
            ) || panel.Name != result.Name
                || !MatchesDetailArguments(
                    initialize,
                    notebook,
                    callback,
                    arguments
                )
                || !MatchesDetailArguments(
                    insert,
                    notebook,
                    panel,
                    title
                )
                || !entries.TryGetValue((initialize.Function, 3), out int target))
            {
                return false;
            }

            int failed = Label();
            owner.Op(Allocate, BeamWire.UnsignedTag, 2);
            owner.Operand(BeamWire.UnsignedTag, 4);
            owner.MoveRegister(
                XRegisterTag,
                1,
                YRegisterTag,
                0
            );
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                1
            );
            owner.MoveRegister(
                XRegisterTag,
                2,
                XRegisterTag,
                1
            );
            owner.MoveRegister(
                XRegisterTag,
                3,
                XRegisterTag,
                2
            );
            var previous = location;
            EmitWindowLocal(initialize, target, ref previous);
            Load(1, 1);
            SaveWindowResult(1);
            Load(0, 2);
            owner.Op(Swap, XRegisterTag, 0);
            owner.Operand(XRegisterTag, 1);
            TrimDetailFrame(1, 1);
            EmitWindowCall(insert, ref previous);
            owner.EmitAtomTest(0, inserted.Name, failed);
            Load(0);
            owner.EmitFrameReturn(1);
            Mark(failed);
            owner.Op(BadMatch, XRegisterTag, 0);

            return true;
        }

        private bool TryEmitScrolledDetailPanel(FunctionDefinition function)
        {
            if (function.Clauses is not [
                {
                    Guard: null, Patterns: [Pattern.Variable parent, Pattern.Variable callback, Pattern.Variable arguments],
                    Body: Expr.Sequence
                    {
                        Items: [
                        Expr.Match { Pattern: Pattern.Variable panel, Value: Expr.Call { Module: not null } create },
                        Expr.Call { Arguments: [Expr.Variable scrollPanel, var horizontal, var vertical] } enable,
                        Expr.Call { Arguments: [Expr.Variable barPanel, var stepX, var stepY, var offsetX, var offsetY] } bars,
                        Expr.Match
                        {
                            Pattern: Pattern.Variable sizer, Value: Expr.Call
                            {
                                Module: not null,
                                Arguments: [var orientation]
                            } createSizer
                        },
                        Expr.Match
                        {
                            Pattern: Pattern.Variable child, Value: Expr.Call
                            {
                                Function: AutoImportNames.Apply, Module: null or ErlangModuleNames.Module, DynamicRemote: false,
                                Arguments: [Expr.Variable action, Expr.List
                                {
                                    Items: [Expr.Variable callbackPanel],
                                    Tail: Expr.Variable tail
                                }]
                            } apply
                        },
                        Expr.Call { Arguments: [Expr.Variable layout, Expr.Variable window, var options] } add,
                        Expr.Call setSizer, Expr.Variable returned]
                    }
                }
            ] || !DistinctBindings(
                parent,
                callback,
                arguments,
                panel,
                sizer,
                child
            )
                || !IsWindowCall(create, parent) || !IsWindowCall(setSizer, panel, sizer)
                || scrollPanel.Name != panel.Name || barPanel.Name != panel.Name || callbackPanel.Name != panel.Name
                || action.Name != callback.Name || tail.Name != arguments.Name || returned.Name != panel.Name
                || layout.Name != sizer.Name || window.Name != child.Name || !owner.ResolvesErlangBif(apply)
                || !IsStaticWindowCall(enable) || !IsStaticWindowCall(bars) || !IsStaticWindowCall(createSizer) || !IsStaticWindowCall(add)
                || !AttributeLiteral.TryNormalizeConstant(horizontal, out Term horizontalValue)
                || !AttributeLiteral.TryNormalizeConstant(vertical, out Term verticalValue)
                || !AttributeLiteral.TryNormalizeConstant(stepX, out Term xValue)
                || !AttributeLiteral.TryNormalizeConstant(stepY, out Term yValue)
                || !AttributeLiteral.TryNormalizeConstant(offsetX, out Term offsetXValue)
                || !AttributeLiteral.TryNormalizeConstant(offsetY, out Term offsetYValue)
                || !AttributeLiteral.TryNormalizeConstant(orientation, out Term orientationValue)
                || !AttributeLiteral.TryNormalizeConstant(options, out Term optionValue))
            {
                return false;
            }

            owner.Op(Allocate, BeamWire.UnsignedTag, 3);
            owner.Operand(BeamWire.UnsignedTag, 3);
            owner.InitStackSlot(0);
            owner.MoveRegister(
                XRegisterTag,
                2,
                YRegisterTag,
                1
            );
            owner.MoveRegister(
                XRegisterTag,
                1,
                YRegisterTag,
                2
            );
            var previous = location;
            EmitWindowCall(create, ref previous);
            SaveWindowResult(0);
            owner.EmitConstant(verticalValue, 2);
            owner.EmitConstant(horizontalValue, 1);
            EmitWindowCall(enable, ref previous);
            owner.EmitConstant(yValue, 2);
            owner.EmitConstant(offsetXValue, 3);
            owner.EmitConstant(xValue, 1);
            owner.EmitConstant(offsetYValue, 4);
            Load(0);
            EmitWindowCall(bars, ref previous);
            owner.EmitConstant(orientationValue, 0);
            EmitWindowCall(createSizer, ref previous);
            owner.Op(TestHeap, BeamWire.UnsignedTag, ConsHeapWords);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.Op(PutList, YRegisterTag, 0);
            owner.Operand(YRegisterTag, 1);
            owner.Operand(XRegisterTag, 1);
            owner.Op(Swap, YRegisterTag, 2);
            owner.Operand(XRegisterTag, 0);
            owner.MoveRegister(
                YRegisterTag,
                0,
                YRegisterTag,
                1
            );
            TrimDetailFrame(1, 2);
            EmitWindowCall(apply with { Module = ErlangModuleNames.Module }, ref previous);
            owner.MoveRegister(
                XRegisterTag,
                0,
                XRegisterTag,
                1
            );
            owner.EmitConstant(optionValue, 2);
            Load(1);
            EmitWindowCall(add, ref previous);
            Load(1, 1);
            owner.MoveRegister(
                YRegisterTag,
                0,
                YRegisterTag,
                1
            );
            TrimDetailFrame(1, 1);
            Load(0);
            EmitWindowCall(setSizer, ref previous);
            Load(0);
            owner.EmitFrameReturn(1);

            return true;
        }
    }
}
