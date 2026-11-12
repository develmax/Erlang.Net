using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitPageRecord(FunctionDefinition function)
        {
            if (function.Clauses is not [
                {
                    Guard: null, Patterns: [Pattern.Variable parent, Pattern.Variable text, Pattern.Variable table, Pattern.Variable app],
                    Body: Expr.Sequence
                    {
                        Items: [
                    Expr.Call close, Expr.Call busy,
                    Expr.Match { Pattern: Pattern.Variable panel, Value: Expr.Call create },
                    Expr.Call setPage, Expr.Call idle,
                    Expr.Tuple { Items: [Expr.Variable result, Expr.RecordConstruction { Wildcard: null } record] }
                ]
                    }
                }] || !DistinctBindings(
                    parent,
                    text,
                    table,
                    app,
                    panel
                ) || panel.Name != result.Name
                || !IsWindowCall(close) || !IsWindowCall(busy) || !IsWindowCall(create, parent)
                || !IsWindowCall(setPage, panel, text) || !IsWindowCall(idle))
            {
                return false;
            }

            var slots = new Dictionary<string, int>(StringComparer.Ordinal)
            {
                [parent.Name] = 3,
                [panel.Name] = 2,
                [table.Name] = 1,
                [app.Name] = 0
            };
            var fields = new List<(Term? Literal, int Slot)>();
            foreach (Expr member in RecordExpressions.Create(record).Items)
            {
                if (member is Expr.Variable variable && slots.TryGetValue(variable.Name, out int slot))
                {
                    fields.Add((null, slot));
                }
                else if (AttributeLiteral.TryNormalizeConstant(member, out Term literal))
                {
                    fields.Add((literal, 0));
                }
                else
                {
                    return false;
                }
            }

            owner.Op(Allocate, BeamWire.UnsignedTag, 4);
            owner.Operand(BeamWire.UnsignedTag, 4);
            for (int register = 3; register >= 0; register--)
            {
                owner.MoveRegister(
                    XRegisterTag,
                    register,
                    YRegisterTag,
                    3 - register
                );
            }

            var previous = location;
            EmitWindowCall(close, ref previous);
            EmitWindowCall(busy, ref previous);
            Load(3);
            EmitWindowCall(create, ref previous);
            Load(2, 1);
            SaveWindowResult(2);
            EmitWindowCall(setPage, ref previous);
            EmitWindowCall(idle, ref previous);
            owner.Op(TestHeap, BeamWire.UnsignedTag, fields.Count + 4);
            owner.Operand(BeamWire.UnsignedTag, 0);
            owner.EmitTupleHeader(XRegisterTag, fields.Count);
            foreach (var (literal, slot) in fields)
            {
                if (literal is null)
                {
                    owner.Operand(YRegisterTag, slot);
                }
                else
                {
                    owner.LiteralOperand(literal);
                }
            }

            owner.EmitTagMatchTuple(0, (YRegisterTag, 2), (XRegisterTag, 0));
            owner.EmitFrameReturn(4);

            return true;
        }

        private bool TryEmitTupleFallbackRecordUpdate(Clause clause, int argument, Pattern.Tuple tuple)
        {
            Expr body = clause.Body;
            if (body is Expr.Sequence
                {
                    Items:
                    [
                        Expr.Match { Pattern: Pattern.Variable savedRecord } selection,
                        Expr.Match { Pattern: Pattern.Variable updatedRecord, Value: Expr.RecordUpdate updateValue },
                        Expr.Call { Arguments: [var tailKey, Expr.Variable forwarded] } tailCall
                    ]
                }
                && DistinctBindings(savedRecord, updatedRecord)
                && updatedRecord.Name == forwarded.Name
                && !clause.Patterns.SelectMany(Semantics.Variables).Contains(updatedRecord.Name, StringComparer.Ordinal))
            {
                body = new Expr.Sequence([selection, tailCall with { Arguments = [tailKey, updateValue] }]);
            }

            if (body is not Expr.Sequence { Items:
                [
                    Expr.Match
                    {
                        Pattern: Pattern.Variable saved,
                        Value: Expr.Case
                        {
                            Value: Expr.Call read,
                            Clauses:
                            [
                                { Patterns: [Pattern.Variable record], Guard: { } guard, Body: Expr.Variable returned },
                                { Patterns: [Pattern.Variable { Name: VariableScopeNames.Wildcard }], Guard: null, Body: var fallback }
                            ]
                        }
                    },
                    Expr.Call
                    {
                        Module: null, DynamicRemote: false,
                        Arguments: [var key, Expr.RecordUpdate
                        {
                            Value: Expr.Variable original,
                            Fields: [var field]
                        } update]
                    } tail
                ]
            }
                || !DistinctBindings(saved, record)
                || record.Name != returned.Name || saved.Name != original.Name
                || clause.Patterns.SelectMany(Semantics.Variables).Contains(saved.Name, StringComparer.Ordinal)
                || clause.Patterns.SelectMany(Semantics.Variables).Contains(record.Name, StringComparer.Ordinal)
                || owner.ReadRecordTest(guard) is not { Value: Expr.Variable checkedValue } test
                || checkedValue.Name != record.Name || test.Name.Name != update.Name || test.Arity != update.Arity
                || !AttributeLiteral.TryNormalizeConstant(fallback, out Term defaultValue)
                || defaultValue is not TupleTerm defaultTuple || defaultTuple.Items.Count != update.Arity
                || !test.Name.Equals(defaultTuple.Items[0])
                || !AttributeLiteral.TryNormalizeConstant(key, out Term constantKey)
                || field.Value is not Expr.Variable replacement
                || ReadEntryCall(read, []) is not { } initial
                || !entries.TryGetValue((tail.Function, 2), out int target))
            {
                return false;
            }

            int index = Array.FindIndex(tuple.Items.ToArray(),
                pattern => pattern is Pattern.Variable variable && variable.Name == replacement.Name);
            if (index < 0)
            {
                return false;
            }

            EmitFrameAllocation(clause.Patterns.Count, 1);
            owner.MoveRegister(XRegisterTag, argument, YRegisterTag, 0);
            EmitEntryCall(initial);
            int useDefault = Label();
            int updateRecord = Label();
            EmitDetailRecordTest(0, test.Arity, test.Name, useDefault);
            JumpTo(updateRecord);
            Mark(useDefault);
            owner.EmitConstant(defaultTuple, 0);
            Mark(updateRecord);
            owner.Op(TestHeap, BeamWire.UnsignedTag, update.Arity + 1);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.Op(GetTupleElement, YRegisterTag, 0);
            owner.Operand(BeamWire.UnsignedTag, index);
            owner.Operand(XRegisterTag, 1);
            owner.Op(UpdateRecord, AtomTag, owner.Intern("reuse"));
            owner.Operand(BeamWire.UnsignedTag, update.Arity);
            owner.EmitTypedRegister(0, TupleTypeBits);
            owner.Operand(XRegisterTag, 1);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.Operand(BeamWire.UnsignedTag, field.Index + 1);
            owner.Operand(XRegisterTag, 1);
            owner.EmitConstant(constantKey, 0);
            owner.Op(TailLocalCallWithFrame, BeamWire.UnsignedTag, 2);
            owner.Operand(LabelTag, target);
            owner.Operand(BeamWire.UnsignedTag, 1);
            return true;
        }

        private bool TryEmitReturnedRecordUpdate(FunctionDefinition function)
        {
            if (function.Clauses is not [
                {
                    Guard: null, Patterns: [Pattern.Variable input, Pattern.Variable replacement],
                    Body: Expr.Sequence
                    {
                        Items: [
                    Expr.Match
                        {
                            Pattern: Pattern.Tuple { Items: [Pattern.Variable first, Pattern.Variable record] },
                            Value: Expr.Call
                            {
                                Module: null, DynamicRemote: false, Arguments: [
                            Expr.Variable forwarded, var second, var third, var fourth
                        ]
                            } call
                        },
                    Expr.Tuple
                        {
                            Items: [Expr.Variable returned, Expr.RecordUpdate
                            {
                                Value: Expr.Variable original, Fields: [var field]
                            } update]
                        }
                ]
                    }
                }] || !DistinctBindings(
                    input,
                    replacement,
                    first,
                    record
                )
                || forwarded.Name != input.Name || returned.Name != first.Name || original.Name != record.Name
                || field.Value is not Expr.Variable value || value.Name != replacement.Name
                || !AttributeLiteral.TryNormalizeConstant(second, out Term secondValue)
                || !AttributeLiteral.TryNormalizeConstant(third, out Term thirdValue)
                || !AttributeLiteral.TryNormalizeConstant(fourth, out Term fourthValue)
                || !entries.TryGetValue((call.Function, 4), out int target)
                || !localFunctions.TryGetValue((call.Function, 4), out var producer)
                || producer.Clauses is not [{ Body: var producerBody }]
                || LastExpression(producerBody) is not Expr.Tuple { Items: [_, Expr.RecordConstruction produced] }
                || produced.Definition.Name != update.Name || produced.Definition.Fields.Count + 1 != update.Arity
                || !HasOnlyLocalCalls(
                    function,
                    2,
                    (caller, invocation) => caller.Clauses.Any(clause =>
                    ReferenceEquals(clause.Body, invocation)
                    && invocation.Arguments[1] is Expr.Variable argument
                    && clause.Guard is Expr.Call
                    {
                        Function: AutoImportNames.IsAtom, DynamicRemote: false,
                        Arguments: [Expr.Variable guarded]
                    } guard
                    && guarded.Name == argument.Name && owner.ResolvesErlangBif(guard))
                ))
            {
                return false;
            }

            owner.Op(Allocate, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.MoveRegister(
                XRegisterTag,
                1,
                YRegisterTag,
                0
            );
            owner.EmitConstant(thirdValue, 2);
            owner.EmitConstant(fourthValue, 3);
            owner.EmitConstant(secondValue, 1);
            owner.EmitCallLine(call, location);
            owner.EmitFieldCall(4, target);
            owner.Op(TestHeap, BeamWire.UnsignedTag, update.Arity + 4);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.EmitTuplePart(0, 1, 1);
            owner.Op(UpdateRecord, AtomTag, owner.Intern("reuse"));
            owner.Operand(BeamWire.UnsignedTag, update.Arity);
            owner.EmitTypedRegister(1, TupleTypeBits);
            owner.Operand(XRegisterTag, 1);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.Operand(BeamWire.UnsignedTag, field.Index + 1);
            owner.EmitTypedRegister(0, AtomTypeBits, registerTag: YRegisterTag);
            owner.EmitTuplePart(0, 0, 0);
            owner.EmitTagMatchTuple(0, (XRegisterTag, 0), (XRegisterTag, 1));
            owner.EmitFrameReturn(1);

            return true;
        }
    }
}
