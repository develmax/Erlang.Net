using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record RecordEffectFallback(Expr.Call Call, Term Message, int? Field);

        private bool TryEmitRetainedRecordEffects(FunctionDefinition function)
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled
                || function.Clauses is not
                [
                {
                    Guard: null,
                    Patterns: [Pattern.Literal { Value: Atom command }, Pattern.Variable ignored, AliasPattern alias],
                    Body: Expr.Sequence { Items: [_, .., var result] } sequence
                },
                {
                    Guard: null, Patterns: [Pattern.Variable input, Pattern.Variable { }, Pattern.Variable fallbackState],
                    Body: var otherwise
                } fallback
                ] || !HasUnconditionalParameters(fallback)
                || !ReadEventState(alias, out var state, out var fields)
                || fields is not [Pattern.Literal { Value: Atom tag }, ..]
                || ReadReply(result, state.Name) is not { } reply)
            {
                return false;
            }

            var lexical = new HashSet<string>(StringComparer.Ordinal) { state.Name };
            if (state.Name == VariableScopeNames.Wildcard
                || ignored.Name != VariableScopeNames.Wildcard && !lexical.Add(ignored.Name))
            {
                return false;
            }

            var bindings = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int index = 1; index < fields.Count; index++)
            {
                if (fields[index] is not Pattern.Variable field)
                {
                    return false;
                }

                if (field.Name != VariableScopeNames.Wildcard)
                {
                    if (!lexical.Add(field.Name))
                    {
                        return false;
                    }

                    bindings.Add(field.Name, index);
                }
            }

            var calls = new List<(Expr.Call Call, int Field)>();
            foreach (Expr item in sequence.Items.SkipLast(1))
            {
                if (item is not Expr.Call { Module: not null, DynamicRemote: false, Arguments: [var argument] } effect
                    || !TryResolveOrdinaryCallTarget(effect, out _))
                {
                    return false;
                }

                int field = argument switch
                {
                    Expr.Variable variable when bindings.TryGetValue(variable.Name, out int index) => index,
                    Expr.RecordAccess { Value: Expr.Variable holder } access when holder.Name == state.Name
                        && access.Name == tag.Name && access.Arity == fields.Count => access.Index,
                    _ => -1
                };
                if (field < 0)
                {
                    return false;
                }

                calls.Add((effect, field));
            }

            int[] shared = calls.GroupBy(call => call.Field).Where(group => group.Count() > 1).Select(group => group.Key).ToArray();
            if (shared.Length > 1 || shared is [int projected] && projected != calls[0].Field)
            {
                return false;
            }

            RecordEffectFallback? report = null;
            var fallbackReply = otherwise;
            if (otherwise is Expr.Sequence { Items: [Expr.Call log, var returned] })
            {
                if (input.Name == VariableScopeNames.Wildcard || fallbackState.Name == VariableScopeNames.Wildcard
                    || log is not { Module: not null, DynamicRemote: false }
                    || !TryResolveOrdinaryCallTarget(log, out _))
                {
                    return false;
                }

                Expr text;
                Expr items;
                int? field;
                if (log.Arguments is [var message, var data])
                {
                    text = message;
                    items = data;
                    field = null;
                }
                else if (log.Arguments is
                    [Expr.RecordAccess { Value: Expr.Variable holder } config, var formatted, var dataItems]
                    && holder.Name == fallbackState.Name && config.Name == tag.Name && config.Arity == fields.Count)
                {
                    text = formatted;
                    items = dataItems;
                    field = config.Index;
                }
                else
                {
                    return false;
                }

                if (items is not Expr.List { Items: [Expr.Variable value], Tail: null }
                    || value.Name != input.Name || !AttributeLiteral.TryNormalizeConstant(text, out Term format))
                {
                    return false;
                }

                report = new(log, format, field);
                fallbackReply = returned;
            }

            if (ReadReply(fallbackReply, fallbackState.Name) is not { } resultPrefix)
            {
                return false;
            }

            int other = Label();
            int shapeFailure = report?.Field is null ? other : Label();
            const int stateRegister = 2;
            owner.EmitAtomTest(0, command.Name, other);
            EmitDetailRecordTest(
                stateRegister,
                fields.Count,
                tag,
                shapeFailure
            );
            int stateSlot = shared.Length;
            EmitFrameAllocation(function.Arity, stateSlot + 1);
            owner.MoveRegister(
                XRegisterTag,
                stateRegister,
                YRegisterTag,
                stateSlot
            );
            int savedProjection = -1;
            int lastUse = -1;
            if (shared is [int used])
            {
                savedProjection = used;
                GetTupleField(
                    stateRegister,
                    used,
                    YRegisterTag,
                    0
                );
                lastUse = calls.FindLastIndex(call => call.Field == used);
            }

            var previous = location;
            for (int index = 0; index < calls.Count; index++)
            {
                var (call, field) = calls[index];
                if (field == savedProjection)
                {
                    owner.MoveRegister(
                        YRegisterTag,
                        0,
                        XRegisterTag,
                        0
                    );
                    if (index == lastUse)
                    {
                        owner.Op(TrimFrame, BeamWire.UnsignedTag, 1);
                        owner.Operand(BeamWire.UnsignedTag, 1);
                        stateSlot = 0;
                    }
                }
                else if (index == 0)
                {
                    owner.EmitTuplePart(stateRegister, field, 0);
                }
                else
                {
                    EmitSavedRecordField(stateSlot, field, 0);
                }

                owner.EmitCallLine(call, previous);
                owner.EmitStaticRemoteCall(call);
                previous = CallLocation(call, previous);
            }

            EmitRetainedRecordReply(reply);
            Mark(other);
            if (report is null)
            {
                owner.Op(TestHeap, BeamWire.UnsignedTag, resultPrefix.Length + 2);
                owner.Operand(BeamWire.UnsignedTag, function.Arity);
                owner.EmitTupleHeader(XRegisterTag, resultPrefix.Length + 1);
                foreach (Term value in resultPrefix)
                {
                    owner.LiteralOperand(value);
                }

                owner.Operand(XRegisterTag, stateRegister);
                owner.instructions.WriteByte(Return);

                return true;
            }

            if (report.Field is not null)
            {
                EmitDetailRecordTest(
                    stateRegister,
                    fields.Count,
                    tag,
                    shapeFailure
                );
            }

            EmitFrameAllocation(function.Arity, 1, ConsHeapWords);
            owner.MoveRegister(
                XRegisterTag,
                stateRegister,
                YRegisterTag,
                0
            );
            owner.Op(PutList, XRegisterTag, 0);
            owner.LiteralOperand(Nil.Value);
            owner.Operand(XRegisterTag, report.Field.HasValue ? 2 : 1);
            if (report.Field is int configuration)
            {
                EmitSavedRecordField(0, configuration, 0);
            }

            owner.EmitConstant(report.Message, report.Field.HasValue ? 1 : 0);
            owner.EmitCallLine(report.Call, location);
            owner.EmitStaticRemoteCall(report.Call);
            EmitRetainedRecordReply(resultPrefix);
            if (report.Field.HasValue)
            {
                Mark(shapeFailure);
                owner.Op(BadRecord, XRegisterTag, stateRegister);
            }

            return true;

            static Term[]? ReadReply(Expr? expression, string name)
            {
                if (name == VariableScopeNames.Wildcard
                    || expression is not Expr.Tuple { Items: [.., Expr.Variable value] } tuple || value.Name != name)
                {
                    return null;
                }

                var constants = new Term[tuple.Items.Count - 1];
                for (int index = 0; index < constants.Length; index++)
                {
                    if (!AttributeLiteral.TryNormalizeConstant(tuple.Items[index], out Term term))
                    {
                        return null;
                    }

                    constants[index] = term;
                }

                return constants;
            }
        }
    }
}
