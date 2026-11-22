using static Erlang.Compiler.BeamEmissionFormat;
using SourcePattern = Erlang.Pattern;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitCheckedRetryOptionalUpdate(CheckedNilStateRetry plan)
        {
            if (DecoderSlot(0) != 0 || plan.Retry.Body is not Expr.Sequence
                {
                    Items:
                    [
                        Expr.Match
                    {
                        Pattern: SourcePattern.Variable { Name: not VariableScopeNames.Wildcard } updated,
                        Value: Expr.Case
                        {
                            Value: Expr.Variable selector,
                            Clauses: [var change, var unchanged]
                        }
                    },
                        Expr.Case
                    {
                        Value: Expr.Call { Arguments: [Expr.Variable argument] } consumer
                    } result
                    ]
                }
                || selector.Name != CheckedNilStateField || argument.Name != updated.Name
                || plan.RetryInputs.ContainsKey(updated.Name)
                || change is not
                {
                    Guard: null,
                    Patterns: [SourcePattern.Literal { Value: Atom key }],
                    Body: Expr.RecordUpdate
                    {
                        Value: Expr.Variable original,
                        Fields: [{ Value: Expr.Call { Arguments: [Expr.Variable payload, var separator] } producer } field]
                    } update
                }
                || unchanged is not
                {
                    Guard: null,
                    Patterns: [SourcePattern.Any or SourcePattern.Variable { Name: VariableScopeNames.Wildcard }],
                    Body: Expr.Variable carried
                }
                || carried.Name != original.Name || !plan.RetryInputs.TryGetValue(original.Name, out int recordRegister)
                || recordRegister != 0 || update.Name != plan.InnerTag.Name || update.Arity != plan.InnerWidth
                || field.Index != plan.Access.Index
                || !plan.RetryInputs.TryGetValue(payload.Name, out int payloadRegister)
                || payloadRegister != plan.Retry.Patterns.Count + 1
                || !AttributeLiteral.TryNormalizeConstant(separator, out Term literal)
                || !TryResolveOrdinaryCallTarget(producer, out string? producerModule)
                || !TryResolveOrdinaryCallTarget(consumer, out string? consumerModule))
            {
                return false;
            }

            var parameters = plan.RetryInputs.Keys.Append(updated.Name)
                .Select(name => new SourcePattern.Variable(name)).ToArray();
            var prepared = new Clause(parameters, null, result with { Value = new Expr.Literal(Nil.Value) });
            if (ReadUsedEntryParameters(prepared) is not { } used
                || used.Contains(updated.Name)
                || used.Contains(original.Name) || used.Contains(payload.Name) || used.Contains(selector.Name))
            {
                return false;
            }

            var saved = plan.RetryInputs.Where(binding => used.Contains(binding.Key))
                .OrderByDescending(binding => binding.Value).ToArray();
            if (saved.Any(binding => binding.Value is not (2 or 3)))
            {
                return false;
            }

            var owned = saved is [var binding] && binding.Value == 3
                ? ReadOwnedRetryResult(result, plan, binding.Key) : null;
            int retryFrame = owned is null ? frameSize : 2;
            int innerSlot = Slot();
            EmitFrameAllocation(payloadRegister + 1, retryFrame);
            EmitFrameInitialization(saved.Length + 1, retryFrame);
            owner.MoveRegister(
                XRegisterTag,
                recordRegister,
                YRegisterTag,
                innerSlot
            );
            foreach (var (name, register) in saved)
            {
                int slot = Slot();
                variables.Add(name, slot);
                owner.MoveRegister(
                    XRegisterTag,
                    register,
                    YRegisterTag,
                    slot
                );
            }

            foreach (SourcePattern pattern in plan.Retry.Patterns)
            {
                RememberEntryPatternRecords(pattern);
            }

            int useOriginal = Label();
            int invoke = Label();
            // This register is the checked selector on the failed nil edge.
            owner.EmitAtomTest(
                1,
                key.Name,
                useOriginal,
                type: NonNilTypeBits
            );
            owner.EmitConstant(literal, 1);
            owner.MoveRegister(
                XRegisterTag,
                payloadRegister,
                XRegisterTag,
                0
            );
            owner.EmitCallLine(producer, location);
            Call(producerModule, producer.Function, producer.Arguments.Count);
            owner.Op(TestHeap, BeamWire.UnsignedTag, update.Arity + 1);
            owner.Operand(BeamWire.UnsignedTag, 1);
            EmitSavedRecordUpdate(update.Arity, field.Index);
            JumpTo(invoke);

            Mark(useOriginal);
            Load(innerSlot);
            Mark(invoke);
            owner.InitStackSlot(innerSlot);
            owner.EmitCallLine(consumer, CallLocation(producer, location));
            Call(consumerModule, consumer.Function, consumer.Arguments.Count);

            if (owned is not null)
            {
                EmitOwnedRetryResult(owned, plan, CallLocation(consumer, CallLocation(producer, location)));

                return true;
            }

            // The original case consumes the ordinary call result already in X0.
            EmitCase(result, guardFailure: null, inputPrepared: true);
            owner.EmitFrameReturn(frameSize);

            return true;
        }
    }
}
