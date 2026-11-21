using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitRecordFieldListAndClosure(FunctionDefinition function)
        {
            if (owner.ModuleOptimizationDisabled || function.Clauses is not
                [
                {
                    Guard: null, Patterns: [Pattern.Variable record, AliasPattern alias],
                    Body: Expr.Sequence
                    {
                        Items:
                            [
                                Expr.Call before,
                                Expr.Call { Arguments: [var format, Expr.List { Items.Count: 2, Tail: null } members] } print,
                                Expr.Call { Arguments: [Expr.Fun { SelfName: null } callback, Expr.Variable member] } afterwards,
                                Expr.Tuple { Items: [Expr.Literal { Value: Atom tag }, Expr.Variable returned] }
                            ]
                    }
                }
                ]
                || !TryReadPatternAlias(alias, out var state, out var statePattern)
                || statePattern is not Pattern.Tuple tuple || !HasTupleTag(tuple, out Atom stateTag)
                || record.Name == VariableScopeNames.Wildcard || state.Name == VariableScopeNames.Wildcard
                || returned.Name != state.Name || !AttributeLiteral.TryNormalizeConstant(format, out _)
                || !TryResolveOrdinaryCall(afterwards, out string? module, preparedArgument: 0)
                || ReadEntryCall(before, []) is not { } prefix
                || ReadEntryCall(print, [], preparedOperand: 1) is not { } fieldsCall)
            {
                return false;
            }

            int field = -1;
            for (int index = 1; index < tuple.Items.Count; index++)
            {
                if (tuple.Items[index] is not Pattern.Variable binding)
                {
                    return false;
                }

                if (binding.Name == VariableScopeNames.Wildcard)
                {
                    continue;
                }

                if (field >= 0 || binding.Name != member.Name || binding.Name == record.Name || binding.Name == state.Name)
                {
                    return false;
                }

                field = index;
            }

            if (field < 0 || record.Name == state.Name
                || members.Items[0] is not Expr.RecordAccess first
                || members.Items.Any(item => item is not Expr.RecordAccess { Value: Expr.Variable source } projection
                    || source.Name != record.Name || projection.Name != first.Name || projection.Arity != first.Arity))
            {
                return false;
            }

            var bindings = new Dictionary<string, Term>(StringComparer.Ordinal)
            {
                [record.Name] = Nil.Value,
                [state.Name] = Nil.Value,
                [member.Name] = Nil.Value
            };
            var captures = ClosureCaptures.Capture(callback, bindings);
            if (captures.Count != 1 || !captures.ContainsKey(record.Name))
            {
                return false;
            }

            int functionInfo = owner.nextLabel - 2;
            int badRecord = Label();
            EmitDetailRecordTest(
                1,
                tuple.Items.Count,
                stateTag,
                functionInfo
            );
            EmitFrameAllocation(function.Arity, 2);
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
            EmitEntryCall(prefix);
            owner.EmitRecordTest(
                badRecord,
                YRegisterTag,
                1,
                Term.A(first.Name),
                first.Arity
            );
            owner.Op(TestHeap, BeamWire.UnsignedTag, members.Items.Count * ConsHeapWords);
            owner.Operand(BeamWire.UnsignedTag, 0);
            var fields = members.Items.Cast<Expr.RecordAccess>().ToArray();
            for (int index = 0; index < fields.Length; index++)
            {
                EmitSavedRecordField(1, fields[index].Index, index);
            }

            for (int index = fields.Length - 1; index >= 0; index--)
            {
                owner.Op(PutList, XRegisterTag, index);
                if (index == fields.Length - 1)
                {
                    owner.LiteralOperand(Nil.Value);
                }
                else
                {
                    owner.Operand(XRegisterTag, 1);
                }
                owner.Operand(XRegisterTag, 1);
            }

            EmitEntryCall(fieldsCall, CallLocation(before, location));
            var previous = variables;
            variables = new(StringComparer.Ordinal) { [record.Name] = 1 };
            EmitGeneralClosure(callback);
            variables = previous;
            EmitSavedRecordField(0, field, 1);
            owner.MoveRegister(
                YRegisterTag,
                0,
                YRegisterTag,
                1
            );
            owner.Op(TrimFrame, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.EmitCallLine(afterwards, CallLocation(print, location));
            Call(module, afterwards.Function, 2);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 3);
            owner.Operand(BeamWire.UnsignedTag, 0);
            owner.EmitTagMatchTuple(0, (AtomTag, owner.Intern(tag.Name)), (YRegisterTag, 0));
            owner.EmitFrameReturn(1);

            Mark(badRecord);
            owner.EmitLine(first.SourceLine == 0 ? CallLocation(print, location) : new(first.PreprocessorFile ?? location.File, first.SourceLine));
            owner.Op(BadRecord, YRegisterTag, 1);

            return true;
        }
    }
}
