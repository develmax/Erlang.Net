using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private bool TryEmitComputedPairArgumentList(Clause clause, BeamSourceLocation location)
    {
        if (clause is not
            {
                Guard: null, Patterns: [Pattern.Variable input],
                Body: Expr.Call { Module: not (null or ErlangModuleNames.Module), DynamicRemote: false,
                    Arguments: [Expr.Literal { Value: Atom target }, Expr.List list] } call
            }
            || input.Name == VariableScopeNames.Wildcard)
        {
            return false;
        }

        var pair = list switch
        {
            { Items: [Expr.Variable head, Expr.Call value], Tail: null } => (head, value),
            { Items: [Expr.Variable head], Tail: Expr.List { Items: [Expr.Call value], Tail: null } } => (head, value),
            _ => default
        };
        if (pair.head?.Name != input.Name
            || pair.value is not { Module: not (null or ErlangModuleNames.Module), DynamicRemote: false, Arguments: [var source] } producer
            || !AttributeLiteral.TryNormalizeConstant(source, out Term argument))
        {
            return false;
        }

        Op(Allocate, BeamWire.UnsignedTag, 1);
        Operand(BeamWire.UnsignedTag, 1);
        MoveRegister(XRegisterTag, 0, YRegisterTag, 0);
        EmitConstant(argument, 0);
        EmitCallLine(producer, location);
        EmitStaticRemoteCall(producer);
        Op(TestHeap, BeamWire.UnsignedTag, 2 * ConsHeapWords);
        Operand(BeamWire.UnsignedTag, 1);
        Op(PutList, XRegisterTag, 0);
        Operand(AtomTag, NilAtomIndex);
        Operand(XRegisterTag, 0);
        Op(PutList, YRegisterTag, 0);
        Operand(XRegisterTag, 0);
        Operand(XRegisterTag, 1);
        EmitConstant(target, 0);
        EmitCallLine(call, CallLocation(producer, location));
        Op(TailExternalCallWithFrame, BeamWire.UnsignedTag, 2);
        Operand(BeamWire.UnsignedTag, Import(call.Module!, call.Function, 2));
        Operand(BeamWire.UnsignedTag, 1);
        return true;
    }

    private bool TryEmitPairArgumentList(Clause clause, BeamSourceLocation location)
    {
        if (clause is not
            {
                Guard: null, Patterns: [Pattern.Variable first, Pattern.Variable second],
                Body: Expr.Call { Module: not (null or ErlangModuleNames.Module), DynamicRemote: false, Arguments: [
                    var prefix, Expr.List { Items: [Expr.Variable head, Expr.Variable tail], Tail: null }, var suffix
                ] } call
            }
            || !DistinctBindings(first, second) || head.Name != first.Name || tail.Name != second.Name
            || !AttributeLiteral.TryNormalizeConstant(prefix, out Term leading)
            || !AttributeLiteral.TryNormalizeConstant(suffix, out Term trailing))
        {
            return false;
        }

        Op(TestHeap, BeamWire.UnsignedTag, 2 * ConsHeapWords);
        Operand(BeamWire.UnsignedTag, 2);
        Op(PutList, XRegisterTag, 1);
        Operand(AtomTag, NilAtomIndex);
        Operand(XRegisterTag, 1);
        Op(PutList, XRegisterTag, 0);
        Operand(XRegisterTag, 1);
        Operand(XRegisterTag, 1);
        EmitConstant(trailing, 2);
        EmitConstant(leading, 0);
        EmitCallLine(call, location);
        Op(TailExternalCall, BeamWire.UnsignedTag, 3);
        Operand(BeamWire.UnsignedTag, Import(call.Module!, call.Function, 3));
        return true;
    }

    private bool TryEmitSelfArgumentList(Clause clause, BeamSourceLocation location)
    {
        if (clause is not
            {
                Guard: null, Body: Expr.Call
                {
                    Module: not null, DynamicRemote: false, Arguments: [
            Expr.Literal module, Expr.List { Tail: null } arguments, var options]
                } start
            }
            || !HasUnconditionalParameters(clause) || clause.Patterns.Count == 0 || clause.Patterns.Count >= FirstReservedXRegister
            || arguments.Items.Count != clause.Patterns.Count + 1
            || arguments.Items[^1] is not Expr.Call
            {
                Module: null or ErlangModuleNames.Module, DynamicRemote: false,
                Function: AutoImportNames.Self, Arguments: []
            } self
            || !ResolvesErlangBif(self) || !AttributeLiteral.TryNormalizeConstant(options, out Term optionValue)
            || !arguments.Items.SkipLast(1).Zip(clause.Patterns).All(pair =>
                pair.First is Expr.Variable value && pair.Second is Pattern.Variable parameter && value.Name == parameter.Name))
        {
            return false;
        }

        int arity = clause.Patterns.Count;
        Op(TestHeap, BeamWire.UnsignedTag, (arity + 1) * ConsHeapWords);
        Operand(BeamWire.UnsignedTag, arity);
        Op(BifZero, BeamWire.UnsignedTag, Import(ErlangModuleNames.Module, AutoImportNames.Self, 0));
        Operand(XRegisterTag, arity);
        Op(PutList, XRegisterTag, arity);
        LiteralOperand(Nil.Value);
        Operand(XRegisterTag, arity);
        for (int index = arity - 1; index >= 0; index--)
        {
            Op(PutList, XRegisterTag, index);
            Operand(XRegisterTag, index + 1);
            Operand(XRegisterTag, index == 0 ? 1 : index);
        }
        EmitConstant(optionValue, 2);
        EmitConstant(module.Value, 0);
        EmitCallLine(start, location);
        Op(TailExternalCall, BeamWire.UnsignedTag, 3);
        Operand(BeamWire.UnsignedTag, Import(start.Module, start.Function, 3));

        return true;
    }

    private sealed partial class GeneralEmitter
    {
        private bool TryEmitDetailWindow(FunctionDefinition function)
        {
            if (function.Clauses is not [
                {
                    Guard: null, Patterns: [Pattern.Variable id, Pattern.Variable parentFrame, Pattern.Variable callback,
                    Pattern.Variable app, Pattern.Variable parent, Pattern.Tuple
                    {
                        Items: [
                        Pattern.Variable title, Pattern.Variable details, Pattern.Variable warning]
                    }],
                    Body: Expr.Sequence
                    {
                        Items: [
                        Expr.Match { Pattern: Pattern.Variable scale, Value: Expr.Call { Module: not null, Arguments: [] } getScale },
                        Expr.Match
                        {
                            Pattern: Pattern.Variable frame, Value: Expr.Call
                            {
                                Module: not null, Arguments: [
                            Expr.Variable ownerFrame, var windowId, Expr.List { Tail: null, Items: [Expr.Variable caption] },
                            Expr.List
                                {
                                    Tail: null, Items: [var style, Expr.Tuple
                                    {
                                        Items: [
                                Expr.Literal sizeTag, Expr.Tuple { Items: [Expr.Binary width, Expr.Binary height] }]
                                    }]
                                }]
                            } create
                        },
                        Expr.Match { Pattern: Pattern.Variable menu, Value: Expr.Call { Module: not null, Arguments: [] } createMenu },
                        Expr.Call { Module: null } fillMenu, Expr.Call setMenu,
                        Expr.Match
                        {
                            Pattern: Pattern.Variable panel, Value: Expr.Call
                            {
                                Module: not null, Arguments: [
                            Expr.Variable panelFrame, var panelOptions]
                            } createPanel
                        },
                        Expr.Match { Pattern: Pattern.Variable sizer, Value: Expr.Call { Module: not null, Arguments: [var orientation] } createSizer },
                        Expr.Match
                        {
                            Pattern: Pattern.Tuple { Items: [Pattern.Variable infoPanel, Pattern.Variable pages] },
                            Value: Expr.Call
                            {
                                Module: null, Arguments: [Expr.Variable pagePanel,
                                Expr.Call
                                {
                                    DynamicRemote: true, Arguments: [
                                    Expr.Variable module, Expr.Literal { Value: Atom member }, Expr.List { Tail: null, Items.Count: 0 }]
                                } pageSpecs,
                                Expr.List { Tail: null, Items: [Expr.Variable pageDetails] }]
                            } createPages
                        },
                        Expr.Call { Arguments: [Expr.Variable firstSizer, Expr.Variable page, var pageOptions] } addPage,
                        Expr.Case
                        {
                            Value: Expr.Variable warningInput, Clauses: [
                            { Guard: null, Patterns: [var nil], Body: Expr.Literal },
                            {
                                Guard: null, Patterns: [Pattern.Variable { Name: VariableScopeNames.Wildcard }],
                                Body: Expr.Sequence
                                {
                                    Items: [
                                    Expr.Match { Pattern: Pattern.Variable bar, Value: Expr.Call { Module: not null } createBar },
                                    Expr.Call { Arguments: [Expr.Variable secondSizer, Expr.Variable statusBar, var statusOptions] } addBar,
                                    Expr.Call write, Expr.Variable returnedBar]
                                }
                            }]
                        },
                        Expr.Call setSizer,
                        Expr.Call { Arguments: [Expr.Variable closeFrame, var closeEvent] } connectClose,
                        Expr.Call { Arguments: [Expr.Variable menuFrame, var menuEvent] } connectMenu,
                        Expr.Call show, Expr.Call { Module: null } dismiss,
                        Expr.Tuple { Items: [Expr.Variable returnedFrame, Expr.RecordConstruction { Wildcard: null } state] }]
                    }
                }
            ] || !DistinctBindings(
                id,
                parentFrame,
                callback,
                app,
                parent,
                title,
                details,
                warning,
                scale,
                frame,
                menu,
                panel,
                sizer,
                infoPanel,
                pages,
                bar
            )
                || ownerFrame.Name != parentFrame.Name || caption.Name != title.Name || panelFrame.Name != frame.Name
                || pagePanel.Name != panel.Name || module.Name != callback.Name || pageDetails.Name != details.Name
                || firstSizer.Name != sizer.Name || page.Name != infoPanel.Name || warningInput.Name != warning.Name
                || !IsEmptyListPattern(nil) || secondSizer.Name != sizer.Name || statusBar.Name != bar.Name || returnedBar.Name != bar.Name
                || closeFrame.Name != frame.Name || menuFrame.Name != frame.Name || returnedFrame.Name != frame.Name
                || !MatchesDetailArguments(fillMenu, menu) || !MatchesDetailArguments(dismiss, app)
                || !IsWindowCall(setMenu, frame, menu) || !IsWindowCall(createBar, panel) || !IsWindowCall(write, bar, warning)
                || !IsWindowCall(setSizer, panel, sizer) || !IsWindowCall(show, frame)
                || !IsStaticWindowCall(connectClose) || !IsStaticWindowCall(connectMenu) || !IsStaticWindowCall(addPage)
                || !IsStaticWindowCall(addBar) || !IsStaticWindowCall(getScale) || !IsStaticWindowCall(create)
                || !IsStaticWindowCall(createMenu) || !IsStaticWindowCall(createPanel) || !IsStaticWindowCall(createSizer)
                || !TryWindowScale(width, scale.Name, out int widthFactor) || !TryWindowScale(height, scale.Name, out int heightFactor)
                || !AttributeLiteral.TryNormalizeConstant(windowId, out Term idValue)
                || !AttributeLiteral.TryNormalizeConstant(style, out Term styleValue)
                || !AttributeLiteral.TryNormalizeConstant(panelOptions, out Term panelValue)
                || !AttributeLiteral.TryNormalizeConstant(orientation, out Term orientationValue)
                || !AttributeLiteral.TryNormalizeConstant(pageOptions, out Term pageValue)
                || !AttributeLiteral.TryNormalizeConstant(statusOptions, out Term statusValue)
                || !AttributeLiteral.TryNormalizeConstant(closeEvent, out Term closeValue)
                || !AttributeLiteral.TryNormalizeConstant(menuEvent, out Term menuValue)
                || RecordExpressions.Create(state).Items is not [Expr.Literal stateTag, Expr.Variable stateParent,
                    Expr.Variable stateFrame, Expr.Variable stateId, Expr.Variable statePages]
                || stateParent.Name != parent.Name || stateId.Name != id.Name || stateFrame.Name != frame.Name || statePages.Name != pages.Name
                || !entries.TryGetValue((fillMenu.Function, 1), out int fillEntry)
                || !entries.TryGetValue((createPages.Function, 3), out int pagesEntry)
                || !ReturnsTuple(
                    createPages,
                    2,
                    [],
                    exact: true
                )
                || !entries.TryGetValue((dismiss.Function, 1), out int dismissEntry))
            {
                return false;
            }

            int info = owner.nextLabel - 2;
            int noWarning = Label();
            owner.EmitTupleCheck(5, 3, info);
            owner.Op(Allocate, BeamWire.UnsignedTag, 9);
            owner.Operand(BeamWire.UnsignedTag, 6);
            ClearDetailSlots(0, 1, 2);
            for (int register = 5; register >= 0; register--)
            {
                owner.MoveRegister(
                    XRegisterTag,
                    register,
                    YRegisterTag,
                    8 - register
                );
            }
            var previous = location;
            EmitWindowCall(getScale, ref previous);
            owner.EmitArithmeticLine(width.SourceLine, ref previous);
            owner.GcArithmetic(
                GcBifTwo,
                ErlangOperators.Multiply,
                1,
                1,
                (XRegisterTag, 0, 0),
                (BeamWire.IntegerTag, widthFactor, 0)
            );
            owner.EmitArithmeticLine(height.SourceLine, ref previous);
            owner.GcArithmetic(
                GcBifTwo,
                ErlangOperators.Multiply,
                2,
                0,
                (XRegisterTag, 0, NumberTypeBits),
                (BeamWire.IntegerTag, heightFactor, 0)
            );
            owner.Op(TestHeap, BeamWire.UnsignedTag, 12);
            owner.Operand(BeamWire.UnsignedTag, 2);
            ReadWindowField(0, XRegisterTag, 2);
            EmitWindowSingleton(XRegisterTag, 2, 2);
            owner.EmitTagMatchTuple(0, (XRegisterTag, 1), (XRegisterTag, 0));
            owner.EmitTupleHeader(XRegisterTag, 2);
            owner.LiteralOperand(sizeTag.Value);
            owner.Operand(XRegisterTag, 0);
            EmitWindowSingleton(XRegisterTag, 0, 0);
            owner.instructions.WriteByte(PutList);
            owner.LiteralOperand(styleValue);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(XRegisterTag, 3);
            owner.EmitConstant(idValue, 1);
            Load(7);
            owner.InitStackSlot(7);
            EmitWindowCall(create, ref previous);
            SaveWindowResult(7);
            EmitWindowCall(createMenu, ref previous);
            SaveWindowResult(2);
            EmitWindowLocal(fillMenu, fillEntry, ref previous);
            Load(2, 1);
            owner.InitStackSlot(2);
            Load(7);
            EmitWindowCall(setMenu, ref previous);
            owner.EmitConstant(panelValue, 1);
            Load(7);
            EmitWindowCall(createPanel, ref previous);
            SaveWindowResult(2);
            owner.EmitConstant(orientationValue, 0);
            EmitWindowCall(createSizer, ref previous);
            SaveWindowResult(1);
            owner.EmitConstant(member, 1);
            Load(6);
            owner.InitStackSlot(6);
            owner.EmitCallLine(pageSpecs, previous);
            owner.Op(DynamicApply, BeamWire.UnsignedTag, 0);
            previous = CallLocation(pageSpecs, previous);
            owner.Op(TestHeap, BeamWire.UnsignedTag, ConsHeapWords);
            owner.Operand(BeamWire.UnsignedTag, 1);
            ReadWindowField(1, XRegisterTag, 1);
            EmitWindowSingleton(XRegisterTag, 1, 2);
            owner.MoveRegister(
                XRegisterTag,
                0,
                XRegisterTag,
                1
            );
            Load(2);
            EmitWindowLocal(createPages, pagesEntry, ref previous);
            owner.EmitTuplePart(0, 0, 1);
            GetTupleField(
                0,
                1,
                YRegisterTag,
                6
            );
            owner.EmitConstant(pageValue, 2);
            Load(1);
            EmitWindowCall(addPage, ref previous);
            ReadWindowField(2, YRegisterTag, 3);
            owner.Op(ExactNotEqualTest, LabelTag, noWarning);
            owner.Operand(YRegisterTag, 3);
            owner.LiteralOperand(Nil.Value);
            Load(2);
            EmitWindowCall(createBar, ref previous);
            SaveWindowResult(0);
            Load(1, 1);
            owner.EmitConstant(statusValue, 2);
            owner.Op(Swap, XRegisterTag, 0);
            owner.Operand(XRegisterTag, 1);
            EmitWindowCall(addBar, ref previous);
            Load(3, 1);
            Load(0);
            ClearDetailSlots(0, 3);
            EmitWindowCall(write, ref previous);
            Mark(noWarning);
            Load(1, 1);
            Load(2);
            TrimDetailFrame(4, 5);
            EmitWindowCall(setSizer, ref previous);
            owner.EmitConstant(closeValue, 1);
            Load(3);
            EmitWindowCall(connectClose, ref previous);
            owner.EmitConstant(menuValue, 1);
            Load(3);
            EmitWindowCall(connectMenu, ref previous);
            Load(3);
            EmitWindowCall(show, ref previous);
            Load(1);
            owner.MoveRegister(
                YRegisterTag,
                0,
                YRegisterTag,
                1
            );
            TrimDetailFrame(1, 4);
            EmitWindowLocal(dismiss, dismissEntry, ref previous);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 9);
            owner.Operand(BeamWire.UnsignedTag, 0);
            owner.EmitTupleHeader(XRegisterTag, 5);
            owner.LiteralOperand(stateTag.Value);
            owner.Operand(YRegisterTag, 0);
            owner.Operand(YRegisterTag, 2);
            owner.Operand(YRegisterTag, 3);
            owner.Operand(YRegisterTag, 1);
            owner.EmitTagMatchTuple(0, (YRegisterTag, 2), (XRegisterTag, 0));
            owner.EmitFrameReturn(4);

            return true;
        }

        private static bool IsStaticWindowCall(Expr.Call call) => call is { Module: not null, DynamicRemote: false };

        private static bool IsWindowCall(Expr.Call call, params Pattern.Variable[] arguments) =>
            IsStaticWindowCall(call) && MatchesDetailArguments(call, arguments);

        private static bool TryWindowScale(Expr.Binary expression, string scale, out int value)
        {
            value = 0;
            if (expression is not
                {
                    Operator: ErlangOperators.Multiply, Left: Expr.Variable input,
                    Right: Expr.Literal { Value: Integer factor }
                } || input.Name != scale
                || factor.Value < int.MinValue || factor.Value > int.MaxValue)
            {
                return false;
            }
            value = (int)factor.Value;

            return true;
        }

        private void ReadWindowField(int field, int tag, int register)
        {
            owner.Op(GetTupleElement, YRegisterTag, 3);
            owner.Operand(BeamWire.UnsignedTag, field);
            owner.Operand(tag, register);
        }

        private void EmitWindowSingleton(int tag, int register, int destination)
        {
            owner.Op(PutList, tag, register);
            owner.LiteralOperand(Nil.Value);
            owner.Operand(XRegisterTag, destination);
        }

        private void SaveWindowResult(int slot) => owner.MoveRegister(
            XRegisterTag,
            0,
            YRegisterTag,
            slot
        );

        private void EmitWindowCall(Expr.Call call, ref BeamSourceLocation previous)
        {
            owner.EmitCallLine(call, previous);
            owner.EmitStaticRemoteCall(call);
            previous = CallLocation(call, previous);
        }

        private void EmitWindowLocal(Expr.Call call, int target, ref BeamSourceLocation previous)
        {
            owner.EmitCallLine(call, previous);
            owner.EmitFieldCall(call.Arguments.Count, target);
            previous = CallLocation(call, previous);
        }
    }
}
