using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitNestedTupleFieldCalls(Clause clause)
        {
            if (clause.Guard is not null || clause.Patterns is not [Pattern.Variable first, Pattern.Variable tail]
                || !HasUnconditionalParameters(clause) || clause.Body is not Expr.Sequence
                {
                    Items: [
                        Expr.Match { Pattern: Pattern.Variable forwarded, Value: Expr.Call producer },
                        Expr.Match
                        {
                            Pattern: Pattern.Tuple { Items: [Pattern.Tuple unused, Pattern.Tuple retained] },
                            Value: Expr.Call { Arguments: [Expr.Variable input] } selector
                        } match,
                        Expr.Tuple
                        {
                            Items: [
                                Expr.Binary { Operator: ErlangOperators.Append, Left: var prefix, Right: Expr.Variable suffix } append,
                                Expr.List { Tail: Expr.Variable listTail } list
                            ]
                        }
                    ]
                }
                || first.Name != suffix.Name || tail.Name != listTail.Name || forwarded.Name != input.Name
                || forwarded.Name == VariableScopeNames.Wildcard || first.Name == VariableScopeNames.Wildcard
                || tail.Name == VariableScopeNames.Wildcard || forwarded.Name == first.Name || forwarded.Name == tail.Name
                || !AttributeLiteral.TryNormalizeConstant(prefix, out Term prefixValue)
                || retained.Items.Count < 2 || retained.Items.Count != list.Items.Count
                || retained.Items.Any(item => item is not Pattern.Variable { Name: not VariableScopeNames.Wildcard })
                || unused.Items.Any(item => item is not Erlang.Pattern.Variable))
            {
                return false;
            }

            string[] parameters = [first.Name, tail.Name];
            if (Semantics.Variables(match.Pattern).Append(forwarded.Name).Concat(parameters)
                .GroupBy(name => name, StringComparer.Ordinal).Any(group => group.Count() > 1)
                || ReadEntryCall(selector, [forwarded.Name]) is not { } select)
            {
                return false;
            }

            var start = ReadEntryCall(producer, parameters);
            if (start is null && producer is
                { Module: ErlangModuleNames.Module, Function: ErlangClockNames.Timestamp, DynamicRemote: false, Arguments: [] })
            {
                start = new EntryCall(producer, ErlangModuleNames.Module, [], []);
            }

            if (start is null)
            {
                return false;
            }

            var calls = new EntryCall[list.Items.Count];
            for (int index = 0; index < list.Items.Count; index++)
            {
                string field = ((Pattern.Variable)retained.Items[index]).Name;
                if (list.Items[index] is not Expr.Call { Arguments: [Expr.Variable operand] } call
                    || operand.Name != field || ReadEntryCall(call, [field]) is not { } plan)
                {
                    return false;
                }

                calls[index] = plan;
            }

            int frame = calls.Length + 1;
            int resultSlot = frame - 1;
            int listSlot = frame - 2;
            int tupleSlot = frame - 3;
            EmitFrameAllocation(parameters.Length, frame);
            EmitFrameInitialization(0, listSlot);
            owner.MoveRegister(XRegisterTag, 1, YRegisterTag, listSlot);
            owner.MoveRegister(XRegisterTag, 0, YRegisterTag, resultSlot);
            EmitEntryCall(start);
            var previous = CallLocation(start.Call, location);
            EmitEntryCall(select, previous);
            previous = CallLocation(select.Call, previous);

            int failure = Label();
            EmitTupleShapeTest(XRegisterTag, 0, 2, failure);
            owner.EmitTuplePart(0, 0, 1);
            EmitTupleShapeTest(XRegisterTag, 1, unused.Items.Count, failure);
            owner.Op(GetTupleElement, XRegisterTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.Operand(YRegisterTag, tupleSlot);
            EmitTupleShapeTest(YRegisterTag, tupleSlot, retained.Items.Count, failure);

            owner.MoveRegister(YRegisterTag, resultSlot, XRegisterTag, 1);
            EmitFrameInitialization(resultSlot, frame);
            owner.EmitConstant(prefixValue, 0);
            var appendLocation = new BeamSourceLocation(location.File, append.SourceLine);
            if (append.SourceLine != 0 && appendLocation != previous)
            {
                owner.EmitLine(appendLocation);
                previous = appendLocation;
            }

            Call(ErlangModuleNames.Module, ErlangOperators.Append, 2);
            for (int index = 0; index < calls.Length; index++)
            {
                owner.EmitTuplePart(tupleSlot, index, 1, YRegisterTag);
                owner.MoveRegister(XRegisterTag, 0, YRegisterTag, index == 0 ? resultSlot : index - 1);
                owner.MoveRegister(XRegisterTag, 1, XRegisterTag, 0);
                EmitEntryCall(calls[index], previous);
                previous = CallLocation(calls[index].Call, previous);
            }

            const int resultArity = 2;
            owner.Op(TestHeap, BeamWire.UnsignedTag, resultArity + 1 + ConsHeapWords * calls.Length);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.Op(PutList, XRegisterTag, 0);
            owner.Operand(YRegisterTag, listSlot);
            owner.Operand(XRegisterTag, 0);
            for (int slot = tupleSlot; slot >= 0; slot--)
            {
                owner.Op(PutList, YRegisterTag, slot);
                owner.Operand(XRegisterTag, 0);
                owner.Operand(XRegisterTag, 0);
            }

            owner.EmitTupleHeader(XRegisterTag, resultArity);
            owner.Operand(YRegisterTag, resultSlot);
            owner.Operand(XRegisterTag, 0);
            owner.EmitFrameReturn(frame);

            Mark(failure);
            owner.EmitLine(match.SourceLine == 0 ? CallLocation(select.Call, location)
                : new BeamSourceLocation(match.PreprocessorFile, match.SourceLine));
            owner.Op(BadMatch, XRegisterTag, 0);

            return true;
        }
    }
}
