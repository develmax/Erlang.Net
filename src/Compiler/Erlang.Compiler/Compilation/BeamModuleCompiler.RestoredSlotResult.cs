using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private Expr? TryEmitRestoredSlotResultPrefix(Expr rest)
        {
            if (rest is not Expr.Sequence
                {
                    Items:
                    [
                        Expr.Match
                    {
                        Pattern: Pattern.Variable { Name: not VariableScopeNames.Wildcard } result,
                        Value: Expr.Call producer
                    },
                        var discarded,
                        _, ..
                    ]
                } sequence
                || UnwrapWildcardMatch(discarded) is not Expr.Call
                {
                    Arguments: [Expr.Variable oldValue]
                } restore
                || variables.ContainsKey(result.Name) || deferredPairMembers.ContainsKey(result.Name)
                || !variables.TryGetValue(oldValue.Name, out int slot)
                || deferredPairMembers.Values.Any(member => member.Slot == slot)
                || !TryResolveOrdinaryCall(producer, out _)
                || !TryResolveOrdinaryCall(restore, out string? module))
            {
                return null;
            }

            Expr continuation = sequence.Items.Count == 3
                ? sequence.Items[2]
                : sequence with { Items = sequence.Items.Skip(2).ToArray() };
            string[] aliases = variables.Where(binding => binding.Value == slot).Select(binding => binding.Key).ToArray();
            Pattern[] tracked = [result, .. aliases.Select(name => new Pattern.Variable(name))];
            if (ReadUsedEntryParameters(new Clause(tracked, null, continuation)) is not { } used
                || !used.Contains(result.Name) || aliases.Any(used.Contains))
            {
                return null;
            }

            // The restoring call consumes the last read of the old slot. Keep its
            // argument in X0 and give the slot to the producer result across that call.
            Expression(producer);
            owner.Op(Swap, YRegisterTag, slot);
            owner.Operand(XRegisterTag, 0);
            owner.EmitCallLine(restore, CallLocation(producer, location));
            Call(module, restore.Function, 1);
            foreach (string alias in aliases)
            {
                variables.Remove(alias);
                checkedRecordBindings.Remove(alias);
            }

            variables.Add(result.Name, slot);

            return continuation;
        }
    }
}
