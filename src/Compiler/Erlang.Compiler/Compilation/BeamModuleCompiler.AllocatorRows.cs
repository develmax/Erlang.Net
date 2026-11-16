using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed record PairConsMapping(ComprehensionQualifier.Generator Generator) : MappingPlan;
    private sealed record AllocatorRows(
        Expr.Call Columns,
        Expr.Call Recur,
        Expr.Call Length,
        Expr.Call Duplicate,
        PairConsMapping Mapping
    );

    private static AllocatorRows? ReadAllocatorRows(IReadOnlyList<Clause> clauses)
    {
        if (clauses is not [var complete, var inferred, var skip, var empty, var end]
            || complete is not
            {
                Guard: null, Patterns: [Pattern.List
                {
                    Items: [Pattern.Tuple
                    {
                        Items: [
                Pattern.Variable title, Pattern.Variable columns, Pattern.Variable data]
                    }], Tail: Pattern.Variable rest
                }],
                Body: Expr.List
                {
                    Items: [Expr.Tuple
                    {
                        Items: [Expr.Variable label,
                    Expr.Call { Module: null, Arguments: [Expr.Variable columnInput] } describe,
                    Expr.ListComprehension
                        {
                            Items: [Expr.List { Items: [Expr.Variable keyOutput], Tail: Expr.Variable valueOutput }],
                            Qualifiers: [ComprehensionQualifier.Generator
                            {
                                Strict: false, Source: Expr.Variable rowInput,
                                Pattern: Pattern.Tuple { Items: [Pattern.Variable key, Pattern.Variable value] }
                            } generator]
                        }]
                    }],
                    Tail: Expr.Call { Module: null, Arguments: [Expr.Variable remaining] } recur
                }
            }
            || !DistinctBindings(
                title,
                columns,
                data,
                rest,
                key,
                value
            )
            || label.Name != title.Name || columnInput.Name != columns.Name || rowInput.Name != data.Name
            || keyOutput.Name != key.Name || valueOutput.Name != value.Name || remaining.Name != rest.Name
            || inferred is not
            {
                Guard: null, Patterns: [Pattern.List
                {
                    Items: [Pattern.Tuple
                    {
                        Items: [
                Pattern.Variable inferredTitle, AliasPattern
                        {
                            Left: Pattern.List
                            {
                                Items: [Pattern.Tuple
                                {
                                    Items: [
                    Pattern.Variable { Name: VariableScopeNames.Wildcard }, Pattern.Variable sample]
                                }],
                                Tail: Pattern.Variable { Name: VariableScopeNames.Wildcard }
                            }, Right: Pattern.Variable inferredRows
                        }]
                    }],
                    Tail: Pattern.Variable inferredRest
                }],
                Body: Expr.Call
                {
                    Module: null, Arguments: [Expr.List
                    {
                        Items: [Expr.Tuple
                        {
                            Items: [
                    Expr.Variable inferredLabel, Expr.Call
                            {
                                Module: "lists", Function: "duplicate", DynamicRemote: false,
                                Arguments: [Expr.Call
                                {
                                    Module: null or ErlangModuleNames.Module, Function: "length",
                                    DynamicRemote: false, Arguments: [Expr.Variable measured]
                                } length, var seed]
                            } duplicate,
                    Expr.Variable inferredData]
                        }], Tail: Expr.Variable inferredTail
                    }]
                } retry
            }
            || !DistinctBindings(
                inferredTitle,
                inferredRows,
                sample,
                inferredRest
            )
            || inferredLabel.Name != inferredTitle.Name || inferredData.Name != inferredRows.Name
            || measured.Name != sample.Name || inferredTail.Name != inferredRest.Name || retry.Function != recur.Function
            || !IsNilExpression(seed)
            || skip is not
            {
                Guard: null, Patterns: [Pattern.List
                {
                    Items: [Pattern.Tuple { Items: [var blank, var noRows] }],
                    Tail: Pattern.Variable skippedRest
                }], Body: Expr.Call { Module: null, Arguments: [Expr.Variable skipped] } skipCall
            }
            || !IsEmptyListPattern(blank) || !IsEmptyListPattern(noRows) || !DistinctBindings(skippedRest)
            || skipped.Name != skippedRest.Name || skipCall.Function != recur.Function
            || empty is not
            {
                Guard: null, Patterns: [Pattern.List
                {
                    Items: [Pattern.Tuple
                    {
                        Items: [
                Pattern.Variable emptyTitle, AliasPattern { Left: var noData, Right: Pattern.Variable emptyData }]
                    }],
                    Tail: Pattern.Variable emptyRest
                }],
                Body: Expr.Call
                {
                    Module: null, Arguments: [Expr.List
                    {
                        Items: [Expr.Tuple
                        {
                            Items: [
                    Expr.Variable emptyLabel, var noColumns, Expr.Variable emptyRows]
                        }], Tail: Expr.Variable emptyTail
                    }]
                } emptyCall
            }
            || !IsEmptyListPattern(noData) || !IsNilExpression(noColumns) || !DistinctBindings(emptyTitle, emptyData, emptyRest)
            || emptyLabel.Name != emptyTitle.Name || emptyRows.Name != emptyData.Name || emptyTail.Name != emptyRest.Name
            || emptyCall.Function != recur.Function
            || end is not { Guard: null, Patterns: [var nil], Body: var result } || !IsEmptyListPattern(nil) || !IsNilExpression(result))
        {
            return null;
        }

        return new(
            describe,
            recur,
            length,
            duplicate,
            new(generator)
        );
    }

    private static bool IsNilExpression(Expr expression) =>
        AttributeLiteral.TryNormalizeConstant(expression, out Term literal) && literal is Nil;

    private static BeamSourceLocation PairConsLocation(PairConsMapping flow, BeamSourceLocation location) =>
        flow.Generator.GeneratorLine == 0 ? location : new(flow.Generator.GeneratorFile, flow.Generator.GeneratorLine);

    private void EmitAllocatorRows(AllocatorRows flow, BeamSourceLocation location,
        IReadOnlyDictionary<(string Name, int Arity), int> entries)
    {
        if (flow.Recur.Function != activeFunction || !ResolvesErlangBif(flow.Length)
            || !entries.TryGetValue((flow.Columns.Function, 1), out int columns)
            || !entries.TryGetValue((activeFunction, 1), out int recur)
            || !entries.TryGetValue((MappingName(activeFunction, 1), 1), out int mapping))
        {
            throw new NotSupportedException(Unsupported);
        }

        int info = nextLabel - 2;
        int complete = nextLabel++;
        int inferred = nextLabel++;
        int blank = nextLabel++;
        int empty = nextLabel++;
        int end = nextLabel++;
        EmitNonemptyList(0, end);
        EmitListParts(
            0,
            XRegisterTag,
            1,
            XRegisterTag,
            2
        );
        Op(IsTuple, LabelTag, info);
        Operand(XRegisterTag, 1);
        instructions.WriteByte(SelectTupleArity);
        EmitTypedRegister(1, TupleTypeBits);
        Operand(LabelTag, info);
        instructions.WriteByte(ExtendedListOperand);
        Operand(BeamWire.UnsignedTag, 4);
        Operand(BeamWire.UnsignedTag, 2);
        Operand(LabelTag, inferred);
        Operand(BeamWire.UnsignedTag, 3);
        Operand(LabelTag, complete);

        Op(Label, BeamWire.UnsignedTag, complete);
        Op(Allocate, BeamWire.UnsignedTag, 3);
        Operand(BeamWire.UnsignedTag, 3);
        InitStackSlot(0);
        MoveRegister(
            XRegisterTag,
            1,
            YRegisterTag,
            1
        );
        MoveRegister(
            XRegisterTag,
            2,
            YRegisterTag,
            2
        );
        EmitTuplePart(1, 1, 0);
        EmitCallLine(flow.Columns, location);
        EmitFieldCall(1, columns);
        Op(GetTupleElement, YRegisterTag, 1);
        Operand(BeamWire.UnsignedTag, 2);
        Operand(XRegisterTag, 1);
        MoveRegister(
            XRegisterTag,
            0,
            YRegisterTag,
            0
        );
        MoveRegister(
            XRegisterTag,
            1,
            XRegisterTag,
            0
        );
        var generator = PairConsLocation(flow.Mapping, CallLocation(flow.Columns, location));
        EmitLine(generator);
        EmitFieldCall(1, mapping);
        Op(Swap, YRegisterTag, 2);
        Operand(XRegisterTag, 0);
        EmitCallLine(flow.Recur, generator);
        EmitFieldCall(1, recur);
        EmitAllocatorHeap(1);
        Op(GetTupleElement, YRegisterTag, 1);
        Operand(BeamWire.UnsignedTag, 0);
        Operand(XRegisterTag, 1);
        EmitTagMatchTuple(
            1,
            (XRegisterTag, 1),
            (YRegisterTag, 0),
            (YRegisterTag, 2)
        );
        Op(PutList, XRegisterTag, 1);
        Operand(XRegisterTag, 0);
        Operand(XRegisterTag, 0);
        EmitFrameReturn(3);

        Op(Label, BeamWire.UnsignedTag, inferred);
        EmitTuplePart(1, 0, 3);
        EmitTuplePart(1, 1, 1);
        EmitNonemptyList(1, blank);
        Op(GetHead, XRegisterTag, 1);
        Operand(XRegisterTag, 4);
        EmitTupleCheck(4, 2, blank);
        EmitTuplePart(4, 1, 0);
        EmitCallLine(flow.Length, location);
        GcArithmetic(
            GcBifOne,
            flow.Length.Function,
            4,
            0,
            (XRegisterTag, 0, 0)
        );
        Op(Allocate, BeamWire.UnsignedTag, 3);
        Operand(BeamWire.UnsignedTag, 4);
        MoveRegister(
            XRegisterTag,
            3,
            YRegisterTag,
            0
        );
        MoveRegister(
            XRegisterTag,
            1,
            YRegisterTag,
            1
        );
        MoveRegister(
            XRegisterTag,
            2,
            YRegisterTag,
            2
        );
        EmitConstant(Nil.Value, 1);
        EmitCallLine(flow.Duplicate, CallLocation(flow.Length, location));
        EmitStaticRemoteCall(flow.Duplicate);
        EmitAllocatorHeap(1);
        EmitTagMatchTuple(
            0,
            (YRegisterTag, 0),
            (XRegisterTag, 0),
            (YRegisterTag, 1)
        );
        Op(PutList, XRegisterTag, 0);
        Operand(YRegisterTag, 2);
        Operand(XRegisterTag, 0);
        Op(TailLocalCallWithFrame, BeamWire.UnsignedTag, 1);
        Operand(LabelTag, recur);
        Operand(BeamWire.UnsignedTag, 3);

        Op(Label, BeamWire.UnsignedTag, blank);
        EmitNilTest(XRegisterTag, 3, empty);
        EmitNilTest(XRegisterTag, 1, info);
        MoveRegister(
            XRegisterTag,
            2,
            XRegisterTag,
            0
        );
        Op(TailLocalCall, BeamWire.UnsignedTag, 1);
        Operand(LabelTag, recur);
        Op(Label, BeamWire.UnsignedTag, empty);
        EmitNilTest(XRegisterTag, 1, info);
        EmitAllocatorHeap(4);
        EmitTupleHeader(XRegisterTag, 3);
        Operand(XRegisterTag, 3);
        LiteralOperand(Nil.Value);
        LiteralOperand(Nil.Value);
        Op(PutList, XRegisterTag, 0);
        Operand(XRegisterTag, 2);
        Operand(XRegisterTag, 0);
        Op(TailLocalCall, BeamWire.UnsignedTag, 1);
        Operand(LabelTag, recur);
        Op(Label, BeamWire.UnsignedTag, end);
        EmitNilTest(XRegisterTag, 0, info);
        instructions.WriteByte(Return);
    }

    private void EmitAllocatorHeap(int live)
    {
        Op(TestHeap, BeamWire.UnsignedTag, 4 + ConsHeapWords);
        Operand(BeamWire.UnsignedTag, live);
    }

    private void EmitPairConsHelper(
        string name,
        PairConsMapping flow,
        BeamSourceLocation location,
        int entry
    )
    {
        StartFunction(
            name,
            1,
            PairConsLocation(flow, location),
            exported: false
        );
        int skip = nextLabel++;
        int empty = nextLabel++;
        int invalid = nextLabel++;
        EmitNonemptyList(0, empty);
        EmitListParts(
            0,
            XRegisterTag,
            1,
            XRegisterTag,
            0
        );
        EmitTupleCheck(1, 2, skip);
        Op(Allocate, BeamWire.UnsignedTag, 1);
        Operand(BeamWire.UnsignedTag, 2);
        MoveRegister(
            XRegisterTag,
            1,
            YRegisterTag,
            0
        );
        EmitFieldCall(1, entry);
        Op(TestHeap, BeamWire.UnsignedTag, 2 * ConsHeapWords);
        Operand(BeamWire.UnsignedTag, 1);
        Op(GetTupleElement, YRegisterTag, 0);
        Operand(BeamWire.UnsignedTag, 0);
        Operand(XRegisterTag, 1);
        Op(GetTupleElement, YRegisterTag, 0);
        Operand(BeamWire.UnsignedTag, 1);
        Operand(XRegisterTag, 2);
        Op(PutList, XRegisterTag, 1);
        Operand(XRegisterTag, 2);
        Operand(XRegisterTag, 1);
        Op(PutList, XRegisterTag, 1);
        Operand(XRegisterTag, 0);
        Operand(XRegisterTag, 0);
        EmitFrameReturn(1);
        Op(Label, BeamWire.UnsignedTag, skip);
        Op(TailLocalCall, BeamWire.UnsignedTag, 1);
        Operand(LabelTag, entry);
        EmitMappingEnd(empty, invalid);
    }
}
