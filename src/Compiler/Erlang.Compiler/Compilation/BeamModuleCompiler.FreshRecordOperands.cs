namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private const string SendAfterOperation = "send_after";
        private HashSet<RecordUpdateField>? freshRecordUpdateOperands;

        private bool PrefersCopyRecordOperand(RecordUpdateField field) =>
            owner.SsaTypeInferenceEnabled && !owner.ModuleOptimizationDisabled
            && (freshRecordUpdateOperands?.Contains(field) == true || IsFreshRecordOperand(field.Value));

        private void RememberFreshRecordOperand(Expr binding, Expr continuation)
        {
            if (!owner.SsaTypeInferenceEnabled || owner.ModuleOptimizationDisabled
                || binding is not Expr.Match
                {
                    Pattern: Pattern.Variable { Name: not VariableScopeNames.Wildcard } variable,
                    Value: var value
                }
                || variables.ContainsKey(variable.Name) || deferredPairMembers.ContainsKey(variable.Name)
                || !IsFreshRecordOperand(value))
            {
                return;
            }

            HashSet<RecordUpdateField>? next = null;
            foreach (RecordUpdateField field in ReadResultRecordFields(continuation))
            {
                if (field.Value is not Expr.Variable operand || operand.Name != variable.Name)
                {
                    continue;
                }

                // A fresh value is rarely identical to the previous field. Record the
                // operand occurrence so the proof cannot cross into a shadowed scope.
                next ??= freshRecordUpdateOperands is null
                    ? new HashSet<RecordUpdateField>(ReferenceEqualityComparer.Instance)
                    : new HashSet<RecordUpdateField>(freshRecordUpdateOperands, ReferenceEqualityComparer.Instance);
                next.Add(field);
            }

            freshRecordUpdateOperands = next ?? freshRecordUpdateOperands;
        }

        private bool IsFreshRecordOperand(Expr expression) =>
            expression is Expr.Call { Module: null or ErlangModuleNames.Module, DynamicRemote: false } call
            && owner.ResolvesErlangBif(call)
            && (call.Function, call.Arguments.Count) is
                (AutoImportNames.MakeRef, 0) or (AutoImportNames.Monitor, 2) or (SendAfterOperation, 3 or 4);

        private static IEnumerable<RecordUpdateField> ReadResultRecordFields(Expr expression)
        {
            switch (expression)
            {
                case Expr.Tuple wrapper:
                    foreach (Expr item in wrapper.Items)
                    {
                        foreach (var field in ReadResultRecordFields(item))
                        {
                            yield return field;
                        }
                    }

                    break;
                case Expr.RecordUpdate update:
                    foreach (RecordUpdateField field in update.Fields)
                    {
                        yield return field;
                        foreach (var nested in ReadResultRecordFields(field.Value))
                        {
                            yield return nested;
                        }
                    }

                    break;
            }
        }
    }
}
